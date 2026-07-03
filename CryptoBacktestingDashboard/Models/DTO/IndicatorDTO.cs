using System.ComponentModel.DataAnnotations;
using CryptoBacktestingDashboard.Models.Crypto;

namespace CryptoBacktestingDashboard.Models.DTO
{
    public class IndicatorDTO
    {
        public int Id { get; set; }

        [Required]
        public string? Name { get; set; }

        public IndicatorType Type { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Period must be at least 1.")]
        public int Period { get; set; }

        public decimal Threshold { get; set; }

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
