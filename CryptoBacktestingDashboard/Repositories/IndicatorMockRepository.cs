using CryptoBacktestingDashboard.Models.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CryptoBacktestingDashboard.Repositories
{
    public class IndicatorMockRepository
    {
        private static readonly List<Indicator> _indicators = new()
        {
            new Indicator
            {
                Id = 1,
                Name = "RSI Indicator",
                Type = IndicatorType.RSI,
                Period = 14,
                Threshold = 30m,
                Description = "Relative Strength Index for overbought/oversold detection",
                CreatedAt = new DateTime(2025, 01, 01),
                Strategies = new List<BacktestStrategy>()
            },
            new Indicator
            {
                Id = 2,
                Name = "MACD Indicator",
                Type = IndicatorType.MACD,
                Period = 12,
                Threshold = 0m,
                Description = "Moving Average Convergence Divergence for trend detection",
                CreatedAt = new DateTime(2025, 01, 02),
                Strategies = new List<BacktestStrategy>()
            },
            new Indicator
            {
                Id = 3,
                Name = "Moving Average",
                Type = IndicatorType.MovingAverage,
                Period = 50,
                Threshold = 0m,
                Description = "50-period Simple Moving Average",
                CreatedAt = new DateTime(2025, 01, 03),
                Strategies = new List<BacktestStrategy>()
            },
            new Indicator
            {
                Id = 4,
                Name = "Bollinger Bands",
                Type = IndicatorType.BollingerBands,
                Period = 20,
                Threshold = 2m,
                Description = "Bollinger Bands for volatility analysis",
                CreatedAt = new DateTime(2025, 01, 04),
                Strategies = new List<BacktestStrategy>()
            },
            new Indicator
            {
                Id = 5,
                Name = "Stochastic Oscillator",
                Type = IndicatorType.Stochastic,
                Period = 14,
                Threshold = 20m,
                Description = "Stochastic for momentum analysis",
                CreatedAt = new DateTime(2025, 01, 05),
                Strategies = new List<BacktestStrategy>()
            },
            new Indicator
            {
                Id = 6,
                Name = "ATR",
                Type = IndicatorType.ATR,
                Period = 14,
                Threshold = 0m,
                Description = "Average True Range for volatility measurement",
                CreatedAt = new DateTime(2025, 01, 06),
                Strategies = new List<BacktestStrategy>()
            }
        };

        public List<Indicator> GetAll()
        {
            return _indicators;
        }

        public Indicator GetById(int id)
        {
            return _indicators.FirstOrDefault(i => i.Id == id);
        }

        public void Add(Indicator indicator)
        {
            indicator.Id = _indicators.Max(i => i.Id) + 1;
            _indicators.Add(indicator);
        }

        public void Delete(int id)
        {
            var indicator = GetById(id);
            if (indicator != null)
                _indicators.Remove(indicator);
        }
    }
}
