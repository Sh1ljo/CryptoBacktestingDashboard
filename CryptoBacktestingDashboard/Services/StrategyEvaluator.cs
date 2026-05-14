using CryptoBacktestingDashboard.Models.Crypto;

namespace CryptoBacktestingDashboard.Services
{
    public enum TradingSignal
    {
        Hold,
        Buy,
        Sell
    }

    public static class StrategyEvaluator
    {
        public static TradingSignal Evaluate(
            IndicatorType indicatorType,
            decimal? currentValue,
            decimal? previousValue,
            decimal threshold,
            decimal? currentPrice = null,
            decimal? previousPrice = null,
            decimal? upperBand = null,
            decimal? lowerBand = null)
        {
            if (currentValue == null)
                return TradingSignal.Hold;

            switch (indicatorType)
            {
                case IndicatorType.RSI:
                    // RSI > threshold (e.g. 70) = overbought → Sell
                    // RSI < (100 - threshold) (e.g. 30) = oversold → Buy
                    if (currentValue > threshold)
                        return TradingSignal.Sell;
                    if (currentValue < (100 - threshold))
                        return TradingSignal.Buy;
                    return TradingSignal.Hold;

                case IndicatorType.MACD:
                    // Histogram crossing zero: negative → positive = Buy, positive → negative = Sell
                    if (previousValue == null)
                        return TradingSignal.Hold;
                    if (previousValue <= 0 && currentValue > 0)
                        return TradingSignal.Buy;
                    if (previousValue >= 0 && currentValue < 0)
                        return TradingSignal.Sell;
                    return TradingSignal.Hold;

                case IndicatorType.MovingAverage:
                    // Price crossing above MA = Buy, below = Sell
                    if (currentPrice == null || previousPrice == null || previousValue == null)
                        return TradingSignal.Hold;
                    if (previousPrice <= previousValue && currentPrice > currentValue)
                        return TradingSignal.Buy;
                    if (previousPrice >= previousValue && currentPrice < currentValue)
                        return TradingSignal.Sell;
                    return TradingSignal.Hold;

                case IndicatorType.BollingerBands:
                    // Price touches upper band = Sell, touches lower band = Buy
                    if (currentPrice == null || upperBand == null || lowerBand == null)
                        return TradingSignal.Hold;
                    if (currentPrice >= upperBand)
                        return TradingSignal.Sell;
                    if (currentPrice <= lowerBand)
                        return TradingSignal.Buy;
                    return TradingSignal.Hold;

                case IndicatorType.Stochastic:
                    // K > 80 = overbought → Sell, K < 20 = oversold → Buy
                    if (currentValue > 80)
                        return TradingSignal.Sell;
                    if (currentValue < 20)
                        return TradingSignal.Buy;
                    return TradingSignal.Hold;

                case IndicatorType.ATR:
                    // ATR is a volatility measure, not a direct signal
                    return TradingSignal.Hold;

                default:
                    return TradingSignal.Hold;
            }
        }
    }
}