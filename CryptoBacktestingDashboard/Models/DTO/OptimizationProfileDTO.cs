using System.Collections.Generic;

namespace CryptoBacktestingDashboard.Models.DTO
{
    // Form-bound input describing the sweep ranges the user configured on the Optimize page.
    public class OptimizationProfileDTO
    {
        public List<IndicatorSweepDTO> Indicators { get; set; } = new();

        public decimal? SlMin { get; set; }
        public decimal? SlMax { get; set; }
        public decimal? SlStep { get; set; }

        public decimal? TpMin { get; set; }
        public decimal? TpMax { get; set; }
        public decimal? TpStep { get; set; }

        public decimal? TsMin { get; set; }
        public decimal? TsMax { get; set; }
        public decimal? TsStep { get; set; }

        public decimal? PsMin { get; set; }
        public decimal? PsMax { get; set; }
        public decimal? PsStep { get; set; }
    }

    public class IndicatorSweepDTO
    {
        public int IndicatorId { get; set; }

        public decimal? PeriodMin { get; set; }
        public decimal? PeriodMax { get; set; }
        public decimal? PeriodStep { get; set; }

        public decimal? ThresholdMin { get; set; }
        public decimal? ThresholdMax { get; set; }
        public decimal? ThresholdStep { get; set; }
    }
}
