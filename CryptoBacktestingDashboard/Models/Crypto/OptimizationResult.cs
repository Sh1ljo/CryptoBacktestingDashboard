using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoBacktestingDashboard.Models.Crypto
{
    // Stores the result of a single parameter combination tested during an OptimizationRun.
    public class OptimizationResult
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("OptimizationRun")]
        public int OptimizationRunId { get; set; }

        // The parameter values used (serialized Dictionary<string, decimal>)
        public string ParamsJson { get; set; } = "{}";

        public double CompositeScore { get; set; }

        public decimal Profit { get; set; }
        public decimal ProfitFactor { get; set; }
        public decimal WinRate { get; set; }
        public decimal MaxDrawdownPercent { get; set; }
        public int TotalTrades { get; set; }
        public decimal SharpeApprox { get; set; }

        public virtual OptimizationRun? OptimizationRun { get; set; }
    }
}
