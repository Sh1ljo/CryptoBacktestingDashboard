using System;
using System.Collections.Generic;

namespace CryptoBacktestingDashboard.Models.Crypto
{
    public class CryptoPair
    {
        public int Id { get; set; }
        public string Symbol { get; set; } // e.g., BTC/USD
        public string BaseAsset { get; set; } // e.g., BTC
        public string QuoteAsset { get; set; } // e.g., USD
        public decimal CurrentPrice { get; set; }
        public DateTime CreatedAt { get; set; }

        // 1-N relationship: one pair has many candle data points
        public List<CandleData> CandleDataHistory { get; set; }

        // N-N relationship: one pair can be used in many backtests
        public List<BacktestSession> BacktestSessions { get; set; }

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
