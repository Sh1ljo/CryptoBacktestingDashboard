using System;

namespace CryptoBacktestingDashboard.Models.Crypto
{
    public enum TradeType
    {
        Long,
        Short
    }

    public class BacktestResult
    {
        public int Id { get; set; }
        public int BacktestSessionId { get; set; }
        public TradeType TradeType { get; set; }
        public DateTime EntryTime { get; set; }
        public DateTime ExitTime { get; set; }
        public decimal EntryPrice { get; set; }
        public decimal ExitPrice { get; set; }
        public decimal Quantity { get; set; }
        public decimal Commission { get; set; }
        public bool IsWinningTrade { get; set; }

        // Foreign key
        public BacktestSession BacktestSession { get; set; }

        public BacktestResult()
        {
        }

        public decimal GetProfit()
        {
            decimal profitLoss = 0;
            if (TradeType == TradeType.Long)
            {
                profitLoss = (ExitPrice - EntryPrice) * Quantity;
            }
            else
            {
                profitLoss = (EntryPrice - ExitPrice) * Quantity;
            }
            return profitLoss - Commission;
        }

        public decimal GetProfitPercent()
        {
            if (EntryPrice == 0) return 0;
            decimal profit = 0;
            if (TradeType == TradeType.Long)
            {
                profit = ((ExitPrice - EntryPrice) / EntryPrice) * 100;
            }
            else
            {
                profit = ((EntryPrice - ExitPrice) / EntryPrice) * 100;
            }
            return profit;
        }

        public override string ToString()
        {
            return $"Trade: {TradeType} - Entry: ${EntryPrice} Exit: ${ExitPrice} - P/L: ${GetProfit():F2} ({GetProfitPercent():F2}%)";
        }
    }
}
