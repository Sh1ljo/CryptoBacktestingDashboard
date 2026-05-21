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
        private const decimal CommissionPerTrade = 10m;

        public BacktestService(CandleDataRepository candleRepo, BacktestResultRepository resultRepo)
        {
            _candleRepo = candleRepo;
            _resultRepo = resultRepo;
        }

        public async Task<BacktestSession> RunBacktestAsync(BacktestSession session)
        {
            var strategy = session.Strategy;
            if (strategy == null || strategy.Indicators == null || strategy.Indicators.Count == 0)
                throw new InvalidOperationException("Strategy must have at least one indicator.");

            var candles = await _candleRepo.GetByPairIdAndDateRangeAsync(
                session.CryptoPairId, session.StartDate, session.EndDate);

            // Calculate the minimum warmup from the actual indicator periods
            var maxIndicatorPeriod = GetMaxIndicatorPeriod(strategy.Indicators);
            var warmupCandles = Math.Max(strategy.LookbackPeriod, maxIndicatorPeriod);
            var minRequired = warmupCandles + 1;

            if (candles.Count < minRequired)
                throw new InvalidOperationException(
                    $"Not enough candle data in date range {session.StartDate:yyyy-MMM-dd} to {session.EndDate:yyyy-MMM-dd}. " +
                    $"Need at least {minRequired} candles " +
                    $"(warmup requires {warmupCandles}), got {candles.Count}.");

            var results = new List<BacktestResult>();
            decimal cash = session.InitialBalance;
            decimal? positionEntryPrice = null;
            decimal? positionQuantity = null;
            decimal? positionSize = null;
            int? positionOpenIndex = null;
            decimal highestPriceSinceEntry = 0;

            var prices = candles.Select(c => c.Close).ToList();
            var highs = candles.Select(c => c.High).ToList();
            var lows = candles.Select(c => c.Low).ToList();

            for (int i = warmupCandles; i < candles.Count; i++)
            {
                var candle = candles[i];
                var currentPrices = prices.Take(i + 1).ToList();
                var currentHighs = highs.Take(i + 1).ToList();
                var currentLows = lows.Take(i + 1).ToList();

                // Compute signals from all indicators
                bool hasBuy = false, hasSell = false;

                foreach (var indicator in strategy.Indicators)
                {
                    var signal = ComputeSignal(indicator, currentPrices, currentHighs, currentLows, i, candle);
                    if (signal == TradingSignal.Buy) hasBuy = true;
                    else if (signal == TradingSignal.Sell) hasSell = true;
                }

                // Compute signals from indicator comparisons
                foreach (var comparison in strategy.Comparisons)
                {
                    var signal = ComputeComparisonSignal(comparison, currentPrices, currentHighs, currentLows, i);
                    if (signal == TradingSignal.Buy) hasBuy = true;
                    else if (signal == TradingSignal.Sell) hasSell = true;
                }

                // Combined signal: simple majority wins, conflicting signals = hold
                TradingSignal combinedSignal = TradingSignal.Hold;
                if (hasBuy && !hasSell) combinedSignal = TradingSignal.Buy;
                else if (hasSell && !hasBuy) combinedSignal = TradingSignal.Sell;

                // Check risk management for open position
                if (positionEntryPrice.HasValue)
                {
                    var risk = strategy.RiskManagement;
                    bool exitByRisk = false;
                    decimal exitPrice = candle.Close;

                    if (risk != null)
                    {
                        switch (risk.Type)
                        {
                            case RiskManagementType.StopLoss:
                                var stopLevel = positionEntryPrice.Value * (1 - risk.Value / 100m);
                                if (candle.Low <= stopLevel)
                                {
                                    exitPrice = stopLevel;
                                    exitByRisk = true;
                                }
                                break;

                            case RiskManagementType.TakeProfit:
                                var takeProfitLevel = positionEntryPrice.Value * (1 + risk.Value / 100m);
                                if (candle.High >= takeProfitLevel)
                                {
                                    exitPrice = takeProfitLevel;
                                    exitByRisk = true;
                                }
                                break;

                            case RiskManagementType.TrailingStop:
                                highestPriceSinceEntry = Math.Max(highestPriceSinceEntry, candle.High);
                                var trailStop = highestPriceSinceEntry * (1 - risk.Value / 100m);
                                if (candle.Low <= trailStop)
                                {
                                    exitPrice = trailStop;
                                    exitByRisk = true;
                                }
                                break;
                        }
                    }

                    if (exitByRisk || combinedSignal == TradingSignal.Sell)
                    {
                        // Close position
                        var trade = new BacktestResult
                        {
                            BacktestSessionId = session.Id,
                            TradeType = TradeType.Long,
                            EntryTime = candles[positionOpenIndex!.Value].OpenTime,
                            ExitTime = candle.OpenTime,
                            EntryPrice = positionEntryPrice.Value,
                            ExitPrice = exitPrice,
                            Quantity = positionQuantity!.Value,
                            Commission = CommissionPerTrade,
                            IsWinningTrade = exitPrice > positionEntryPrice.Value
                        };

                        cash += (exitPrice * positionQuantity.Value) - CommissionPerTrade;
                        results.Add(trade);

                        positionEntryPrice = null;
                        positionQuantity = null;
                        positionSize = null;
                        positionOpenIndex = null;
                        highestPriceSinceEntry = 0;
                    }
                    else
                    {
                        // Update trailing highest price
                        highestPriceSinceEntry = Math.Max(highestPriceSinceEntry, candle.High);
                    }
                }

                // Open new position if no open position and we have a buy signal
                if (!positionEntryPrice.HasValue && combinedSignal == TradingSignal.Buy)
                {
                    var qty = CalculatePositionSize(cash, candle.Close, strategy, currentPrices, currentHighs, currentLows);
                    if (qty > 0)
                    {
                        var cost = qty * candle.Close;
                        if (cost + CommissionPerTrade <= cash)
                        {
                            positionEntryPrice = candle.Close;
                            positionQuantity = qty;
                            positionSize = cost;
                            positionOpenIndex = i;
                            highestPriceSinceEntry = candle.High;

                            cash -= (cost + CommissionPerTrade);
                        }
                    }
                }
            }

            // Close any remaining position at last price
            if (positionEntryPrice.HasValue && positionOpenIndex.HasValue)
            {
                var lastCandle = candles[^1];
                var trade = new BacktestResult
                {
                    BacktestSessionId = session.Id,
                    TradeType = TradeType.Long,
                    EntryTime = candles[positionOpenIndex.Value].OpenTime,
                    ExitTime = lastCandle.OpenTime,
                    EntryPrice = positionEntryPrice.Value,
                    ExitPrice = lastCandle.Close,
                    Quantity = positionQuantity!.Value,
                    Commission = CommissionPerTrade,
                    IsWinningTrade = lastCandle.Close > positionEntryPrice.Value
                };

                cash += (lastCandle.Close * positionQuantity.Value) - CommissionPerTrade;
                results.Add(trade);
            }

            // Update session
            session.FinalBalance = cash;
            session.ExecutedAt = DateTime.Now;

            // Persist results
            await _resultRepo.DeleteBySessionIdAsync(session.Id);

            foreach (var r in results)
            {
                await _resultRepo.InsertItemAsync(r);
            }

            session.Results = results;
            return session;
        }

        private static int GetMaxIndicatorPeriod(ICollection<Indicator> indicators)
        {
            return indicators.Max(i => i.Type switch
            {
                IndicatorType.MACD => Math.Max(i.Period, (int)i.Threshold), // slow period is stored in Threshold
                IndicatorType.BollingerBands => i.Period,
                IndicatorType.Stochastic => i.Period + 3 + 3, // K period + smoothing + D period
                _ => i.Period
            });
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
                        var curr = rsi.ElementAtOrDefault(currentIndex);
                        var prev = rsi.ElementAtOrDefault(currentIndex - 1);
                        return StrategyEvaluator.Evaluate(indicator.Type, curr, prev, indicator.Threshold);
                    }

                case IndicatorType.MACD:
                    {
                        int fastPeriod = indicator.Period > 0 ? indicator.Period : 12;
                        int slowPeriod = (int)(indicator.Threshold > 0 ? indicator.Threshold : 26);
                        var (_, _, hist) = IndicatorCalculator.CalculateMacd(prices, fastPeriod, slowPeriod);
                        var curr = hist.ElementAtOrDefault(currentIndex);
                        var prev = hist.ElementAtOrDefault(currentIndex - 1);
                        return StrategyEvaluator.Evaluate(indicator.Type, curr, prev, indicator.Threshold);
                    }

                case IndicatorType.MovingAverage:
                    {
                        var ma = IndicatorCalculator.CalculateEma(prices, indicator.Period);
                        var currMa = ma.ElementAtOrDefault(currentIndex);
                        var prevMa = ma.ElementAtOrDefault(currentIndex - 1);
                        return StrategyEvaluator.Evaluate(
                            indicator.Type, currMa, prevMa, indicator.Threshold,
                            currentPrice: currentCandle.Close,
                            previousPrice: currentIndex > 0 ? prices[currentIndex - 1] : null);
                    }

                case IndicatorType.BollingerBands:
                    {
                        var (upper, _, lower) = IndicatorCalculator.CalculateBollingerBands(
                            prices, indicator.Period, indicator.Threshold);
                        var currUpper = upper.ElementAtOrDefault(currentIndex);
                        var currLower = lower.ElementAtOrDefault(currentIndex);
                        return StrategyEvaluator.Evaluate(
                            indicator.Type, null, null, indicator.Threshold,
                            currentPrice: currentCandle.Close,
                            upperBand: currUpper, lowerBand: currLower);
                    }

                case IndicatorType.Stochastic:
                    {
                        var (kValues, _) = IndicatorCalculator.CalculateStochastic(
                            highs, lows, prices, indicator.Period);
                        var curr = kValues.ElementAtOrDefault(currentIndex);
                        var prev = kValues.ElementAtOrDefault(currentIndex - 1);
                        return StrategyEvaluator.Evaluate(indicator.Type, curr, prev, indicator.Threshold);
                    }

                case IndicatorType.ATR:
                    // ATR is informational (volatility), no direct signal
                    return TradingSignal.Hold;

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

            var indicatorA = comparison.IndicatorA;
            var indicatorB = comparison.IndicatorB;

            // Compute values for both indicators
            var valuesA = ComputeIndicatorValues(indicatorA, prices, highs, lows);
            var valuesB = ComputeIndicatorValues(indicatorB, prices, highs, lows);

            var currA = valuesA.ElementAtOrDefault(currentIndex);
            var currB = valuesB.ElementAtOrDefault(currentIndex);
            var prevA = valuesA.ElementAtOrDefault(currentIndex - 1);
            var prevB = valuesB.ElementAtOrDefault(currentIndex - 1);

            return StrategyEvaluator.EvaluateComparison(
                comparison.ComparisonType, currA, currB, prevA, prevB, comparison.TargetSignal);
        }

        private List<decimal?> ComputeIndicatorValues(Indicator indicator, List<decimal> prices, List<decimal> highs, List<decimal> lows)
        {
            if (indicator.Type == IndicatorType.RSI)
                return IndicatorCalculator.CalculateRsi(prices, indicator.Period);

            if (indicator.Type == IndicatorType.MACD)
            {
                int fastPeriod = indicator.Period > 0 ? indicator.Period : 12;
                int slowPeriod = (int)(indicator.Threshold > 0 ? indicator.Threshold : 26);
                var (_, _, hist) = IndicatorCalculator.CalculateMacd(prices, fastPeriod, slowPeriod);
                return hist;
            }

            if (indicator.Type == IndicatorType.MovingAverage)
                return IndicatorCalculator.CalculateEma(prices, indicator.Period);

            if (indicator.Type == IndicatorType.BollingerBands)
            {
                var (_, middle, _) = IndicatorCalculator.CalculateBollingerBands(prices, indicator.Period, indicator.Threshold);
                return middle;
            }

            if (indicator.Type == IndicatorType.Stochastic)
            {
                var (kValues, _) = IndicatorCalculator.CalculateStochastic(highs, lows, prices, indicator.Period);
                return kValues;
            }

            if (indicator.Type == IndicatorType.ATR)
                return IndicatorCalculator.CalculateAtr(highs, lows, prices, indicator.Period);

            return new List<decimal?>();
        }

        private decimal CalculatePositionSize(
            decimal cash, decimal entryPrice, BacktestStrategy strategy,
            List<decimal> prices, List<decimal> highs, List<decimal> lows)
        {
            if (cash <= 0 || entryPrice <= 0) return 0;

            var risk = strategy.RiskManagement;
            if (risk == null)
            {
                // Default: invest all cash (leaving room for commission)
                var availableCash = Math.Max(0, cash - CommissionPerTrade);
                return Math.Round(availableCash / entryPrice, 6);
            }

            switch (risk.Type)
            {
                case RiskManagementType.FixedPositionSize:
                    return risk.Value;

                case RiskManagementType.PercentageRisk:
                    // Risk Value% of capital: position size = cash * (Value/100)
                    var riskCapital = cash * (risk.Value / 100m);
                    var availableRisk = Math.Max(0, riskCapital - CommissionPerTrade);
                    return Math.Round(availableRisk / entryPrice, 6);

                case RiskManagementType.StopLoss:
                case RiskManagementType.TakeProfit:
                case RiskManagementType.TrailingStop:
                default:
                    // If risk management is for exit (SL/TP), still need a position size
                    // Default to full allocation (leaving room for commission)
                    var available = Math.Max(0, cash - CommissionPerTrade);
                    return Math.Round(available / entryPrice, 6);
            }
        }
    }
}