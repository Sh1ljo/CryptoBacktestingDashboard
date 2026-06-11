using System;
using System.Collections.Generic;
using System.Linq;

namespace CryptoBacktestingDashboard.Services
{
    public static class IndicatorCalculator
    {
        public static List<decimal?> CalculateRsi(List<decimal> prices, int period = 14)
        {
            var result = new List<decimal?>();
            if (prices.Count < period + 1)
            {
                for (int i = 0; i < prices.Count; i++) result.Add(null);
                return result;
            }

            for (int i = 0; i < prices.Count; i++)
            {
                if (i < period)
                {
                    result.Add(null);
                    continue;
                }

                decimal avgGain = 0, avgLoss = 0;
                for (int j = i - period + 1; j <= i; j++)
                {
                    var diff = prices[j] - prices[j - 1];
                    if (diff > 0) avgGain += diff;
                    else avgLoss += Math.Abs(diff);
                }
                avgGain /= period;
                avgLoss /= period;

                if (avgLoss == 0)
                {
                    result.Add(100);
                    continue;
                }

                var rs = avgGain / avgLoss;
                result.Add(100 - (100 / (1 + rs)));
            }

            return result;
        }

        public static (List<decimal?> MacdLine, List<decimal?> SignalLine, List<decimal?> Histogram) CalculateMacd(
            List<decimal> prices, int fastPeriod = 12, int slowPeriod = 26, int signalPeriod = 9)
        {
            var fastEma = CalculateEma(prices, fastPeriod);
            var slowEma = CalculateEma(prices, slowPeriod);

            var macdLine = new List<decimal?>();
            for (int i = 0; i < prices.Count; i++)
            {
                if (fastEma[i] == null || slowEma[i] == null)
                    macdLine.Add(null);
                else
                    macdLine.Add(fastEma[i]!.Value - slowEma[i]!.Value);
            }

            var validMacd = macdLine.Select(v => v).ToList();
            var signalLine = CalculateEma(validMacd, signalPeriod);

            var histogram = new List<decimal?>();
            for (int i = 0; i < prices.Count; i++)
            {
                if (macdLine[i] == null || signalLine[i] == null)
                    histogram.Add(null);
                else
                    histogram.Add(macdLine[i]!.Value - signalLine[i]!.Value);
            }

            return (macdLine, signalLine, histogram);
        }

        public static List<decimal?> CalculateSma(List<decimal> prices, int period)
        {
            var result = new List<decimal?>();
            for (int i = 0; i < prices.Count; i++)
            {
                if (i < period - 1)
                {
                    result.Add(null);
                    continue;
                }

                decimal sum = 0;
                for (int j = i - period + 1; j <= i; j++)
                    sum += prices[j];
                result.Add(sum / period);
            }
            return result;
        }

        public static List<decimal?> CalculateEma(List<decimal> prices, int period)
        {
            var result = new List<decimal?>();
            if (prices.Count == 0) return result;

            var multiplier = 2m / (period + 1);

            for (int i = 0; i < prices.Count; i++)
            {
                if (i < period - 1)
                {
                    result.Add(null);
                    continue;
                }

                if (i == period - 1)
                {
                    decimal sum = 0;
                    for (int j = 0; j < period; j++)
                        sum += prices[j];
                    result.Add(sum / period);
                }
                else
                {
                    result.Add((prices[i] - result[i - 1]!.Value) * multiplier + result[i - 1]!.Value);
                }
            }

            return result;
        }

        // Overload for nullable decimal list (used for MACD signal line).
        // The input can begin with a run of leading nulls (e.g. the MACD line is null
        // during the slow-EMA warmup), so the seed is driven by how many consecutive
        // non-null values we've seen, NOT by a fixed index. This avoids dereferencing a
        // null previous EMA value, which previously crashed every MACD backtest.
        private static List<decimal?> CalculateEma(List<decimal?> values, int period)
        {
            var result = new List<decimal?>();
            if (values.Count == 0) return result;
            var multiplier = 2m / (period + 1);

            decimal? previousEma = null;   // last computed EMA value
            decimal seedSum = 0;           // running sum while collecting seed window
            int seedCount = 0;             // consecutive non-null values gathered for the seed

            for (int i = 0; i < values.Count; i++)
            {
                if (values[i] == null)
                {
                    // A gap resets the seed; we can only EMA over a contiguous run.
                    result.Add(null);
                    previousEma = null;
                    seedSum = 0;
                    seedCount = 0;
                    continue;
                }

                var value = values[i]!.Value;

                if (previousEma == null)
                {
                    // Still building the initial SMA seed.
                    seedSum += value;
                    seedCount++;

                    if (seedCount < period)
                    {
                        result.Add(null);
                    }
                    else
                    {
                        previousEma = seedSum / period;
                        result.Add(previousEma);
                    }
                }
                else
                {
                    previousEma = (value - previousEma.Value) * multiplier + previousEma.Value;
                    result.Add(previousEma);
                }
            }

            return result;
        }

