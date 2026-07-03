using System;
using System.Collections.Generic;

namespace CryptoBacktestingDashboard.Models.Crypto
{
    // A range of values to sweep, e.g. Period from 7 to 21 in steps of 2.
    public class ParameterSweep
    {
        public decimal Min { get; set; }
        public decimal Max { get; set; }
        public decimal Step { get; set; }

        // Expands the range into a deduplicated, ascending list of values.
        public List<decimal> Expand()
        {
            var values = new List<decimal>();
            if (Step <= 0 || Max <= Min)
            {
                values.Add(Min);
                return values;
            }

            for (var v = Min; v <= Max; v += Step)
                values.Add(v);

            if (values[^1] != Max)
                values.Add(Max);

            return new List<decimal>(new SortedSet<decimal>(values));
        }
    }

    // Period and/or threshold ranges to sweep for a single indicator.
    public class IndicatorSweep
    {
        public ParameterSweep? PeriodSweep { get; set; }
        public ParameterSweep? ThresholdSweep { get; set; }
    }

    // Input to OptimizationService: which parameters to sweep and over what ranges.
    public class OptimizationProfile
    {
        // Keyed by Indicator.Id
        public Dictionary<int, IndicatorSweep> IndicatorSweeps { get; set; } = new();

        public ParameterSweep? StopLossSweep { get; set; }
        public ParameterSweep? TakeProfitSweep { get; set; }
        public ParameterSweep? TrailingStopSweep { get; set; }
        public ParameterSweep? PositionSizeSweep { get; set; }
    }
}
