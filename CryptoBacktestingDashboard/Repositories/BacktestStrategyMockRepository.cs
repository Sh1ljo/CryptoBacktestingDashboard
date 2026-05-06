using CryptoBacktestingDashboard.Models.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CryptoBacktestingDashboard.Repositories
{
    public class BacktestStrategyMockRepository
    {
        private readonly IndicatorMockRepository _indicatorRepo;
        private readonly RiskManagementMockRepository _riskMgmtRepo;

        private static readonly List<BacktestStrategy> _strategies = new();

        public BacktestStrategyMockRepository(
            IndicatorMockRepository indicatorRepo,
            RiskManagementMockRepository riskMgmtRepo)
        {
            _indicatorRepo = indicatorRepo;
            _riskMgmtRepo = riskMgmtRepo;
            InitializeStrategies();
        }

        private void InitializeStrategies()
        {
            if (_strategies.Count > 0)
                return;

            var rsiIndicator = _indicatorRepo.GetById(1);
            var macdIndicator = _indicatorRepo.GetById(2);
            var maIndicator = _indicatorRepo.GetById(3);
            var stochastic = _indicatorRepo.GetById(5);

            var conservativeRisk = _riskMgmtRepo.GetById(1);
            var moderateRisk = _riskMgmtRepo.GetById(2);

            _strategies.Add(new BacktestStrategy
            {
                Id = 1,
                Name = "RSI Overbought/Oversold",
                Description = "Uses RSI indicator to identify overbought and oversold conditions for mean reversion trades",
                IsActive = true,
                InitialCapital = 10000m,
                LookbackPeriod = 50,
                CreatedAt = new DateTime(2025, 01, 01),
                RiskManagementId = conservativeRisk.Id,
                RiskManagement = conservativeRisk,
                Indicators = new List<Indicator> { rsiIndicator },
                BacktestSessions = new List<BacktestSession>()
            });

            _strategies.Add(new BacktestStrategy
            {
                Id = 2,
                Name = "MACD Trend Following",
                Description = "Follows trends using MACD crossovers combined with moving averages",
                IsActive = true,
                InitialCapital = 15000m,
                LookbackPeriod = 100,
                CreatedAt = new DateTime(2025, 01, 02),
                RiskManagementId = moderateRisk.Id,
                RiskManagement = moderateRisk,
                Indicators = new List<Indicator> { macdIndicator, maIndicator },
                BacktestSessions = new List<BacktestSession>()
            });

            _strategies.Add(new BacktestStrategy
            {
                Id = 3,
                Name = "Combined Technical Analysis",
                Description = "Combines RSI, Stochastic, and MACD for robust entry and exit signals",
                IsActive = true,
                InitialCapital = 20000m,
                LookbackPeriod = 75,
                CreatedAt = new DateTime(2025, 01, 05),
                RiskManagementId = moderateRisk.Id,
                RiskManagement = moderateRisk,
                Indicators = new List<Indicator> { rsiIndicator, stochastic, macdIndicator },
                BacktestSessions = new List<BacktestSession>()
            });

            _strategies.Add(new BacktestStrategy
            {
                Id = 4,
                Name = "Moving Average Crossover",
                Description = "Simple but effective strategy using 50 and 200 period moving averages",
                IsActive = false,
                InitialCapital = 5000m,
                LookbackPeriod = 200,
                CreatedAt = new DateTime(2025, 01, 08),
                RiskManagementId = conservativeRisk.Id,
                RiskManagement = conservativeRisk,
                Indicators = new List<Indicator> { maIndicator },
                BacktestSessions = new List<BacktestSession>()
            });
        }

        public List<BacktestStrategy> GetAll()
        {
            return _strategies;
        }

        public BacktestStrategy? GetById(int id)
        {
            return _strategies.FirstOrDefault(s => s.Id == id);
        }

        public void Add(BacktestStrategy strategy)
        {
            strategy.Id = _strategies.Max(s => s.Id) + 1;
            _strategies.Add(strategy);
        }

        public void Delete(int id)
        {
            var strategy = GetById(id);
            if (strategy != null)
                _strategies.Remove(strategy);
        }
    }
}