        public static (List<decimal?> Upper, List<decimal?> Middle, List<decimal?> Lower) CalculateBollingerBands(
            List<decimal> prices, int period = 20, decimal stdDev = 2)
        {
            var middle = CalculateSma(prices, period);
            var upper = new List<decimal?>();
            var lower = new List<decimal?>();

            for (int i = 0; i < prices.Count; i++)
            {
                if (middle[i] == null)
                {
                    upper.Add(null);
                    lower.Add(null);
                    continue;
                }

                decimal sumSq = 0;
                for (int j = i - period + 1; j <= i; j++)
                {
                    var diff = prices[j] - middle[i]!.Value;
                    sumSq += diff * diff;
                }
                var std = (decimal)Math.Sqrt((double)(sumSq / period));

                upper.Add(middle[i]!.Value + stdDev * std);
                lower.Add(middle[i]!.Value - stdDev * std);
            }

            return (upper, middle, lower);
        }

        public static (List<decimal?> K, List<decimal?> D) CalculateStochastic(
            List<decimal> highs, List<decimal> lows, List<decimal> closes,
            int kPeriod = 14, int kSmoothing = 3, int dPeriod = 3)
        {
            var rawK = new List<decimal?>();
            for (int i = 0; i < closes.Count; i++)
            {
                if (i < kPeriod - 1)
                {
                    rawK.Add(null);
                    continue;
                }

                var highestHigh = highs.Skip(i - kPeriod + 1).Take(kPeriod).Max();
                var lowestLow = lows.Skip(i - kPeriod + 1).Take(kPeriod).Min();
                var range = highestHigh - lowestLow;

                if (range == 0)
                    rawK.Add(50);
                else
                    rawK.Add(((closes[i] - lowestLow) / range) * 100);
            }

            // Smooth K with SMA
            var kValues = new List<decimal?>();
            for (int i = 0; i < closes.Count; i++)
            {
                if (i < kPeriod - 1 + kSmoothing - 1)
                {
                    kValues.Add(null);
                    continue;
                }

                decimal sum = 0;
                for (int j = i - kSmoothing + 1; j <= i; j++)
                    sum += rawK[j]!.Value;
                kValues.Add(sum / kSmoothing);
            }

            // D line = SMA of K
            var dValues = new List<decimal?>();
            for (int i = 0; i < closes.Count; i++)
            {
                if (kValues[i] == null || i < kPeriod - 1 + kSmoothing - 1 + dPeriod - 1)
                {
                    dValues.Add(null);
                    continue;
                }

                decimal sum = 0;
                for (int j = i - dPeriod + 1; j <= i; j++)
                    sum += kValues[j]!.Value;
                dValues.Add(sum / dPeriod);
            }

            return (kValues, dValues);
        }

        public static List<decimal?> CalculateAtr(
            List<decimal> highs, List<decimal> lows, List<decimal> closes, int period = 14)
        {
            var trueRanges = new List<decimal?>();
            for (int i = 0; i < closes.Count; i++)
            {
                if (i == 0)
                {
                    trueRanges.Add(highs[i] - lows[i]);
                    continue;
                }

                var hl = highs[i] - lows[i];
                var hc = Math.Abs(highs[i] - closes[i - 1]);
                var lc = Math.Abs(lows[i] - closes[i - 1]);
                trueRanges.Add(Math.Max(hl, Math.Max(hc, lc)));
            }

            var actualTr = trueRanges.Select(v => v!.Value).ToList();
            return CalculateEma(actualTr, period);
        }
    }
}