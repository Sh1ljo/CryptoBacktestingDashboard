using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoBacktestingDashboard.Models.Crypto
{
    public class OptimizationRun
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("BacktestSession")]
        public int BacktestSessionId { get; set; }

        // The sweep ranges submitted by the user (serialized OptimizationProfile)
        public string ParameterGridJson { get; set; } = "{}";

        // The winning combination (serialized Dictionary<string, decimal>)
        public string? BestParamsJson { get; set; }

        public double BestCompositeScore { get; set; }

        public int TotalCombinations { get; set; }
        public int CompletedCombinations { get; set; }

        // running | completed | failed
        public string Status { get; set; } = "running";
        public string? ErrorMessage { get; set; }

        public DateTime RanAt { get; set; }

        public virtual BacktestSession? BacktestSession { get; set; }
        public virtual ICollection<OptimizationResult> Results { get; set; }

        public OptimizationRun()
        {
            Results = new List<OptimizationResult>();
            RanAt = DateTime.Now;
        }
    }
}
