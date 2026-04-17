using CryptoBacktestingDashboard.Models.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CryptoBacktestingDashboard.Repositories
{
    public class CryptoPairMockRepository
    {
        private static readonly List<CryptoPair> _pairs = new()
        {
            new CryptoPair
            {
                Id = 1,
                Symbol = "BTC/USD",
                BaseAsset = "BTC",
                QuoteAsset = "USD",
                CurrentPrice = 65432.50m,
                CreatedAt = new DateTime(2025, 01, 01),
                CandleDataHistory = new List<CandleData>(),
                BacktestSessions = new List<BacktestSession>()
            },
            new CryptoPair
            {
                Id = 2,
                Symbol = "ETH/USD",
                BaseAsset = "ETH",
                QuoteAsset = "USD",
                CurrentPrice = 3421.75m,
                CreatedAt = new DateTime(2025, 01, 05),
                CandleDataHistory = new List<CandleData>(),
                BacktestSessions = new List<BacktestSession>()
            },
            new CryptoPair
            {
                Id = 3,
                Symbol = "SOL/USD",
                BaseAsset = "SOL",
                QuoteAsset = "USD",
                CurrentPrice = 189.45m,
                CreatedAt = new DateTime(2025, 01, 10),
                CandleDataHistory = new List<CandleData>(),
                BacktestSessions = new List<BacktestSession>()
            }
        };

        public List<CryptoPair> GetAll()
        {
            return _pairs;
        }

        public CryptoPair GetById(int id)
        {
            return _pairs.FirstOrDefault(p => p.Id == id);
        }

        public void Add(CryptoPair pair)
        {
            pair.Id = _pairs.Max(p => p.Id) + 1;
            _pairs.Add(pair);
        }

        public void Delete(int id)
        {
            var pair = GetById(id);
            if (pair != null)
                _pairs.Remove(pair);
        }
    }
}
