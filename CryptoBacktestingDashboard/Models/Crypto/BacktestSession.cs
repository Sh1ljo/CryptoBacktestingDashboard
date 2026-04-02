using System;
using System.Collections.Generic;

namespace CryptoBacktestingDashboard.Models.Crypto
{
    public class BacktestSession
    {
        public int Id { get; set; }
        public int StrategyId { get; set; }
        public int CryptoPairId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime ExecutedAt { get; set; }
        public decimal InitialBalance { get; set; }
        public decimal FinalBalance { get; set; }
        public bool IsOptimized { get; set; }

        // Foreign keys
        public BacktestStrategy Strategy { get; set; }
        public CryptoPair CryptoPair { get; set; }

        // 1-N relationship: Session has many results/trades
        public List<BacktestResult> Results { get; set; }

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
