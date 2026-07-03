using CryptoBacktestingDashboard.Data;
using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Repositories.EF;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CryptoBacktestingDashboard.Services
{
    // ── DTOs ────────────────────────────────────────────────────────────

    public class ToolDefinition
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public JsonElement InputSchema { get; set; }
    }

    public class ToolCallInfo
    {
        public string Tool { get; set; } = string.Empty;
        public string Args { get; set; } = string.Empty;
        public object? Result { get; set; }
        public bool Success { get; set; }
    }

    // ── AgentTools ──────────────────────────────────────────────────────

    public class AgentTools
    {
        private readonly CryptoPairRepository _pairRepo;
        private readonly BacktestStrategyRepository _strategyRepo;
        private readonly BacktestSessionRepository _sessionRepo;
        private readonly IndicatorRepository _indicatorRepo;
        private readonly BacktestResultRepository _resultRepo;
        private readonly MarketDataService _marketDataService;
        private readonly BacktestService _backtestService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AgentTools> _logger;

        public AgentTools(
            CryptoPairRepository pairRepo,
            BacktestStrategyRepository strategyRepo,
            BacktestSessionRepository sessionRepo,
            IndicatorRepository indicatorRepo,
            BacktestResultRepository resultRepo,
            MarketDataService marketDataService,
            BacktestService backtestService,
            ApplicationDbContext context,
            ILogger<AgentTools> logger)
        {
            _pairRepo = pairRepo;
            _strategyRepo = strategyRepo;
            _sessionRepo = sessionRepo;
            _indicatorRepo = indicatorRepo;
            _resultRepo = resultRepo;
            _marketDataService = marketDataService;
            _backtestService = backtestService;
            _context = context;
            _logger = logger;
        }

        // ── Tool definitions ───────────────────────────────────────────

        public List<ToolDefinition> GetToolDefinitions()
        {
            return new List<ToolDefinition>
            {
                new()
                {
                    Name = "list_pairs",
                    Description = "List all crypto trading pairs in the database with their current prices.",
                    InputSchema = JsonDoc("""{"type":"object","properties":{},"required":[]}""")
                },
                new()
                {
                    Name = "get_pair",
                    Description = "Get detailed information about a specific crypto pair by ID.",
                    InputSchema = JsonDoc("""{"type":"object","properties":{"pair_id":{"type":"integer","description":"The ID of the crypto pair"}},"required":["pair_id"]}""")
                },
                new()
                {
                    Name = "add_pair",
                    Description = "Add a new cryptocurrency trading pair (e.g. ETH/USDT, BTC/USD, SOL/USDT). The symbol must include a '/' separator between base and quote assets.",
                    InputSchema = JsonDoc("""{"type":"object","properties":{"symbol":{"type":"string","description":"Trading pair symbol with '/' separator, e.g. ETH/USDT"},"base_asset":{"type":"string","description":"Base asset ticker, e.g. ETH"},"quote_asset":{"type":"string","description":"Quote asset ticker, e.g. USDT"}},"required":["symbol","base_asset","quote_asset"]}""")
                },
                new()
                {
                    Name = "delete_pair",
                    Description = "Delete a cryptocurrency pair and all its candle data by ID.",
                    InputSchema = JsonDoc("""{"type":"object","properties":{"pair_id":{"type":"integer","description":"The ID of the crypto pair to delete"}},"required":["pair_id"]}""")
                },
                new()
                {
                    Name = "fetch_candles",
                    Description = "Fetch historical OHLCV candle data from Binance for a crypto pair. Uses the pair ID from the database.",
                    InputSchema = JsonDoc("""{"type":"object","properties":{"pair_id":{"type":"integer","description":"The ID of the crypto pair"},"interval":{"type":"string","description":"Candle interval: 1d (daily), 1h (hourly), 1w (weekly)","enum":["1d","1h","4h","1w"]},"days_back":{"type":"integer","description":"How many days of historical data to fetch (max 1000)","default":365}},"required":["pair_id"]}""")
                },
                new()
                {
                    Name = "list_strategies",
                    Description = "List all backtest strategies with their key settings.",
                    InputSchema = JsonDoc("""{"type":"object","properties":{},"required":[]}""")
                },
                new()
                {
                    Name = "get_strategy",
                    Description = "Get detailed information about a specific backtest strategy by ID.",
                    InputSchema = JsonDoc("""{"type":"object","properties":{"strategy_id":{"type":"integer","description":"The ID of the strategy"}},"required":["strategy_id"]}""")
                },
                new()
                {
                    Name = "create_strategy",
                    Description = "Create a new backtest trading strategy. When the user asks for a known strategy style (e.g. \"MA cross\", \"moving average crossover\", \"RSI\", \"MACD\", \"Bollinger\"), ALWAYS pass the matching 'template' so the strategy is created with its indicators AND comparison rules already wired up and ready to backtest — do not create an empty strategy in that case.",
                    InputSchema = JsonDoc("""{"type":"object","properties":{"name":{"type":"string","description":"Strategy name"},"description":{"type":"string","description":"Strategy description"},"template":{"type":"string","description":"Starter template that auto-creates the matching indicators AND signal/comparison rules so the strategy is immediately runnable. 'ma_cross' = fast/slow moving-average crossover (creates a 10-period + 50-period MA and rules: buy when fast crosses above slow, sell when it crosses below). 'rsi' = RSI(14) overbought/oversold. 'macd' = MACD momentum. 'bollinger' = Bollinger Bands(20). Omit only for an intentionally empty strategy.","enum":["ma_cross","rsi","macd","bollinger"]},"initial_capital":{"type":"number","description":"Starting capital for backtests","default":10000},"stop_loss_percent":{"type":"number","description":"Stop loss percentage (e.g. 5 for 5%)","default":5},"take_profit_percent":{"type":"number","description":"Take profit percentage (e.g. 10 for 10%)","default":10},"trailing_stop_percent":{"type":"number","description":"Optional trailing stop percentage"},"position_size_percent":{"type":"number","description":"Position size as percentage of capital (default 100)","default":100}},"required":["name"]}""")
                },
                new()
                {
                    Name = "delete_strategy",
                    Description = "Delete a backtest strategy and all its sessions by ID.",
                    InputSchema = JsonDoc("""{"type":"object","properties":{"strategy_id":{"type":"integer","description":"The ID of the strategy to delete"}},"required":["strategy_id"]}""")
                },
                new()
                {
                    Name = "list_sessions",
                    Description = "List all backtest sessions with their status, strategy, pair, and results summary.",
                    InputSchema = JsonDoc("""{"type":"object","properties":{},"required":[]}""")
                },
                new()
                {
                    Name = "get_session",
                    Description = "Get detailed information about a specific backtest session including all trade results.",
                    InputSchema = JsonDoc("""{"type":"object","properties":{"session_id":{"type":"integer","description":"The ID of the backtest session"}},"required":["session_id"]}""")
                },
                new()
                {
                    Name = "run_backtest",
                    Description = "Execute a backtest for a session. The session must already exist and have a strategy and crypto pair assigned.",
                    InputSchema = JsonDoc("""{"type":"object","properties":{"session_id":{"type":"integer","description":"The ID of the backtest session to run"},"initial_balance":{"type":"number","description":"Optional: override the session's initial balance"}},"required":["session_id"]}""")
                },
                new()
                {
                    Name = "delete_session",
                    Description = "Delete a backtest session and all its results by ID.",
                    InputSchema = JsonDoc("""{"type":"object","properties":{"session_id":{"type":"integer","description":"The ID of the session to delete"}},"required":["session_id"]}""")
                },
                new()
                {
                    Name = "create_session",
                    Description = "Create a new backtest session linking a strategy to a crypto pair over a date range.",
                    InputSchema = JsonDoc("""{"type":"object","properties":{"strategy_id":{"type":"integer","description":"The ID of the strategy to use"},"pair_id":{"type":"integer","description":"The ID of the crypto pair"},"start_date":{"type":"string","description":"Start date in YYYY-MM-DD format"},"end_date":{"type":"string","description":"End date in YYYY-MM-DD format"},"initial_balance":{"type":"number","description":"Initial balance for the backtest","default":10000}},"required":["strategy_id","pair_id","start_date","end_date"]}""")
                },
                new()
                {
                    Name = "list_indicators",
                    Description = "List all technical indicators available in the system.",
                    InputSchema = JsonDoc("""{"type":"object","properties":{},"required":[]}""")
                },
                new()
                {
                    Name = "create_indicator",
                    Description = "Create a new technical indicator configuration.",
                    InputSchema = JsonDoc("""{"type":"object","properties":{"name":{"type":"string","description":"Indicator name"},"type":{"type":"string","description":"Indicator type","enum":["RSI","MACD","MovingAverage","BollingerBands","Stochastic","ATR"]},"period":{"type":"integer","description":"Lookback period (e.g. 14 for RSI)"},"threshold":{"type":"number","description":"Threshold value (e.g. 70 for RSI overbought)"}},"required":["name","type"]}""")
                },
            };
        }

        // ── Tool execution ─────────────────────────────────────────────

        public async Task<(string result, bool success)> ExecuteToolAsync(string toolName, JsonElement args, string userId)
        {
            try
            {
                return toolName switch
                {
                    "list_pairs"        => await ListPairsAsync(),
                    "get_pair"          => await GetPairAsync(args),
                    "add_pair"          => await AddPairAsync(args, userId),
                    "delete_pair"       => await DeletePairAsync(args),
                    "fetch_candles"     => await FetchCandlesAsync(args),
                    "list_strategies"   => await ListStrategiesAsync(userId),
                    "get_strategy"      => await GetStrategyAsync(args, userId),
                    "create_strategy"   => await CreateStrategyAsync(args, userId),
                    "delete_strategy"   => await DeleteStrategyAsync(args, userId),
                    "list_sessions"     => await ListSessionsAsync(userId),
                    "get_session"       => await GetSessionAsync(args, userId),
                    "run_backtest"      => await RunBacktestAsync(args, userId),
                    "delete_session"    => await DeleteSessionAsync(args, userId),
                    "create_session"    => await CreateSessionAsync(args, userId),
                    "list_indicators"   => await ListIndicatorsAsync(),
                    "create_indicator"  => await CreateIndicatorAsync(args),
                    _ => ($"Unknown tool: {toolName}", false)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tool {ToolName} failed for user {UserId}", toolName, userId);
                return ($"Error executing {toolName}: {ex.Message}", false);
            }
        }

        // ── Handler implementations ────────────────────────────────────

        private async Task<(string, bool)> ListPairsAsync()
        {
            var pairs = await _pairRepo.GetItemsAsync();

            // GetItemsAsync doesn't eager-load candle history, so count straight from the DB.
            var candleCounts = await _context.CandleData
                .GroupBy(c => c.CryptoPairId)
                .Select(g => new { PairId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PairId, x => x.Count);

            var data = pairs.Select(p => new
            {
                p.Id,
                p.Symbol,
                p.BaseAsset,
                p.QuoteAsset,
                CurrentPrice = p.CurrentPrice.HasValue ? $"{p.CurrentPrice:F2}" : "N/A",
                CandleCount = candleCounts.TryGetValue(p.Id, out var cc) ? cc : 0
            }).ToList();

            var summary = pairs.Any()
                ? $"Found {pairs.Count} pairs. Use get_pair with an ID for details."
                : "No crypto pairs found. Use add_pair to create one.";
            return (Json(data, summary), true);
        }

        private async Task<(string, bool)> GetPairAsync(JsonElement args)
        {
            var pairId = args.GetProperty("pair_id").GetInt32();
            var pair = await _pairRepo.GetItemAsync(pairId);
            if (pair == null)
                return ($"Pair with ID {pairId} not found.", false);

            var data = new
            {
                pair.Id,
                pair.Symbol,
                pair.BaseAsset,
                pair.QuoteAsset,
                CurrentPrice = pair.CurrentPrice.HasValue ? $"{pair.CurrentPrice:F2}" : "N/A",
                pair.CreatedAt,
                CandleCount = pair.CandleDataHistory?.Count ?? 0,
                DateRange = pair.CandleDataHistory?.Count > 0
                    ? new
                    {
                        Earliest = pair.CandleDataHistory.Min(c => c.OpenTime).ToString("yyyy-MM-dd"),
                        Latest = pair.CandleDataHistory.Max(c => c.OpenTime).ToString("yyyy-MM-dd")
                    }
                    : null
            };
            return (Json(data), true);
        }

        private async Task<(string, bool)> AddPairAsync(JsonElement args, string userId)
        {
            var symbol = args.GetProperty("symbol").GetString()!;
            var baseAsset = args.GetProperty("base_asset").GetString()!;
            var quoteAsset = args.GetProperty("quote_asset").GetString()!;

            // Check for duplicates
            var existing = await _context.CryptoPairs.AnyAsync(p => p.Symbol == symbol);
            if (existing)
                return ($"Pair {symbol} already exists in the database.", false);

            var pair = new CryptoPair
            {
                Symbol = symbol,
                BaseAsset = baseAsset,
                QuoteAsset = quoteAsset,
                CreatedAt = DateTime.UtcNow
            };

            await _pairRepo.InsertItemAsync(pair);

            _logger.LogInformation("Agent created pair {Symbol} (ID {Id}) for user {UserId}", symbol, pair.Id, userId);
            return ($"✅ Created pair **{symbol}** (ID: {pair.Id}). You can now fetch candle data for it.", true);
        }

        private async Task<(string, bool)> DeletePairAsync(JsonElement args)
        {
            var pairId = args.GetProperty("pair_id").GetInt32();
            var result = await _pairRepo.DeleteItemAsync(pairId);
            return result
                ? ($"✅ Deleted pair ID {pairId}.", true)
                : ($"Pair ID {pairId} not found.", false);
        }

        private async Task<(string, bool)> FetchCandlesAsync(JsonElement args)
        {
            var pairId = args.GetProperty("pair_id").GetInt32();
            var interval = args.TryGetProperty("interval", out var i) ? i.GetString() ?? "1d" : "1d";
            var daysBack = args.TryGetProperty("days_back", out var d) ? d.GetInt32() : 365;

            var (inserted, error) = await _marketDataService.FetchCandlesAsync(pairId, interval, daysBack);

            if (!string.IsNullOrEmpty(error))
                return ($"Error: {error}", false);

            return ($"✅ Fetched **{inserted} new candles** for pair ID {pairId}.", true);
        }

        private async Task<(string, bool)> ListStrategiesAsync(string userId)
        {
            var strategies = await _strategyRepo.GetItemsAsync(userId);
            var data = strategies.Select(s => new
            {
                s.Id,
                s.Name,
                s.Description,
                s.IsActive,
                s.InitialCapital,
                s.StopLossPercent,
                s.TakeProfitPercent,
                TrailingStop = s.TrailingStopPercent?.ToString() ?? "N/A",
                PositionSize = s.PositionSizePercent?.ToString() ?? "100%",
                IndicatorCount = s.Indicators?.Count ?? 0,
                s.LastModifiedAt
            }).ToList();

            var summary = strategies.Any()
                ? $"Found {strategies.Count} strategies. Use get_strategy with an ID for full details."
                : "No strategies found. Use create_strategy to make one.";
            return (Json(data, summary), true);
        }

        private async Task<(string, bool)> GetStrategyAsync(JsonElement args, string userId)
        {
            var id = args.GetProperty("strategy_id").GetInt32();
            var strategy = await _strategyRepo.GetItemAsync(id, userId);
            if (strategy == null)
                return ($"Strategy ID {id} not found.", false);

            var data = new
            {
                strategy.Id,
                strategy.Name,
                strategy.Description,
                strategy.IsActive,
                strategy.InitialCapital,
                strategy.StopLossPercent,
                strategy.TakeProfitPercent,
                TrailingStop = strategy.TrailingStopPercent,
                PositionSize = strategy.PositionSizePercent,
                TradeDirection = strategy.TradeDirection.ToString(),
                Indicators = strategy.Indicators?.Select(ind => new
                {
                    ind.Id,
                    ind.Name,
                    Type = ind.Type.ToString(),
                    ind.Period,
                    ind.Threshold
                }),
                Comparisons = strategy.Comparisons?.Select(c => new
                {
                    c.Name,
                    ComparisonType = c.ComparisonType.ToString(),
                    TargetSignal = c.TargetSignal.ToString(),
                    IndicatorA = c.IndicatorA?.Name,
                    IndicatorB = c.IndicatorB?.Name
                }),
                SessionCount = strategy.BacktestSessions?.Count ?? 0,
                strategy.LastModifiedAt
            };
            return (Json(data), true);
        }

        private async Task<(string, bool)> CreateStrategyAsync(JsonElement args, string userId)
        {
            var name = args.GetProperty("name").GetString()!;
            var description = args.TryGetProperty("description", out var desc) ? desc.GetString() ?? "" : "";
            var initialCapital = args.TryGetProperty("initial_capital", out var cap) ? cap.GetDecimal() : 10000m;
            var sl = args.TryGetProperty("stop_loss_percent", out var slVal) ? slVal.GetDecimal() : 5m;
            var tp = args.TryGetProperty("take_profit_percent", out var tpVal) ? tpVal.GetDecimal() : 10m;
            var ts = args.TryGetProperty("trailing_stop_percent", out var tsVal) ? tsVal.GetDecimal() : (decimal?)null;
            var ps = args.TryGetProperty("position_size_percent", out var psVal) ? psVal.GetDecimal() : 100m;

            var template = args.TryGetProperty("template", out var tplVal)
                ? (tplVal.GetString() ?? "").Trim().ToLowerInvariant()
                : "";

            var strategy = new BacktestStrategy
            {
                Name = name,
                Description = description,
                AppUserId = userId,
                IsActive = true,
                InitialCapital = initialCapital,
                StopLossPercent = sl,
                TakeProfitPercent = tp,
                TrailingStopPercent = ts,
                PositionSizePercent = ps,
                LastModifiedAt = DateTime.UtcNow,
                TradeDirection = TradeDirection.Both,
                LookbackPeriod = 50
            };

            // Build the template's indicators + comparison rules into the same object graph,
            // so a single insert persists the strategy, its indicators (N:N) and rules at once.
            var built = ApplyStrategyTemplate(strategy, template);

            await _strategyRepo.InsertItemAsync(strategy);

            _logger.LogInformation("Agent created strategy '{Name}' (ID {Id}, template '{Template}') for user {UserId}",
                name, strategy.Id, string.IsNullOrEmpty(template) ? "none" : template, userId);

            var msg = $"✅ Created strategy **\"{name}\"** (ID: {strategy.Id}).";
            if (built.Count > 0)
                msg += "\n\nAuto-configured from the template:\n" + string.Join("\n", built.Select(b => $"- {b}"));
            msg += "\n\nYou can now create a session to backtest it.";
            return (msg, true);
        }

        /// <summary>
        /// Populates a strategy's Indicators and Comparisons collections based on a named
        /// template. Returns a human-readable list of what was created (empty if no template).
        /// EF persists the whole graph on insert: navigation properties on each comparison
        /// resolve to the indicator rows created here.
        /// </summary>
        private static List<string> ApplyStrategyTemplate(BacktestStrategy strategy, string template)
        {
            var built = new List<string>();

            Indicator NewIndicator(string name, IndicatorType type, int period, decimal threshold, string desc)
            {
                var ind = new Indicator
                {
                    Name = name,
                    Type = type,
                    Period = period,
                    Threshold = threshold,
                    Description = desc,
                    CreatedAt = DateTime.UtcNow
                };
                strategy.Indicators.Add(ind);
                return ind;
            }

            switch (template)
            {
                case "ma_cross":
                case "ma":
                case "macross":
                case "moving_average_crossover":
                {
                    var fast = NewIndicator($"{strategy.Name} Fast MA (10)", IndicatorType.MovingAverage, 10, 0, "Fast moving average for MA crossover");
                    var slow = NewIndicator($"{strategy.Name} Slow MA (50)", IndicatorType.MovingAverage, 50, 0, "Slow moving average for MA crossover");
                    built.Add("Indicator: Fast MA (period 10)");
                    built.Add("Indicator: Slow MA (period 50)");

                    strategy.Comparisons.Add(new IndicatorComparison
                    {
                        BacktestStrategy = strategy,
                        IndicatorA = fast,
                        IndicatorB = slow,
                        ComparisonType = ComparisonType.CrossoverAbove,
                        TargetSignal = IndicatorSignal.Buy,
                        Name = "Fast MA crosses above Slow MA → Buy",
                        CreatedAt = DateTime.UtcNow
                    });
                    strategy.Comparisons.Add(new IndicatorComparison
                    {
                        BacktestStrategy = strategy,
                        IndicatorA = fast,
                        IndicatorB = slow,
                        ComparisonType = ComparisonType.CrossoverBelow,
                        TargetSignal = IndicatorSignal.Sell,
                        Name = "Fast MA crosses below Slow MA → Sell",
                        CreatedAt = DateTime.UtcNow
                    });
                    built.Add("Rule: Fast MA crosses above Slow MA → Buy");
                    built.Add("Rule: Fast MA crosses below Slow MA → Sell");
                    break;
                }
                case "rsi":
                {
                    NewIndicator($"{strategy.Name} RSI (14)", IndicatorType.RSI, 14, 70, "RSI overbought (>70 Sell) / oversold (<30 Buy)");
                    built.Add("Indicator: RSI (period 14, overbought 70) — buys oversold, sells overbought");
                    break;
                }
                case "macd":
                {
                    NewIndicator($"{strategy.Name} MACD (12)", IndicatorType.MACD, 12, 26, "MACD momentum (fast 12 / slow 26); buys/sells on histogram zero-cross");
                    built.Add("Indicator: MACD (fast 12 / slow 26) — signals on histogram zero-cross");
                    break;
                }
                case "bollinger":
                case "bollinger_bands":
                {
                    NewIndicator($"{strategy.Name} Bollinger (20)", IndicatorType.BollingerBands, 20, 2, "Bollinger Bands (period 20, 2 std-dev); buys lower-band touch, sells upper-band touch");
                    built.Add("Indicator: Bollinger Bands (period 20, 2σ) — buys lower band, sells upper band");
                    break;
                }
            }

            return built;
        }

        private async Task<(string, bool)> DeleteStrategyAsync(JsonElement args, string userId)
        {
            var id = args.GetProperty("strategy_id").GetInt32();
            var strategy = await _strategyRepo.GetItemAsync(id, userId);
            if (strategy == null)
                return ($"Strategy ID {id} not found.", false);

            var result = await _strategyRepo.DeleteItemAsync(id);
            return result
                ? ($"✅ Deleted strategy **\"{strategy.Name}\"** (ID: {id}).", true)
                : ($"Failed to delete strategy ID {id}.", false);
        }

        private async Task<(string, bool)> ListSessionsAsync(string userId)
        {
            var sessions = await _sessionRepo.GetItemsAsync(userId);
            var data = sessions.Select(s => new
            {
                s.Id,
                Strategy = s.Strategy?.Name ?? "N/A",
                Pair = s.CryptoPair?.Symbol ?? "N/A",
                DateRange = $"{s.StartDate:yyyy-MM-dd} to {s.EndDate:yyyy-MM-dd}",
                InitialBalance = $"{s.InitialBalance:F2}",
                FinalBalance = s.HasRun ? $"{s.FinalBalance:F2}" : "Not run",
                TradeCount = s.Results?.Count ?? 0,
                s.ExecutedAt,
                s.IsOptimized
            }).ToList();

            var summary = sessions.Any()
                ? $"Found {sessions.Count} sessions. Use get_session with an ID for trade details."
                : "No sessions found. Use create_session to make one.";
            return (Json(data, summary), true);
        }

        private async Task<(string, bool)> GetSessionAsync(JsonElement args, string userId)
        {
            var id = args.GetProperty("session_id").GetInt32();
            var session = await _sessionRepo.GetItemAsync(id, userId);
            if (session == null)
                return ($"Session ID {id} not found.", false);

            var pnl = session.HasRun
                ? session.FinalBalance - session.InitialBalance
                : (decimal?)null;

            var data = new
            {
                session.Id,
                Strategy = session.Strategy?.Name,
                Pair = session.CryptoPair?.Symbol,
                DateRange = $"{session.StartDate:yyyy-MM-dd} to {session.EndDate:yyyy-MM-dd}",
                session.InitialBalance,
                FinalBalance = session.FinalBalance,
                PnL = pnl,
                PnLPercent = pnl.HasValue && session.InitialBalance > 0
                    ? $"{pnl / session.InitialBalance * 100:F2}%"
                    : null,
                TotalTrades = session.Results?.Count ?? 0,
                WinningTrades = session.Results?.Count(r => r.IsWinningTrade) ?? 0,
                LosingTrades = session.Results?.Count(r => !r.IsWinningTrade) ?? 0,
                WinRate = session.Results?.Count > 0
                    ? $"{session.Results.Count(r => r.IsWinningTrade) * 100m / session.Results.Count:F1}%"
                    : null,
                session.ExecutedAt,
                session.IsOptimized,
                RecentTrades = session.Results?.OrderByDescending(r => r.ExitTime).Take(10).Select(r => new
                {
                    r.EntryTime,
                    r.ExitTime,
                    Type = r.TradeType.ToString(),
                    EntryPrice = $"{r.EntryPrice:F2}",
                    ExitPrice = $"{r.ExitPrice:F2}",
                    Profit = $"{r.GetProfit():F2}",
                    r.IsWinningTrade
                })
            };
            return (Json(data), true);
        }

        private async Task<(string, bool)> RunBacktestAsync(JsonElement args, string userId)
        {
            var sessionId = args.GetProperty("session_id").GetInt32();
            var session = await _sessionRepo.GetItemAsync(sessionId, userId);
            if (session == null)
                return ($"Session ID {sessionId} not found.", false);

            // Load strategy
            if (session.StrategyId == 0)
                return ("This session has no strategy assigned.", false);

            var strategy = await _strategyRepo.GetItemAsync(session.StrategyId, userId);
            if (strategy == null)
                return ("The session's strategy was not found.", false);

            session.Strategy = strategy;

            // Optionally override balance
            if (args.TryGetProperty("initial_balance", out var balOverride))
                session.InitialBalance = balOverride.GetDecimal();

            try
            {
                await _backtestService.RunBacktestAsync(session);
                var pnl = session.FinalBalance - session.InitialBalance;
                var pnlPct = session.InitialBalance > 0
                    ? pnl / session.InitialBalance * 100m
                    : 0m;

                _logger.LogInformation("Agent ran backtest Session {SessionId} for user {UserId}: PnL {Pnl:F2}", sessionId, userId, pnl);

                return ($"✅ Backtest completed for Session {sessionId}:\n" +
                        $"- Initial balance: ${session.InitialBalance:F2}\n" +
                        $"- Final balance: ${session.FinalBalance:F2}\n" +
                        $"- P&L: **${pnl:F2} ({pnlPct:F2}%)**\n" +
                        $"- Trades: {session.Results?.Count ?? 0}\n" +
                        $"- Winning: {session.Results?.Count(r => r.IsWinningTrade) ?? 0}\n" +
                        $"- Losing: {session.Results?.Count(r => !r.IsWinningTrade) ?? 0}",
                    true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Agent backtest Session {SessionId} failed for user {UserId}", sessionId, userId);
                return ($"Backtest failed: {ex.Message}", false);
            }
        }

        private async Task<(string, bool)> DeleteSessionAsync(JsonElement args, string userId)
        {
            var id = args.GetProperty("session_id").GetInt32();
            var result = await _sessionRepo.DeleteItemAsync(id, userId);
            return result
                ? ($"✅ Deleted session ID {id}.", true)
                : ($"Session ID {id} not found.", false);
        }

        private async Task<(string, bool)> CreateSessionAsync(JsonElement args, string userId)
        {
            var strategyId = args.GetProperty("strategy_id").GetInt32();
            var pairId = args.GetProperty("pair_id").GetInt32();
            var startDate = DateTime.Parse(args.GetProperty("start_date").GetString()!);
            var endDate = DateTime.Parse(args.GetProperty("end_date").GetString()!);
            var balance = args.TryGetProperty("initial_balance", out var bal) ? bal.GetDecimal() : 10000m;

            // Verify strategy exists
            var strategy = await _strategyRepo.GetItemAsync(strategyId, userId);
            if (strategy == null)
                return ($"Strategy ID {strategyId} not found.", false);

            // Verify pair exists
            var pair = await _pairRepo.GetItemAsync(pairId);
            if (pair == null)
                return ($"Pair ID {pairId} not found.", false);

            var session = new BacktestSession
            {
                StrategyId = strategyId,
                CryptoPairId = pairId,
                AppUserId = userId,
                StartDate = startDate,
                EndDate = endDate,
                InitialBalance = balance
            };

            await _sessionRepo.InsertItemAsync(session);

            _logger.LogInformation("Agent created session (ID {SessionId}) for user {UserId}", session.Id, userId);
            return ($"✅ Created session **#{session.Id}**: {strategy.Name} on {pair.Symbol} " +
                    $"({startDate:yyyy-MM-dd} → {endDate:yyyy-MM-dd}). " +
                    $"Use run_backtest with session_id={session.Id} to execute it.", true);
        }

        private async Task<(string, bool)> ListIndicatorsAsync()
        {
            var indicators = await _indicatorRepo.GetItemsAsync();
            var data = indicators.Select(i => new
            {
                i.Id,
                i.Name,
                Type = i.Type.ToString(),
                i.Period,
                i.Threshold,
                i.Description
            }).ToList();

            var summary = indicators.Any()
                ? $"Found {indicators.Count} indicators. Create more with create_indicator."
                : "No indicators found. Use create_indicator to add one.";
            return (Json(data, summary), true);
        }

        private async Task<(string, bool)> CreateIndicatorAsync(JsonElement args)
        {
            var name = args.GetProperty("name").GetString()!;
            var typeStr = args.GetProperty("type").GetString()!;
            var period = args.TryGetProperty("period", out var p) ? p.GetInt32() : 14;
            var threshold = args.TryGetProperty("threshold", out var t) ? t.GetDecimal() : 0;

            if (!Enum.TryParse<IndicatorType>(typeStr, true, out var type))
                return ($"Invalid indicator type: {typeStr}. Valid: RSI, MACD, MovingAverage, BollingerBands, Stochastic, ATR", false);

            var indicator = new Indicator
            {
                Name = name,
                Type = type,
                Period = period,
                Threshold = threshold,
                Description = $"{name} ({type}, period={period}, threshold={threshold})"
            };

            await _indicatorRepo.InsertItemAsync(indicator);
            return ($"✅ Created indicator **\"{name}\"** (ID: {indicator.Id}, type: {type}).", true);
        }

        // ── Helpers ────────────────────────────────────────────────────

        private static string Json(object data, string? summaryOverride = null)
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            if (summaryOverride != null)
                return $"{summaryOverride}\n\n```json\n{json}\n```";
            return $"```json\n{json}\n```";
        }

        private static JsonElement JsonDoc(string json)
        {
            return JsonSerializer.Deserialize<JsonElement>(json);
        }
    }
}
