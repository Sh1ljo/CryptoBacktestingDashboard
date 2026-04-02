using CryptoBacktestingDashboard.Models.Crypto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Crypto Backtesting Dashboard - Lab 1 ===\n");

            // Initialize data
            var (pairs, indicators, riskManagements, strategies, sessions) = InitializeData();

            // Display initialized data
            Console.WriteLine("### INITIALIZED DATA ###\n");
            DisplayInitializedData(pairs, indicators, riskManagements, strategies, sessions);

            // Execute LINQ queries
            Console.WriteLine("\n### LINQ QUERIES ###\n");
            ExecuteLINQQueries(pairs, indicators, riskManagements, strategies, sessions);

            // Execute async operations
            Console.WriteLine("\n### ASYNC OPERATIONS ###\n");
            await ExecuteAsyncOperations(strategies, sessions);

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        /// <summary>
        /// Initializes 3 main crypto pairs with complete trading data, strategies, and backtests.
        /// Demonstrates 1-N and N-N relationships as per Lab-1 requirements.
        /// </summary>
        static (List<CryptoPair>, List<Indicator>, List<RiskManagement>, List<BacktestStrategy>, List<BacktestSession>) InitializeData()
        {
            // Initialize 3 main CryptoPair objects
            var bitcoinPair = new CryptoPair
            {
                Id = 1,
                Symbol = "BTC/USD",
                BaseAsset = "BTC",
                QuoteAsset = "USD",
                CurrentPrice = 65432.50m,
            };

            var ethereumPair = new CryptoPair
            {
                Id = 2,
                Symbol = "ETH/USD",
                BaseAsset = "ETH",
                QuoteAsset = "USD",
                CurrentPrice = 3421.75m,
            };

            var solonaPair = new CryptoPair
            {
                Id = 3,
                Symbol = "SOL/USD",
                BaseAsset = "SOL",
                QuoteAsset = "USD",
                CurrentPrice = 189.45m,
            };

            var pairs = new List<CryptoPair> { bitcoinPair, ethereumPair, solonaPair };

            // Add candle data for each pair (1-N relationship)
            AddCandleDataToPair(bitcoinPair, 50);
            AddCandleDataToPair(ethereumPair, 40);
            AddCandleDataToPair(solonaPair, 35);

            // Create Indicators
            var rsiIndicator = new Indicator
            {
                Id = 1,
                Name = "RSI Indicator",
                Type = IndicatorType.RSI,
                Period = 14,
                Threshold = 30m,
                Description = "Relative Strength Index for overbought/oversold detection"
            };

            var macdIndicator = new Indicator
            {
                Id = 2,
                Name = "MACD Indicator",
                Type = IndicatorType.MACD,
                Period = 12,
                Threshold = 0m,
                Description = "Moving Average Convergence Divergence for trend detection"
            };

            var maIndicator = new Indicator
            {
                Id = 3,
                Name = "Moving Average",
                Type = IndicatorType.MovingAverage,
                Period = 50,
                Threshold = 0m,
                Description = "50-period Simple Moving Average"
            };

            var bollingerBandsIndicator = new Indicator
            {
                Id = 4,
                Name = "Bollinger Bands",
                Type = IndicatorType.BollingerBands,
                Period = 20,
                Threshold = 2m,
                Description = "Bollinger Bands for volatility analysis"
            };

            var indicators = new List<Indicator> { rsiIndicator, macdIndicator, maIndicator, bollingerBandsIndicator };

            // Create Risk Management rules
            var strictStopLoss = new RiskManagement
            {
                Id = 1,
                Name = "Strict Stop Loss",
                Type = RiskManagementType.StopLoss,
                Value = 2m,
                Description = "2% stop loss on all trades"
            };

            var profitTarget = new RiskManagement
            {
                Id = 2,
                Name = "Profit Target",
                Type = RiskManagementType.TakeProfit,
                Value = 5m,
                Description = "5% take profit target"
            };

            var trailingStop = new RiskManagement
            {
                Id = 3,
                Name = "Trailing Stop",
                Type = RiskManagementType.TrailingStop,
                Value = 3m,
                Description = "3% trailing stop loss"
            };

            var riskManagements = new List<RiskManagement> { strictStopLoss, profitTarget, trailingStop };

            // Create 3 main BacktestStrategy objects
            var meanReversionStrategy = new BacktestStrategy
            {
                Id = 1,
                Name = "Mean Reversion Strategy",
                Description = "Trades when price deviates from moving average",
                InitialCapital = 10000m,
                LookbackPeriod = 50,
                RiskManagementId = 1,
                RiskManagement = strictStopLoss
            };
            meanReversionStrategy.Indicators.AddRange(new[] { rsiIndicator, maIndicator });

            var trendFollowingStrategy = new BacktestStrategy
            {
                Id = 2,
                Name = "Trend Following Strategy",
                Description = "Follows the trend using MACD and moving averages",
                InitialCapital = 15000m,
                LookbackPeriod = 100,
                RiskManagementId = 2,
                RiskManagement = profitTarget
            };
            trendFollowingStrategy.Indicators.AddRange(new[] { macdIndicator, maIndicator });

            var volatilityStrategy = new BacktestStrategy
            {
                Id = 3,
                Name = "Volatility Breakout Strategy",
                Description = "Trades breakouts using Bollinger Bands",
                InitialCapital = 8000m,
                LookbackPeriod = 30,
                RiskManagementId = 3,
                RiskManagement = trailingStop
            };
            volatilityStrategy.Indicators.AddRange(new[] { bollingerBandsIndicator, rsiIndicator });

            var strategies = new List<BacktestStrategy> { meanReversionStrategy, trendFollowingStrategy, volatilityStrategy };

            // Create BacktestSession objects (N-N relationship between Strategy and CryptoPair)
            var btcMeanReversionSession = new BacktestSession
            {
                Id = 1,
                StrategyId = 1,
                CryptoPairId = 1,
                Strategy = meanReversionStrategy,
                CryptoPair = bitcoinPair,
                StartDate = new DateTime(2025, 01, 01),
                EndDate = new DateTime(2025, 03, 31),
                InitialBalance = 10000m,
                FinalBalance = 12500m
            };
            AddBacktestResults(btcMeanReversionSession, 25);

            var ethTrendFollowingSession = new BacktestSession
            {
                Id = 2,
                StrategyId = 2,
                CryptoPairId = 2,
                Strategy = trendFollowingStrategy,
                CryptoPair = ethereumPair,
                StartDate = new DateTime(2025, 01, 01),
                EndDate = new DateTime(2025, 03, 31),
                InitialBalance = 15000m,
                FinalBalance = 18300m
            };
            AddBacktestResults(ethTrendFollowingSession, 35);

            var solVolatilitySession = new BacktestSession
            {
                Id = 3,
                StrategyId = 3,
                CryptoPairId = 3,
                Strategy = volatilityStrategy,
                CryptoPair = solonaPair,
                StartDate = new DateTime(2025, 01, 01),
                EndDate = new DateTime(2025, 03, 31),
                InitialBalance = 8000m,
                FinalBalance = 9240m
            };
            AddBacktestResults(solVolatilitySession, 18);

            var sessions = new List<BacktestSession> { btcMeanReversionSession, ethTrendFollowingSession, solVolatilitySession };

            // Add sessions to pairs and strategies
            bitcoinPair.BacktestSessions.Add(btcMeanReversionSession);
            ethereumPair.BacktestSessions.Add(ethTrendFollowingSession);
            solonaPair.BacktestSessions.Add(solVolatilitySession);

            meanReversionStrategy.BacktestSessions.Add(btcMeanReversionSession);
            trendFollowingStrategy.BacktestSessions.Add(ethTrendFollowingSession);
            volatilityStrategy.BacktestSessions.Add(solVolatilitySession);

            return (pairs, indicators, riskManagements, strategies, sessions);
        }

        static void AddCandleDataToPair(CryptoPair pair, int candleCount)
        {
            var random = new Random();
            var currentPrice = pair.CurrentPrice;

            for (int i = 0; i < candleCount; i++)
            {
                var openTime = new DateTime(2025, 01, 01).AddDays(i);
                var open = currentPrice;
                var close = currentPrice + (decimal)(random.NextDouble() - 0.5) * 1000;
                var high = Math.Max(open, close) + (decimal)random.NextDouble() * 500;
                var low = Math.Min(open, close) - (decimal)random.NextDouble() * 500;

                pair.CandleDataHistory.Add(new CandleData
                {
                    Id = i + 1,
                    CryptoPairId = pair.Id,
                    CryptoPair = pair,
                    Open = open,
                    High = high,
                    Low = low,
                    Close = close,
                    Volume = (decimal)(random.Next(1000, 5000)),
                    OpenTime = openTime,
                    CloseTime = openTime.AddHours(23).AddMinutes(59)
                });

                currentPrice = close;
            }
        }

        static void AddBacktestResults(BacktestSession session, int tradeCount)
        {
            var random = new Random();
            var currentDate = session.StartDate;

            for (int i = 0; i < tradeCount; i++)
            {
                var entryPrice = (decimal)(40000 + random.NextDouble() * 10000);
                var exitPrice = entryPrice + (decimal)(random.NextDouble() - 0.5) * 2000;
                var isWinning = exitPrice > entryPrice;

                session.Results.Add(new BacktestResult
                {
                    Id = i + 1,
                    BacktestSessionId = session.Id,
                    BacktestSession = session,
                    TradeType = random.Next(2) == 0 ? TradeType.Long : TradeType.Short,
                    EntryTime = currentDate,
                    ExitTime = currentDate.AddHours(random.Next(1, 12)),
                    EntryPrice = entryPrice,
                    ExitPrice = exitPrice,
                    Quantity = (decimal)(random.Next(1, 5)),
                    Commission = (decimal)(random.NextDouble() * 100),
                    IsWinningTrade = isWinning
                });

                currentDate = currentDate.AddDays(random.Next(1, 4));
            }
        }

        static void DisplayInitializedData(List<CryptoPair> pairs, List<Indicator> indicators, 
            List<RiskManagement> riskManagements, List<BacktestStrategy> strategies, List<BacktestSession> sessions)
        {
            Console.WriteLine("1. CRYPTO PAIRS:");
            foreach (var pair in pairs)
            {
                Console.WriteLine($"   {pair}");
                Console.WriteLine($"      Candle Data Points: {pair.CandleDataHistory.Count}");
                Console.WriteLine($"      Backtest Sessions: {pair.BacktestSessions.Count}");
            }

            Console.WriteLine("\n2. INDICATORS:");
            foreach (var indicator in indicators)
            {
                Console.WriteLine($"   {indicator}");
            }

            Console.WriteLine("\n3. RISK MANAGEMENT RULES:");
            foreach (var rm in riskManagements)
            {
                Console.WriteLine($"   {rm}");
            }

            Console.WriteLine("\n4. BACKTEST STRATEGIES:");
            foreach (var strategy in strategies)
            {
                Console.WriteLine($"   {strategy}");
                Console.WriteLine($"      Indicators Used: {string.Join(", ", strategy.Indicators.Select(i => i.Name))}");
                Console.WriteLine($"      Risk Management: {strategy.RiskManagement.Name}");
                Console.WriteLine($"      Backtest Sessions: {strategy.BacktestSessions.Count}");
            }

            Console.WriteLine("\n5. BACKTEST SESSIONS:");
            foreach (var session in sessions)
            {
                Console.WriteLine($"   {session}");
                Console.WriteLine($"      Total Trades: {session.Results.Count}");
            }
        }

        static void ExecuteLINQQueries(List<CryptoPair> pairs, List<Indicator> indicators, 
            List<RiskManagement> riskManagements, List<BacktestStrategy> strategies, List<BacktestSession> sessions)
        {
            // Query 1: Find all profitable backtest sessions
            Console.WriteLine("QUERY 1: Profitable Backtest Sessions (ROI > 10%)");
            var profitableSessions = sessions
                .Where(s => s.GetROI() > 10m)
                .OrderByDescending(s => s.GetROI())
                .ToList();
            
            foreach (var session in profitableSessions)
            {
                Console.WriteLine($"   {session.CryptoPair.Symbol} - ROI: {session.GetROI():F2}%");
            }

            // Query 2: Get strategies with their most profitable pair
            Console.WriteLine("\nQUERY 2: Strategies with Winning Trade Percentages");
            var strategyStats = strategies
                .SelectMany(s => s.BacktestSessions, (strategy, session) => new { strategy, session })
                .GroupBy(x => x.strategy.Name)
                .Select(g => new
                {
                    StrategyName = g.Key,
                    TotalTrades = g.SelectMany(x => x.session.Results).Count(),
                    WinningTrades = g.SelectMany(x => x.session.Results).Where(r => r.IsWinningTrade).Count()
                })
                .ToList();

            foreach (var stat in strategyStats)
            {
                var winPercentage = stat.TotalTrades > 0 ? (stat.WinningTrades * 100.0 / stat.TotalTrades) : 0;
                Console.WriteLine($"   {stat.StrategyName}: {winPercentage:F2}% Win Rate ({stat.WinningTrades}/{stat.TotalTrades})");
            }

            // Query 3: Indicators used in profitable strategies
            Console.WriteLine("\nQUERY 3: Indicators Used in Profitable Strategies (ROI > 15%)");
            var indicatorsInProfitable = strategies
                .Where(s => s.BacktestSessions.Any(bs => bs.GetROI() > 15m))
                .SelectMany(s => s.Indicators)
                .Distinct()
                .OrderBy(i => i.Name)
                .ToList();

            foreach (var indicator in indicatorsInProfitable)
            {
                Console.WriteLine($"   {indicator.Name} ({indicator.Type})");
            }

            // Query 4: Best performing pair
            Console.WriteLine("\nQUERY 4: Best Performing Crypto Pair (Highest Total Profit)");
            var bestPair = pairs
                .Select(p => new
                {
                    Pair = p,
                    TotalProfit = p.BacktestSessions.Sum(s => s.GetProfit()),
                    SessionCount = p.BacktestSessions.Count()
                })
                .OrderByDescending(x => x.TotalProfit)
                .FirstOrDefault();

            if (bestPair != null)
            {
                Console.WriteLine($"   {bestPair.Pair.Symbol} - Total Profit: ${bestPair.TotalProfit:F2} from {bestPair.SessionCount} session(s)");
            }

            // Query 5: Risk Management usage count
            Console.WriteLine("\nQUERY 5: Risk Management Rules by Usage Count");
            var rmUsage = strategies
                .GroupBy(s => s.RiskManagement.Name)
                .Select(g => new { Name = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .ToList();

            foreach (var rm in rmUsage)
            {
                Console.WriteLine($"   {rm.Name}: Used in {rm.Count} strategie(s)");
            }

            // Query 6: Sessions with more than 20 trades
            Console.WriteLine("\nQUERY 6: Sessions with More Than 20 Trades");
            var activeSessions = sessions
                .Where(s => s.Results.Count > 20)
                .Select(s => new
                {
                    Session = s,
                    AvgProfitPerTrade = s.Results.Average(r => r.GetProfit())
                })
                .OrderByDescending(x => x.AvgProfitPerTrade)
                .ToList();

            foreach (var session in activeSessions)
            {
                Console.WriteLine($"   {session.Session.CryptoPair.Symbol} - {session.Session.Results.Count} trades, Avg P/L: ${session.AvgProfitPerTrade:F2}");
            }

            // Query 7: Price movement analysis (High-Low range)
            Console.WriteLine("\nQUERY 7: Crypto Pairs with Highest Average Volatility");
            var volatilityAnalysis = pairs
                .Select(p => new
                {
                    Pair = p,
                    AvgVolatility = p.CandleDataHistory.Average(c => (c.High - c.Low) / c.Open * 100)
                })
                .OrderByDescending(x => x.AvgVolatility)
                .ToList();

            foreach (var vol in volatilityAnalysis)
            {
                Console.WriteLine($"   {vol.Pair.Symbol} - Avg Volatility: {vol.AvgVolatility:F2}%");
            }
        }

        static async Task ExecuteAsyncOperations(List<BacktestStrategy> strategies, List<BacktestSession> sessions)
        {
            Console.WriteLine("Simulating async backtesting operations...\n");

            // Simulate async backtesting for each strategy
            var tasks = strategies.Select(strategy => SimulateBacktestAsync(strategy)).ToList();
            await Task.WhenAll(tasks);

            Console.WriteLine("\nAsync operations completed successfully!");
        }

        static async Task SimulateBacktestAsync(BacktestStrategy strategy)
        {
            Console.WriteLine($"[ASYNC] Starting backtest for '{strategy.Name}' with ${strategy.InitialCapital} capital...");
            
            // Simulate some async work (like calling an external API or database)
            await Task.Delay(300 + strategy.Id * 100);
            
            var profit = (new Random(strategy.Id)).Next(1000, 5000);
            var roi = (profit / (double)strategy.InitialCapital) * 100;
            
            Console.WriteLine($"[ASYNC] Completed '{strategy.Name}' - Estimated Profit: ${profit} (ROI: {roi:F2}%)");
        }
    }
}
