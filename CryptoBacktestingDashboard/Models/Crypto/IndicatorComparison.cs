using System;
using System.ComponentModel.DataAnnotations;

namespace CryptoBacktestingDashboard.Models.Crypto
{
    public enum ComparisonType
    {
        CrossoverAbove,
        CrossoverBelow,
        GreaterThan,
        LessThan,
        Equals
    }

    public enum IndicatorSignal
    {
        Buy,
        Sell
    }

    public class IndicatorComparison
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BacktestStrategyId { get; set; }

        [Required]
        public int IndicatorAId { get; set; }

        [Required]
        public int IndicatorBId { get; set; }

        [Required]
        public ComparisonType ComparisonType { get; set; }

        [Required]
        public IndicatorSignal TargetSignal { get; set; }

        public string? Name { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual BacktestStrategy? BacktestStrategy { get; set; }
        public virtual Indicator? IndicatorA { get; set; }
        public virtual Indicator? IndicatorB { get; set; }

        public IndicatorComparison()
        {
            CreatedAt = DateTime.Now;
        }

        public override string ToString()
        {
            return Name ?? $"Indicator {IndicatorAId} {ComparisonType} Indicator {IndicatorBId} → {TargetSignal}";
        }
    }
}
