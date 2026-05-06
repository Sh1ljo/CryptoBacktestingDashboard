using CryptoBacktestingDashboard.Models.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CryptoBacktestingDashboard.Repositories
{
    public class BacktestSessionMockRepository
    {
        private readonly BacktestStrategyMockRepository _strategyRepo;
        private readonly CryptoPairMockRepository _pairRepo;

        private static readonly List<BacktestSession> _sessions = new();

        public BacktestSessionMockRepository(
            BacktestStrategyMockRepository strategyRepo,
            CryptoPairMockRepository pairRepo)
        {
            _strategyRepo = strategyRepo;
            _pairRepo = pairRepo;
            InitializeSessions();
        }

        private void InitializeSessions()
        {
            if (_sessions.Count > 0)
                return;

            var btcPair = _pairRepo.GetById(1);
            var ethPair = _pairRepo.GetById(2);
            var solPair = _pairRepo.GetById(3);

            var strategy1 = _strategyRepo.GetById(1);
            var strategy2 = _strategyRepo.GetById(2);
            var strategy3 = _strategyRepo.GetById(3);

            // Session 1: BTC with RSI Strategy
            var session1 = new BacktestSession
            {
                Id = 1,
                StrategyId = strategy1.Id,
                Strategy = strategy1,
                CryptoPairId = btcPair.Id,
                CryptoPair = btcPair,
                StartDate = new DateTime(2025, 01, 01),
                EndDate = new DateTime(2025, 03, 31),
                ExecutedAt = new DateTime(2025, 04, 01, 10, 30, 0),
                InitialBalance = 10000m,
                FinalBalance = 12350m,
                IsOptimized = true,
                Results = new List<BacktestResult>
                {
                    new BacktestResult
                    {
                        Id = 1,
                        BacktestSessionId = 1,
                        TradeType = TradeType.Long,
                        EntryTime = new DateTime(2025, 01, 05),
                        ExitTime = new DateTime(2025, 01, 12),
                        EntryPrice = 63000m,
                        ExitPrice = 65000m,
                        Quantity = 0.1m,
                        Commission = 10m,
                        IsWinningTrade = true
                    },
                    new BacktestResult
                    {
                        Id = 2,
                        BacktestSessionId = 1,
                        TradeType = TradeType.Long,
                        EntryTime = new DateTime(2025, 01, 20),
                        ExitTime = new DateTime(2025, 02, 01),
                        EntryPrice = 64500m,
                        ExitPrice = 64000m,
                        Quantity = 0.1m,
                        Commission = 10m,
                        IsWinningTrade = false
                    },
                    new BacktestResult
                    {
                        Id = 3,
                        BacktestSessionId = 1,
                        TradeType = TradeType.Long,
                        EntryTime = new DateTime(2025, 02, 15),
                        ExitTime = new DateTime(2025, 03, 05),
                        EntryPrice = 62000m,
                        ExitPrice = 68000m,
                        Quantity = 0.15m,
                        Commission = 15m,
                        IsWinningTrade = true
                    }
                }
            };

            // Session 2: ETH with MACD Strategy
            var session2 = new BacktestSession
            {
                Id = 2,
                StrategyId = strategy2.Id,
                Strategy = strategy2,
                CryptoPairId = ethPair.Id,
                CryptoPair = ethPair,
                StartDate = new DateTime(2025, 01, 10),
                EndDate = new DateTime(2025, 03, 31),
                ExecutedAt = new DateTime(2025, 04, 02, 14, 15, 0),
                InitialBalance = 15000m,
                FinalBalance = 18750m,
                IsOptimized = false,
                Results = new List<BacktestResult>
                {
                    new BacktestResult
                    {
                        Id = 4,
                        BacktestSessionId = 2,
                        TradeType = TradeType.Long,
                        EntryTime = new DateTime(2025, 01, 15),
                        ExitTime = new DateTime(2025, 01, 25),
                        EntryPrice = 3200m,
                        ExitPrice = 3400m,
                        Quantity = 2m,
                        Commission = 20m,
                        IsWinningTrade = true
                    },
                    new BacktestResult
                    {
                        Id = 5,
                        BacktestSessionId = 2,
                        TradeType = TradeType.Long,
                        EntryTime = new DateTime(2025, 02, 01),
                        ExitTime = new DateTime(2025, 02, 15),
                        EntryPrice = 3300m,
                        ExitPrice = 3600m,
                        Quantity = 2.5m,
                        Commission = 25m,
                        IsWinningTrade = true
                    }
                }
            };

            // Session 3: SOL with Combined Strategy
            var session3 = new BacktestSession
            {
                Id = 3,
                StrategyId = strategy3.Id,
                Strategy = strategy3,
                CryptoPairId = solPair.Id,
                CryptoPair = solPair,
                StartDate = new DateTime(2025, 02, 01),
                EndDate = new DateTime(2025, 03, 31),
                ExecutedAt = new DateTime(2025, 04, 03, 09, 45, 0),
                InitialBalance = 20000m,
                FinalBalance = 19200m,
                IsOptimized = false,
                Results = new List<BacktestResult>
                {
                    new BacktestResult
                    {
                        Id = 6,
                        BacktestSessionId = 3,
                        TradeType = TradeType.Long,
                        EntryTime = new DateTime(2025, 02, 05),
                        ExitTime = new DateTime(2025, 02, 15),
                        EntryPrice = 180m,
                        ExitPrice = 175m,
                        Quantity = 20m,
                        Commission = 30m,
                        IsWinningTrade = false
                    },
                    new BacktestResult
                    {
                        Id = 7,
                        BacktestSessionId = 3,
                        TradeType = TradeType.Short,
                        EntryTime = new DateTime(2025, 02, 20),
                        ExitTime = new DateTime(2025, 03, 10),
                        EntryPrice = 185m,
                        ExitPrice = 190m,
                        Quantity = 15m,
                        Commission = 25m,
                        IsWinningTrade = false
                    }
                }
            };

            _sessions.AddRange(new[] { session1, session2, session3 });
        }

        public List<BacktestSession> GetAll()
        {
            return _sessions;
        }

        public BacktestSession? GetById(int id)
        {
            return _sessions.FirstOrDefault(s => s.Id == id);
        }

        public void Add(BacktestSession session)
        {
            session.Id = _sessions.Max(s => s.Id) + 1;
            _sessions.Add(session);
        }

        public void Delete(int id)
        {
            var session = GetById(id);
            if (session != null)
                _sessions.Remove(session);
        }
    }
}
