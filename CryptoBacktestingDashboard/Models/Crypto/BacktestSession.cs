using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CryptoBacktestingDashboard.Models.Crypto
{
    public class BacktestSession
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string AppUserId { get; set; }

        [ForeignKey(nameof(AppUserId))]
        public virtual AppUser AppUser { get; set; }
        [Required(ErrorMessage = "Strategy is required")]
        [ForeignKey("Strategy")]
        public int StrategyId { get; set; }

        [Required(ErrorMessage = "Crypto pair is required")]
        [ForeignKey("CryptoPair")]
        public int CryptoPairId { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        public DateTime EndDate { get; set; }

        public DateTime ExecutedAt { get; set; }

        [Required(ErrorMessage = "Initial balance is required")]
        [Range(1, double.MaxValue, ErrorMessage = "Initial balance must be strictly positive.")]
        public decimal InitialBalance { get; set; }

        [Required(ErrorMessage = "Final balance is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Final balance cannot be negative.")]
        public decimal FinalBalance { get; set; }

        public bool IsOptimized { get; set; }

        // Foreign keys
        public virtual BacktestStrategy? Strategy { get; set; }
        public virtual CryptoPair? CryptoPair { get; set; }

        // 1-N relationship: Session has many results/trades
        public virtual ICollection<BacktestResult> Results { get; set; }

        // 1-N relationship: Session has many optimization runs
        public virtual ICollection<OptimizationRun> OptimizationRuns { get; set; }

        public BacktestSession()
        {
            Results = new List<BacktestResult>();
            OptimizationRuns = new List<OptimizationRun>();
            ExecutedAt = DateTime.Now;
            IsOptimized = false;
        }

        // A session only has a result once it's been run. A run always leaves a positive
        // FinalBalance (cash can't reach 0 in a long-only backtest), so FinalBalance == 0
        // means "not run yet" — show 0 profit/ROI instead of a phantom -100%.
        public bool HasRun => FinalBalance > 0;

        public decimal GetProfit()
        {
            if (!HasRun) return 0;
            return FinalBalance - InitialBalance;
        }

        public decimal GetROI()
        {
            if (!HasRun || InitialBalance == 0) return 0;
            return (GetProfit() / InitialBalance) * 100;
        }

        public override string ToString()
        {
            return $"Backtest: {CryptoPair?.Symbol} ({StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}) - Profit: ${GetProfit():F2} (ROI: {GetROI():F2}%)";
        }
    }
}
