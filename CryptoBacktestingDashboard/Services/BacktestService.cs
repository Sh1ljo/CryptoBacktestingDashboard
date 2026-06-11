using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Repositories.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Services
{
    public class BacktestService
    {
        private readonly CandleDataRepository _candleRepo;
        private readonly BacktestResultRepository _resultRepo;

        // Commission charged per side (entry and exit) as a fraction of trade notional.
        private const decimal CommissionRate = 0.001m;

        public BacktestService(CandleDataRepository candleRepo, BacktestResultRepository resultRepo)
        {
            _candleRepo = candleRepo;
            _resultRepo = resultRepo;
        }

        public async Task<BacktestSession> RunBacktestAsync(BacktestSession session)
        {
            var strategy = session.Strategy;
            if (strategy == null)
                throw new InvalidOperationException("Strategy not loaded.");

            var hasIndicators = strategy.Indicators != null && strategy.Indicators.Count > 0;
            var hasComparisons = strategy.Comparisons != null && strategy.Comparisons.Count > 0;
            if (!hasIndicators && !hasComparisons)
                throw new InvalidOperationException("Strategy must have at least one indicator or comparison.");

            var candles = await _candleRepo.GetByPairIdAndDateRangeAsync(
                session.CryptoPairId, session.StartDate, session.EndDate);

            var maxIndicatorPeriod = GetMaxIndicatorPeriod(strategy);
            var warmupCandles = Math.Max(strategy.LookbackPeriod, maxIndicatorPeriod);
            var firstTradableIndex = warmupCandles + 1;
            var minRequired = firstTradableIndex + 1;

            if (candles.Count < minRequired)
                throw new InvalidOperationException(
                    $"Not enough candle data in date range {session.StartDate:yyyy-MMM-dd} to {session.EndDate:yyyy-MMM-dd}. " +
                    $"Need at least {minRequired} candles " +
                    $"(warmup requires {warmupCandles}), got {candles.Count}.");

            var results = new List<BacktestResult>();
            var dir = strategy.TradeDirection;

            decimal cash = session.InitialBalance;
            decimal? positionEntryPrice = null;
            decimal? positionQuantity = null;
            decimal? positionSize = null;          // entry notional (qty * entryPrice)
            int? positionOpenIndex = null;
            bool positionIsShort = false;
            decimal highestPriceSinceEntry = 0;    // for long trailing stop
            decimal lowestPriceSinceEntry = decimal.MaxValue; // for short trailing stop
            decimal positionEntryCommission = 0;

            var prices = candles.Select(c => c.Close).ToList();
            var highs = candles.Select(c => c.High).ToList();
            var lows = candles.Select(c => c.Low).ToList();

            for (int i = firstTradableIndex; i < candles.Count; i++)
            {
                var candle = candles[i];
                var currentPrices = prices.Take(i + 1).ToList();
                var currentHighs = highs.Take(i + 1).ToList();
                var currentLows = lows.Take(i + 1).ToList();

                // ── Compute combined signal ──────────────────────────────────
                bool hasBuy = false, hasSell = false;

                if (hasComparisons)
                {
                    foreach (var comparison in strategy.Comparisons)
                    {
                        var sig = ComputeComparisonSignal(comparison, currentPrices, currentHighs, currentLows, i);
                        if (sig == TradingSignal.Buy) hasBuy = true;
                        else if (sig == TradingSignal.Sell) hasSell = true;
                    }
                }

                if (hasIndicators)
                {
                    foreach (var indicator in strategy.Indicators)
                    {
                        var sig = ComputeSignal(indicator, currentPrices, currentHighs, currentLows, i, candle);
                        if (sig == TradingSignal.Buy) hasBuy = true;
                        else if (sig == TradingSignal.Sell) hasSell = true;
                    }
                }

                TradingSignal combinedSignal = TradingSignal.Hold;
                if (hasBuy && !hasSell) combinedSignal = TradingSignal.Buy;
                else if (hasSell && !hasBuy) combinedSignal = TradingSignal.Sell;

                // ── Manage open position ─────────────────────────────────────
                if (positionEntryPrice.HasValue)
                {
                    bool exitByRisk = false;
                    decimal exitPrice = candle.Close;

                    if (!positionIsShort)
                    {
                        // Long risk management
                        highestPriceSinceEntry = Math.Max(highestPriceSinceEntry, candle.High);
                        var stopLevel = positionEntryPrice.Value * (1 - strategy.StopLossPercent / 100m);
                        if (strategy.TrailingStopPercent.HasValue && strategy.TrailingStopPercent.Value > 0)
                        {
                            var trailLevel = highestPriceSinceEntry * (1 - strategy.TrailingStopPercent.Value / 100m);
                            stopLevel = Math.Max(stopLevel, trailLevel);
                        }
                        var tpLevel = positionEntryPrice.Value * (1 + strategy.TakeProfitPercent / 100m);

                        // Conservative: assume stop hit before TP if candle spans both
                        if (candle.Low <= stopLevel)
                        { exitPrice = stopLevel; exitByRisk = true; }
                        else if (candle.High >= tpLevel)
                        { exitPrice = tpLevel; exitByRisk = true; }
                    }
                    else
                    {
                        // Short risk management (inverted: stop above entry, TP below)
                        lowestPriceSinceEntry = Math.Min(lowestPriceSinceEntry, candle.Low);
                        var stopLevel = positionEntryPrice.Value * (1 + strategy.StopLossPercent / 100m);
                        if (strategy.TrailingStopPercent.HasValue && strategy.TrailingStopPercent.Value > 0)
                        {
                            // Trail stop tightens downward as price falls
                            var trailLevel = lowestPriceSinceEntry * (1 + strategy.TrailingStopPercent.Value / 100m);
                            stopLevel = Math.Min(stopLevel, trailLevel);
                        }
                        var tpLevel = positionEntryPrice.Value * (1 - strategy.TakeProfitPercent / 100m);

                        // Conservative: stop (upside) checked before TP (downside)
                        if (candle.High >= stopLevel)
                        { exitPrice = stopLevel; exitByRisk = true; }
                        else if (candle.Low <= tpLevel)
                        { exitPrice = tpLevel; exitByRisk = true; }
                    }

                    // Determine whether the signal closes this position
                    bool signalCloses = !positionIsShort && combinedSignal == TradingSignal.Sell
                                     || positionIsShort  && combinedSignal == TradingSignal.Buy;

                    if (exitByRisk || signalCloses)
                    {
                        var exitNotional = exitPrice * positionQuantity!.Value;
                        var exitCommission = exitNotional * CommissionRate;

                        decimal cashReturn;
                        if (!positionIsShort)
                        {
                            // Long close: receive exit proceeds
                            cashReturn = exitNotional - exitCommission;
                        }
                        else
                        {
                            // Short close: return collateral ± P/L
                            // Net = (entryPrice - exitPrice) * qty - totalCommission
                            // Cash = collateral(positionSize) + netProfit + exitCommission adjustment
                            cashReturn = 2 * positionSize!.Value - exitNotional - exitCommission;
                        }

                        var trade = new BacktestResult
                        {
                            BacktestSessionId = session.Id,
                            TradeType = positionIsShort ? TradeType.Short : TradeType.Long,
                            EntryTime = candles[positionOpenIndex!.Value].OpenTime,
                            ExitTime = candle.OpenTime,
                            EntryPrice = positionEntryPrice.Value,
                            ExitPrice = exitPrice,
                            Quantity = positionQuantity.Value,
                            Commission = positionEntryCommission + exitCommission,
                            IsWinningTrade = positionIsShort
                                ? exitPrice < positionEntryPrice.Value
                                : exitPrice > positionEntryPrice.Value
                        };

                        cash += cashReturn;
                        results.Add(trade);

                        positionEntryPrice = null;
                        positionQuantity = null;
                        positionSize = null;
                        positionOpenIndex = null;
                        positionIsShort = false;
                        highestPriceSinceEntry = 0;
                        lowestPriceSinceEntry = decimal.MaxValue;
                        positionEntryCommission = 0;
                    }
                }

                // ── Open new position ────────────────────────────────────────
                // Evaluated after any close, so a flip (close long + open short on same
                // candle) works naturally when TradeDirection == Both.
                if (!positionEntryPrice.HasValue)
                {
                    bool openLong  = combinedSignal == TradingSignal.Buy
                                  && (dir == TradeDirection.LongOnly || dir == TradeDirection.Both);
                    bool openShort = combinedSignal == TradingSignal.Sell
                                  && (dir == TradeDirection.ShortOnly || dir == TradeDirection.Both);

                    if (openLong || openShort)
                    {
                        var qty = CalculatePositionSize(cash, candle.Close, strategy, currentPrices, currentHighs, currentLows);
                        if (qty > 0)
                        {
                            var cost = qty * candle.Close;
                            var entryCommission = cost * CommissionRate;
                            if (cost + entryCommission <= cash)
                            {
                                positionEntryPrice = candle.Close;
                                positionQuantity = qty;
                                positionSize = cost;
                                positionOpenIndex = i;
                                positionIsShort = openShort;
                                highestPriceSinceEntry = candle.High;
                                lowestPriceSinceEntry = candle.Low;
                                positionEntryCommission = entryCommission;
                                cash -= (cost + entryCommission);
                            }
                        }
                    }
                }
            }

            // ── Close any remaining position at last candle ──────────────────
            if (positionEntryPrice.HasValue && positionOpenIndex.HasValue)
            {
                var lastCandle = candles[^1];
                var exitNotional = lastCandle.Close * positionQuantity!.Value;
                var exitCommission = exitNotional * CommissionRate;

                decimal cashReturn = positionIsShort
                    ? 2 * positionSize!.Value - exitNotional - exitCommission
                    : exitNotional - exitCommission;

                var trade = new BacktestResult
                {
                    BacktestSessionId = session.Id,
                    TradeType = positionIsShort ? TradeType.Short : TradeType.Long,
                    EntryTime = candles[positionOpenIndex.Value].OpenTime,
                    ExitTime = lastCandle.OpenTime,
                    EntryPrice = positionEntryPrice.Value,
                    ExitPrice = lastCandle.Close,
                    Quantity = positionQuantity.Value,
                    Commission = positionEntryCommission + exitCommission,
                    IsWinningTrade = positionIsShort
                        ? lastCandle.Close < positionEntryPrice.Value
                        : lastCandle.Close > positionEntryPrice.Value
                };

                cash += cashReturn;
                results.Add(trade);
            }

            session.FinalBalance = cash;
            session.ExecutedAt = DateTime.Now;

            await _resultRepo.DeleteBySessionIdAsync(session.Id);
            foreach (var r in results)
                await _resultRepo.InsertItemAsync(r);

            session.Results = results;
            return session;
        }

        private static int GetMaxIndicatorPeriod(BacktestStrategy strategy)
        {
            int maxPeriod = 0;
            if (strategy.Indicators != null && strategy.Indicators.Count > 0)
                maxPeriod = strategy.Indicators.Max(GetIndicatorWarmup);

            if (strategy.Comparisons != null && strategy.Comparisons.Count > 0)
            {
                foreach (var c in strategy.Comparisons)
                {
                    if (c.IndicatorA != null) maxPeriod = Math.Max(maxPeriod, GetIndicatorWarmup(c.IndicatorA));
                    if (c.IndicatorB != null) maxPeriod = Math.Max(maxPeriod, GetIndicatorWarmup(c.IndicatorB));
                }
            }
            return maxPeriod;
        }

        private static int GetIndicatorWarmup(Indicator indicator)
        {
            switch (indicator.Type)
            {
                case IndicatorType.MACD:
                    var fastPeriod = indicator.Period > 0 ? indicator.Period : 12;
                    var slowPeriod = (int)(indicator.Threshold > 0 ? indicator.Threshold : 26);
                    return Math.Max(fastPeriod, slowPeriod) + 9;
                case IndicatorType.BollingerBands:
                    return indicator.Period;
                case IndicatorType.Stochastic:
                    return indicator.Period + 3 + 3;
                default:
                    return indicator.Period;
            }
        }

        private TradingSignal ComputeSignal(
            Indicator indicator, List<decimal> prices, List<decimal> highs, List<decimal> lows,
            int currentIndex, CandleData currentCandle)
        {
            switch (indicator.Type)
            {
                case IndicatorType.RSI:
                {
                    var rsi = IndicatorCalculator.CalculateRsi(prices, indicator.Period);
                    return StrategyEvaluator.Evaluate(indicator.Type,
                        rsi.ElementAtOrDefault(currentIndex), rsi.ElementAtOrDefault(currentIndex - 1),
                        indicator.Threshold);
                }
                case IndicatorType.MACD:
                {
                    int fast = indicator.Period > 0 ? indicator.Period : 12;
                    int slow = (int)(indicator.Threshold > 0 ? indicator.Threshold : 26);
                    var (_, _, hist) = IndicatorCalculator.CalculateMacd(prices, fast, slow);
                    return StrategyEvaluator.Evaluate(indicator.Type,
                        hist.ElementAtOrDefault(currentIndex), hist.ElementAtOrDefault(currentIndex - 1),
                        indicator.Threshold);
                }
                case IndicatorType.MovingAverage:
                {
                    var ma = IndicatorCalculator.CalculateEma(prices, indicator.Period);
                    return StrategyEvaluator.Evaluate(indicator.Type,
                        ma.ElementAtOrDefault(currentIndex), ma.ElementAtOrDefault(currentIndex - 1),
                        indicator.Threshold,
                        currentPrice: currentCandle.Close,
                        previousPrice: currentIndex > 0 ? prices[currentIndex - 1] : null);
                }
                case IndicatorType.BollingerBands:
                {
                    var (upper, _, lower) = IndicatorCalculator.CalculateBollingerBands(
                        prices, indicator.Period, indicator.Threshold);
                    return StrategyEvaluator.Evaluate(indicator.Type, null, null, indicator.Threshold,
                        currentPrice: currentCandle.Close,
                        upperBand: upper.ElementAtOrDefault(currentIndex),
                        lowerBand: lower.ElementAtOrDefault(currentIndex));
                }
                case IndicatorType.Stochastic:
                {
                    var (kValues, _) = IndicatorCalculator.CalculateStochastic(highs, lows, prices, indicator.Period);
                    return StrategyEvaluator.Evaluate(indicator.Type,
                        kValues.ElementAtOrDefault(currentIndex), kValues.ElementAtOrDefault(currentIndex - 1),
                        indicator.Threshold);
                }
                default:
                    return TradingSignal.Hold;
            }
        }

        private TradingSignal ComputeComparisonSignal(
            IndicatorComparison comparison, List<decimal> prices, List<decimal> highs, List<decimal> lows,
            int currentIndex)
        {
            if (comparison.IndicatorA == null || comparison.IndicatorB == null)
                return TradingSignal.Hold;

            var valuesA = ComputeIndicatorValues(comparison.IndicatorA, prices, highs, lows);
            var valuesB = ComputeIndicatorValues(comparison.IndicatorB, prices, highs, lows);

            return StrategyEvaluator.EvaluateComparison(
                comparison.ComparisonType,
                valuesA.ElementAtOrDefault(currentIndex), valuesB.ElementAtOrDefault(currentIndex),
                valuesA.ElementAtOrDefault(currentIndex - 1), valuesB.ElementAtOrDefault(currentIndex - 1),
                comparison.TargetSignal);
        }

        private List<decimal?> ComputeIndicatorValues(Indicator indicator, List<decimal> prices, List<decimal> highs, List<decimal> lows)
        {
            switch (indicator.Type)
            {
                case IndicatorType.RSI:
                    return IndicatorCalculator.CalculateRsi(prices, indicator.Period);
                case IndicatorType.MACD:
                {
                    int fast = indicator.Period > 0 ? indicator.Period : 12;
                    int slow = (int)(indicator.Threshold > 0 ? indicator.Threshold : 26);
                    var (_, _, hist) = IndicatorCalculator.CalculateMacd(prices, fast, slow);
                    return hist;
                }
                case IndicatorType.MovingAverage:
                    return IndicatorCalculator.CalculateEma(prices, indicator.Period);
                case IndicatorType.BollingerBands:
                {
                    var (_, middle, _) = IndicatorCalculator.CalculateBollingerBands(prices, indicator.Period, indicator.Threshold);
                    return middle;
                }
                case IndicatorType.Stochastic:
                {
                    var (kValues, _) = IndicatorCalculator.CalculateStochastic(highs, lows, prices, indicator.Period);
                    return kValues;
                }
                case IndicatorType.ATR:
                    return IndicatorCalculator.CalculateAtr(highs, lows, prices, indicator.Period);
                default:
                    return new List<decimal?>();
            }
        }

        private decimal CalculatePositionSize(
            decimal cash, decimal entryPrice, BacktestStrategy strategy,
            List<decimal> prices, List<decimal> highs, List<decimal> lows)
        {
            if (cash <= 0 || entryPrice <= 0) return 0;
            decimal QtyFor(decimal budget) =>
                Math.Round((budget / (1 + CommissionRate)) / entryPrice, 6, MidpointRounding.ToZero);
            var pct = strategy.PositionSizePercent.HasValue && strategy.PositionSizePercent.Value > 0
                ? Math.Min(strategy.PositionSizePercent.Value, 100m)
                : 100m;
            return QtyFor(cash * (pct / 100m));
        }
    }
}
