using System.ComponentModel.DataAnnotations;

namespace CryptoBacktestingDashboard.Models.DTO
{
    public class BacktestSessionDTO
    {
        public int Id { get; set; }

        [Required]
        public int StrategyId { get; set; }

        [Required]
        public int CryptoPairId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        public DateTime? ExecutedAt { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "InitialBalance must be greater than 0.")]
        public decimal InitialBalance { get; set; }

        public decimal FinalBalance { get; set; }

        public bool IsOptimized { get; set; }

        public decimal Profit { get; set; }

        public decimal ROI { get; set; }

        public BacktestStrategyDTO? Strategy { get; set; }

        public CryptoPairDTO? CryptoPair { get; set; }
    }
}
