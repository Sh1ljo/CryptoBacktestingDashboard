using System;
using System.Collections.Generic;

namespace CryptoBacktestingDashboard.Models.Crypto
{
    public enum RiskManagementType
    {
        StopLoss,
        TakeProfit,
        TrailingStop,
        FixedPositionSize,
        PercentageRisk
    }

    public class RiskManagement
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public RiskManagementType Type { get; set; }
        public decimal Value { get; set; } // Value in % or fixed amount
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; }

        // 1-N relationship: one risk management can be used in many strategies
        public List<BacktestStrategy> Strategies { get; set; }

        public RiskManagement()
        {
            Strategies = new List<BacktestStrategy>();
            CreatedAt = DateTime.Now;
        }

        public override string ToString()
        {
            return $"{Name} ({Type}) - Value: {Value}% - {Description}";
        }
    }
}
