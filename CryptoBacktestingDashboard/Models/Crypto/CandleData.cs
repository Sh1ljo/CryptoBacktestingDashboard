using System;

namespace CryptoBacktestingDashboard.Models.Crypto
{
    public class CandleData
    {
        public int Id { get; set; }
        public int CryptoPairId { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
        public DateTime OpenTime { get; set; }
        public DateTime CloseTime { get; set; }

        // Foreign key relationship
        public CryptoPair CryptoPair { get; set; }

        public CandleData()
        {
        }

        public override string ToString()
        {
            return $"Candle: O:{Open} H:{High} L:{Low} C:{Close} Vol:{Volume} at {OpenTime}";
        }
    }
}
