using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoBacktestingDashboard.Models.Crypto
{
    public enum TradeDirection
    {
        LongOnly = 0,
        ShortOnly = 1,
        Both = 2
    }

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

        // --- Built-in risk management (every strategy carries its own) ---
        // Required: exit a losing trade once price drops this % below entry.
        [Range(0.1, 99, ErrorMessage = "Stop Loss must be between 0.1% and 99%.")]
        public decimal StopLossPercent { get; set; } = 5m;

        // Required: exit a winning trade once price rises this % above entry.
        [Range(0.1, 1000, ErrorMessage = "Take Profit must be greater than 0.")]
        public decimal TakeProfitPercent { get; set; } = 10m;

        // Optional: trail the stop this % below the highest price seen since entry.
        public decimal? TrailingStopPercent { get; set; }

        // Optional: % of available cash to deploy per trade (null/0 = 100%).
        public decimal? PositionSizePercent { get; set; }

        // Which position directions the engine is allowed to open.
        // LongOnly (default): only buy entries. ShortOnly: only sell entries.
        // Both: opens longs on Buy signals and shorts on Sell signals.
        public TradeDirection TradeDirection { get; set; } = TradeDirection.LongOnly;

        // Reward-to-risk ratio for display (e.g. TP 10% / SL 5% = 2.0).
        [NotMapped]
        public decimal RiskRewardRatio =>
            StopLossPercent > 0 ? Math.Round(TakeProfitPercent / StopLossPercent, 2) : 0;

        // N-N relationship: Strategy uses many indicators, an indicator can be in many strategies
        public virtual ICollection<Indicator> Indicators { get; set; }

        // 1-N relationship: Strategy has many indicator comparisons
        public virtual ICollection<IndicatorComparison> Comparisons { get; set; }

        // 1-N relationship: Strategy has many backtest sessions
        public virtual ICollection<BacktestSession> BacktestSessions { get; set; }

        public virtual ICollection<Attachment> Attachments { get; set; }

        public BacktestStrategy()
        {
            Indicators = new List<Indicator>();
            Comparisons = new List<IndicatorComparison>();
            BacktestSessions = new List<BacktestSession>();
            Attachments = new List<Attachment>();
            CreatedAt = DateTime.Now;
            IsActive = true;
        }

        public override string ToString()
        {
            return $"Strategy: {Name} - Initial Capital: ${InitialCapital} - Active: {IsActive}";
        }
    }
}
