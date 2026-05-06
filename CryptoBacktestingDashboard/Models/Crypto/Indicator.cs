using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CryptoBacktestingDashboard.Models.Crypto
{
    public enum IndicatorType
    {
        RSI,
        MACD,
        MovingAverage,
        BollingerBands,
        Stochastic,
        ATR
    }

    public class Indicator
    {
        [Key]
        public int Id { get; set; }
        public string? Name { get; set; }
        public IndicatorType Type { get; set; }
        public int Period { get; set; }
        public decimal Threshold { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }

        // N-N relationship: Indicator can be used in many strategies
        public virtual ICollection<BacktestStrategy> Strategies { get; set; }

        public Indicator()
        {
            Strategies = new List<BacktestStrategy>();
            CreatedAt = DateTime.Now;
        }

        public override string ToString()
        {
            return $"{Name} ({Type}) - Period: {Period}, Threshold: {Threshold}";
        }
    }
}
