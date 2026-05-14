using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CryptoBacktestingDashboard.Models.Crypto
{
    public class CryptoPair
    {
        [Key]
        public int Id { get; set; }
        [Required(ErrorMessage = "Symbol is required")]
        [StringLength(20, MinimumLength = 3, ErrorMessage = "Symbol must be between 3 and 20 characters")]
        public string? Symbol { get; set; } // e.g., BTC/USD

        [Required(ErrorMessage = "Base asset is required")]
        [StringLength(10, MinimumLength = 2, ErrorMessage = "Base asset must be between 2 and 10 characters")]
        public string? BaseAsset { get; set; } // e.g., BTC

        [Required(ErrorMessage = "Quote asset is required")]
        [StringLength(10, MinimumLength = 2, ErrorMessage = "Quote asset must be between 2 and 10 characters")]
        public string? QuoteAsset { get; set; } // e.g., USD

        [Required(ErrorMessage = "Current price is required")]
        [Range(0.00000001, double.MaxValue, ErrorMessage = "Current price must be greater than 0")]
        public decimal CurrentPrice { get; set; }
        public DateTime CreatedAt { get; set; }

        // 1-N relationship: one pair has many candle data points
        public virtual ICollection<CandleData> CandleDataHistory { get; set; }

        // N-N relationship: one pair can be used in many backtests
        public virtual ICollection<BacktestSession> BacktestSessions { get; set; }

        public CryptoPair()
        {
            CandleDataHistory = new List<CandleData>();
            BacktestSessions = new List<BacktestSession>();
            CreatedAt = DateTime.Now;
        }

        public override string ToString()
        {
            return $"{Symbol} - Current Price: ${CurrentPrice}";
        }
    }
}
