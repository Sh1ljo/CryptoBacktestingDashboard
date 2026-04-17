using CryptoBacktestingDashboard.Models.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CryptoBacktestingDashboard.Repositories
{
    public class RiskManagementMockRepository
    {
        private static readonly List<RiskManagement> _riskManagements = new()
        {
            new RiskManagement
            {
                Id = 1,
                Name = "Conservative",
                Type = RiskManagementType.StopLoss,
                Value = 2m,
                Description = "2% stop loss on all trades - tight risk control",
                CreatedAt = new DateTime(2025, 01, 01),
                Strategies = new List<BacktestStrategy>()
            },
            new RiskManagement
            {
                Id = 2,
                Name = "Moderate Risk",
                Type = RiskManagementType.StopLoss,
                Value = 5m,
                Description = "5% stop loss for balanced risk/reward",
                CreatedAt = new DateTime(2025, 01, 02),
                Strategies = new List<BacktestStrategy>()
            },
            new RiskManagement
            {
                Id = 3,
                Name = "Profit Target",
                Type = RiskManagementType.TakeProfit,
                Value = 8m,
                Description = "8% take profit target for swing trades",
                CreatedAt = new DateTime(2025, 01, 03),
                Strategies = new List<BacktestStrategy>()
            },
            new RiskManagement
            {
                Id = 4,
                Name = "Trailing Stop",
                Type = RiskManagementType.TrailingStop,
                Value = 3m,
                Description = "3% trailing stop loss to lock in profits",
                CreatedAt = new DateTime(2025, 01, 04),
                Strategies = new List<BacktestStrategy>()
            },
            new RiskManagement
            {
                Id = 5,
                Name = "Fixed Position",
                Type = RiskManagementType.FixedPositionSize,
                Value = 0.1m,
                Description = "Fixed 0.1 BTC position size per trade",
                CreatedAt = new DateTime(2025, 01, 05),
                Strategies = new List<BacktestStrategy>()
            }
        };

        public List<RiskManagement> GetAll()
        {
            return _riskManagements;
        }

        public RiskManagement GetById(int id)
        {
            return _riskManagements.FirstOrDefault(r => r.Id == id);
        }

        public void Add(RiskManagement riskManagement)
        {
            riskManagement.Id = _riskManagements.Max(r => r.Id) + 1;
            _riskManagements.Add(riskManagement);
        }

        public void Delete(int id)
        {
            var riskManagement = GetById(id);
            if (riskManagement != null)
                _riskManagements.Remove(riskManagement);
        }
    }
}
