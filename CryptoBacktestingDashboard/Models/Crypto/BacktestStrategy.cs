using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoBacktestingDashboard.Models.Crypto
{
    public class BacktestStrategy
    {
        [Key]
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public decimal InitialCapital { get; set; }
        public int LookbackPeriod { get; set; } // number of candles to look back
        public DateTime CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }

        // N-N relationship: Strategy uses many indicators, an indicator can be in many strategies
        public virtual ICollection<Indicator> Indicators { get; set; }

        // 1-N relationship: Strategy has one risk management rule
        [ForeignKey("RiskManagement")]
        public int RiskManagementId { get; set; }
        public virtual RiskManagement? RiskManagement { get; set; }

        // 1-N relationship: Strategy has many backtest sessions
        public virtual ICollection<BacktestSession> BacktestSessions { get; set; }

        public BacktestStrategy()
        {
            Indicators = new List<Indicator>();
            BacktestSessions = new List<BacktestSession>();
            CreatedAt = DateTime.Now;
            IsActive = true;
        }

        public override string ToString()
        {
            return $"Strategy: {Name} - Initial Capital: ${InitialCapital} - Active: {IsActive}";
        }
    }
}
