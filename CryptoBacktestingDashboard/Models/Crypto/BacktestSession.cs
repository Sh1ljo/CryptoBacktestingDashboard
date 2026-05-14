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

        public BacktestSession()
        {
            Results = new List<BacktestResult>();
            ExecutedAt = DateTime.Now;
            IsOptimized = false;
        }

        public decimal GetProfit()
        {
            return FinalBalance - InitialBalance;
        }

        public decimal GetROI()
        {
            if (InitialBalance == 0) return 0;
            return (GetProfit() / InitialBalance) * 100;
        }

        public override string ToString()
        {
            return $"Backtest: {CryptoPair?.Symbol} ({StartDate:yyyy-MM-dd} to {EndDate:yyyy-MM-dd}) - Profit: ${GetProfit():F2} (ROI: {GetROI():F2}%)";
        }
    }
}
