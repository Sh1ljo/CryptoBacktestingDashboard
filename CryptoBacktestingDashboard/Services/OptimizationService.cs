using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Models.DTO;
using CryptoBacktestingDashboard.Repositories.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Services
{
    public class OptimizationService
    {
        private readonly BacktestService _backtestService;
        private readonly BacktestSessionRepository _sessionRepo;
        private readonly BacktestStrategyRepository _strategyRepo;
        private readonly IndicatorRepository _indicatorRepo;
        private readonly OptimizationRunRepository _runRepo;
        private readonly OptimizationResultRepository _optResultRepo;
        private readonly ILogger<OptimizationService> _logger;

        public OptimizationService(
            BacktestService backtestService,
            BacktestSessionRepository sessionRepo,
            BacktestStrategyRepository strategyRepo,
            IndicatorRepository indicatorRepo,
            OptimizationRunRepository runRepo,
            OptimizationResultRepository optResultRepo,
            ILogger<OptimizationService> logger)
        {
            _backtestService = backtestService;
            _sessionRepo = sessionRepo;
            _strategyRepo = strategyRepo;
            _indicatorRepo = indicatorRepo;
            _runRepo = runRepo;
            _optResultRepo = optResultRepo;
            _logger = logger;
        }

        // ── Profile construction ────────────────────────────────────────

        public OptimizationProfile BuildProfile(OptimizationProfileDTO dto)
        {
            var profile = new OptimizationProfile();

            foreach (var ind in dto.Indicators)
            {
                var sweep = new IndicatorSweep
                {
                    PeriodSweep = BuildSweep(ind.PeriodMin, ind.PeriodMax, ind.PeriodStep),
                    ThresholdSweep = BuildSweep(ind.ThresholdMin, ind.ThresholdMax, ind.ThresholdStep)
                };

                if (sweep.PeriodSweep != null || sweep.ThresholdSweep != null)
                    profile.IndicatorSweeps[ind.IndicatorId] = sweep;
            }

            profile.StopLossSweep = BuildSweep(dto.SlMin, dto.SlMax, dto.SlStep);
            profile.TakeProfitSweep = BuildSweep(dto.TpMin, dto.TpMax, dto.TpStep);
            profile.TrailingStopSweep = BuildSweep(dto.TsMin, dto.TsMax, dto.TsStep);
            profile.PositionSizeSweep = BuildSweep(dto.PsMin, dto.PsMax, dto.PsStep);

            return profile;
        }

        private static ParameterSweep? BuildSweep(decimal? min, decimal? max, decimal? step)
        {
            if (!min.HasValue || !max.HasValue || !step.HasValue) return null;
            if (max.Value <= min.Value || step.Value <= 0) return null;
            return new ParameterSweep { Min = min.Value, Max = max.Value, Step = step.Value };
        }

        // ── Grid expansion ───────────────────────────────────────────────

        // Expands the profile into the cartesian product of all parameter combinations.
        // Each combo is keyed by "Indicator_{id}_Period", "Indicator_{id}_Threshold",
        // "SL", "TP", "TS", "PS". A profile with no sweeps configured returns a single
        // empty combo.
        public List<Dictionary<string, decimal>> ExpandParameterGrid(OptimizationProfile profile, BacktestStrategy strategy)
        {
            var referencedIndicatorIds = new HashSet<int>();
            foreach (var ind in strategy.Indicators ?? Enumerable.Empty<Indicator>())
                referencedIndicatorIds.Add(ind.Id);
            foreach (var c in strategy.Comparisons ?? Enumerable.Empty<IndicatorComparison>())
            {
                if (c.IndicatorA != null) referencedIndicatorIds.Add(c.IndicatorA.Id);
                if (c.IndicatorB != null) referencedIndicatorIds.Add(c.IndicatorB.Id);
            }

            var axes = new List<(string Key, List<decimal> Values)>();

            foreach (var (indicatorId, sweep) in profile.IndicatorSweeps)
            {
                if (!referencedIndicatorIds.Contains(indicatorId)) continue;
                if (sweep.PeriodSweep != null)
                    axes.Add(($"Indicator_{indicatorId}_Period", sweep.PeriodSweep.Expand()));
                if (sweep.ThresholdSweep != null)
                    axes.Add(($"Indicator_{indicatorId}_Threshold", sweep.ThresholdSweep.Expand()));
            }

            if (profile.StopLossSweep != null) axes.Add(("SL", profile.StopLossSweep.Expand()));
            if (profile.TakeProfitSweep != null) axes.Add(("TP", profile.TakeProfitSweep.Expand()));
            if (profile.TrailingStopSweep != null) axes.Add(("TS", profile.TrailingStopSweep.Expand()));
            if (profile.PositionSizeSweep != null) axes.Add(("PS", profile.PositionSizeSweep.Expand()));

            var combos = new List<Dictionary<string, decimal>> { new() };
            foreach (var (key, values) in axes)
            {
                var next = new List<Dictionary<string, decimal>>(combos.Count * values.Count);
                foreach (var combo in combos)
                {
                    foreach (var v in values)
                    {
                        var clone = new Dictionary<string, decimal>(combo) { [key] = v };
                        next.Add(clone);
                    }
                }
                combos = next;
            }

            return combos;
        }

        // ── Strategy cloning ─────────────────────────────────────────────

        // Builds a copy of the strategy with the combo's parameter values applied.
        // The real strategy/indicators are never modified here.
        public BacktestStrategy CloneStrategyWithCombo(BacktestStrategy strategy, Dictionary<string, decimal> combo)
        {
            var clonedIndicators = new Dictionary<int, Indicator>();

            Indicator CloneIndicator(Indicator ind)
            {
                if (clonedIndicators.TryGetValue(ind.Id, out var existing)) return existing;

                var clone = new Indicator
                {
                    Id = ind.Id,
                    Name = ind.Name,
                    Type = ind.Type,
                    Period = ind.Period,
                    Threshold = ind.Threshold,
                    Description = ind.Description
                };

                if (combo.TryGetValue($"Indicator_{ind.Id}_Period", out var p))
                    clone.Period = (int)p;
                if (combo.TryGetValue($"Indicator_{ind.Id}_Threshold", out var t))
                    clone.Threshold = t;

                clonedIndicators[ind.Id] = clone;
                return clone;
            }

            var clonedStrategy = new BacktestStrategy
            {
                Id = strategy.Id,
                Name = strategy.Name,
                Description = strategy.Description,
                IsActive = strategy.IsActive,
                InitialCapital = strategy.InitialCapital,
                LookbackPeriod = strategy.LookbackPeriod,
                TradeDirection = strategy.TradeDirection,
                StopLossPercent = combo.TryGetValue("SL", out var sl) ? sl : strategy.StopLossPercent,
                TakeProfitPercent = combo.TryGetValue("TP", out var tp) ? tp : strategy.TakeProfitPercent,
                TrailingStopPercent = combo.TryGetValue("TS", out var ts) ? ts : strategy.TrailingStopPercent,
                PositionSizePercent = combo.TryGetValue("PS", out var ps) ? ps : strategy.PositionSizePercent,
            };

            clonedStrategy.Indicators = (strategy.Indicators ?? Enumerable.Empty<Indicator>())
                .Select(CloneIndicator).ToList();

            clonedStrategy.Comparisons = (strategy.Comparisons ?? Enumerable.Empty<IndicatorComparison>())
                .Select(c => new IndicatorComparison
                {
                    BacktestStrategyId = c.BacktestStrategyId,
                    IndicatorAId = c.IndicatorAId,
                    IndicatorBId = c.IndicatorBId,
                    ComparisonType = c.ComparisonType,
                    TargetSignal = c.TargetSignal,
                    Name = c.Name,
                    IndicatorA = c.IndicatorA != null ? CloneIndicator(c.IndicatorA) : null,
                    IndicatorB = c.IndicatorB != null ? CloneIndicator(c.IndicatorB) : null
                }).ToList();

            return clonedStrategy;
        }

        // ── Scoring ───────────────────────────────────────────────────────

        public class ComboMetrics
        {
            public decimal Profit { get; set; }
            public decimal ProfitPercent { get; set; }
            public decimal ProfitFactor { get; set; }
            public decimal WinRate { get; set; }
            public decimal MaxDrawdownPercent { get; set; }
            public int TotalTrades { get; set; }
            public decimal SharpeApprox { get; set; }
            public double CompositeScore { get; set; }
        }

        public ComboMetrics ComputeMetrics(decimal initialBalance, decimal finalBalance, List<BacktestResult> results)
        {
            var metrics = new ComboMetrics
            {
                Profit = finalBalance - initialBalance,
                TotalTrades = results.Count,
                ProfitPercent = initialBalance > 0 ? (finalBalance - initialBalance) / initialBalance * 100m : 0m
            };

            if (results.Count == 0)
            {
                metrics.CompositeScore = 0;
                return metrics;
            }

            var wins = results.Where(r => r.IsWinningTrade).ToList();
            var losses = results.Where(r => !r.IsWinningTrade).ToList();
            metrics.WinRate = (decimal)wins.Count / results.Count * 100m;

            var grossProfit = wins.Sum(r => r.GetProfit());
            var grossLoss = Math.Abs(losses.Sum(r => r.GetProfit()));
            metrics.ProfitFactor = grossLoss > 0 ? grossProfit / grossLoss : (grossProfit > 0 ? 10m : 0m);

            var ordered = results.OrderBy(r => r.ExitTime).ToList();
            decimal running = initialBalance;
            decimal peak = initialBalance;
            decimal maxDd = 0;
            var tradeReturns = new List<decimal>();
            foreach (var r in ordered)
            {
                running += r.GetProfit();
                if (running > peak) peak = running;
                var dd = peak > 0 ? (running - peak) / peak * 100m : 0m;
                if (dd < maxDd) maxDd = dd;
                tradeReturns.Add(r.GetProfitPercent());
            }
            metrics.MaxDrawdownPercent = maxDd;

            var avgReturn = tradeReturns.Average();
            var variance = tradeReturns.Select(x => (x - avgReturn) * (x - avgReturn)).Sum() / tradeReturns.Count;
            var stdDev = (decimal)Math.Sqrt((double)variance);
            metrics.SharpeApprox = stdDev > 0 ? avgReturn / stdDev : 0;

            metrics.CompositeScore = ComputeCompositeScore(metrics);
            return metrics;
        }

        // Composite score (0-100), weighted across profit, profit factor, win rate,
        // drawdown, Sharpe approx, and trade count. Profit is normalized as ROI mapped
        // from [-100%, +100%] onto [0, 1] (0% ROI -> 0.5), so it's self-contained per
        // combo and doesn't need a second pass over all results.
        private double ComputeCompositeScore(ComboMetrics m)
        {
            var roiClamped = Math.Max(-100m, Math.Min(100m, m.ProfitPercent));
            var profitNormalized = (double)((roiClamped + 100m) / 200m);

            var profitFactorNorm = (double)Math.Min(m.ProfitFactor, 10m) / 10.0;
            var winRateNorm = (double)m.WinRate / 100.0;
            var drawdownNorm = 1.0 - Math.Min(Math.Abs((double)m.MaxDrawdownPercent) / 100.0, 1.0);
            var sharpeNorm = Math.Max(Math.Min((double)m.SharpeApprox, 3.0) / 3.0, 0.0);
            var tradeCountNorm = m.TotalTrades >= 5 ? 1.0 : m.TotalTrades / 5.0;

            var score = profitNormalized * 0.25
                + profitFactorNorm * 0.20
                + winRateNorm * 0.15
                + drawdownNorm * 0.20
                + sharpeNorm * 0.10
                + tradeCountNorm * 0.10;

            return Math.Round(score * 100, 2);
        }

        // ── Run lifecycle ─────────────────────────────────────────────────

        // The criteria the user can optimize toward. Each becomes a selectable
        // option at the end of a run. Order here is the display order; the first
        // entry (composite) is the default recommendation.
        public static readonly (string Key, string Label)[] Criteria =
        {
            ("composite",    "Best Overall (composite)"),
            ("profit",       "Highest Profit"),
            ("profitFactor", "Best Profit Factor"),
            ("winRate",      "Highest Win Rate"),
            ("drawdown",     "Lowest Drawdown"),
        };

        // Scores every combo in parallel (Simulate is pure and CPU-bound), tracking
        // the best combo for each criterion. Only the winning combos are persisted —
        // a grid of thousands no longer means thousands of DB writes. Progress is
        // flushed to the DB on a throttled background writer so the client can poll.
        // Combos that throw (e.g. not enough candle data after a period change) are
        // scored 0 and skipped.
        public async Task RunOptimizationLoopAsync(int runId, int sessionId, List<Dictionary<string, decimal>> combos)
        {
            var run = await _runRepo.GetItemAsync(runId);
            if (run == null) return;

            _logger.LogInformation(
                "Optimization starting — Run {RunId}, Session {SessionId}, {TotalCombos} combos to evaluate",
                runId, sessionId, combos.Count);

            try
            {
                var session = await _sessionRepo.GetItemAsync(sessionId);
                if (session == null) throw new InvalidOperationException("Session not found.");

                var strategy = await _strategyRepo.GetItemAsync(session.StrategyId);
                if (strategy == null) throw new InvalidOperationException("Strategy not found.");
                session.Strategy = strategy;

                var candles = await _backtestService.GetCandlesAsync(session);
                var initialBalance = session.InitialBalance;

                // Winner-tracking is shared across threads, so guard updates with a lock.
                var bestByCriterion = new Dictionary<string, (Dictionary<string, decimal> Combo, ComboMetrics Metrics)>();
                var winnerLock = new object();
                int completed = 0;

                void Consider(Dictionary<string, decimal> combo, ComboMetrics m)
                {
                    lock (winnerLock)
                    {
                        foreach (var (key, _) in Criteria)
                        {
                            if (!bestByCriterion.TryGetValue(key, out var current)
                                || IsBetter(key, m, current.Metrics))
                            {
                                bestByCriterion[key] = (combo, m);
                            }
                        }
                    }
                }

                // Throttled progress writer — the only thread touching the DbContext
                // while the parallel sweep runs (which does no DB work). Awaited before
                // we persist results, so context access is never concurrent.
                using var progressCts = new CancellationTokenSource();
                var progressTask = Task.Run(async () =>
                {
                    while (!progressCts.IsCancellationRequested)
                    {
                        try { await Task.Delay(500, progressCts.Token); }
                        catch (TaskCanceledException) { break; }

                        run.CompletedCombinations = Volatile.Read(ref completed);
                        lock (winnerLock)
                        {
                            if (bestByCriterion.TryGetValue("composite", out var c))
                            {
                                run.BestCompositeScore = c.Metrics.CompositeScore;
                                run.BestParamsJson = JsonSerializer.Serialize(c.Combo);
                            }
                        }
                        await _runRepo.UpdateItemAsync(run);
                    }
                });

                // Leave a core free so Kestrel can still answer status polls and the
                // progress writer can run — otherwise a large sweep starves the thread
                // pool and the progress bar appears frozen until the run finishes.
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1)
                };

                Parallel.For(0, combos.Count, parallelOptions, i =>
                {
                    var combo = combos[i];
                    ComboMetrics metrics;
                    try
                    {
                        var testStrategy = CloneStrategyWithCombo(strategy, combo);
                        var (finalBalance, results) = _backtestService.Simulate(
                            testStrategy, initialBalance, candles, session.Id, session.StartDate, session.EndDate);
                        metrics = ComputeMetrics(initialBalance, finalBalance, results);
                    }
                    catch (InvalidOperationException)
                    {
                        metrics = new ComboMetrics();
                    }

                    Consider(combo, metrics);
                    Interlocked.Increment(ref completed);
                });

                progressCts.Cancel();
                await progressTask;

                // Persist the distinct winning combos, then map each criterion to its row.
                var winners = new List<(string Key, Dictionary<string, decimal> Combo, ComboMetrics Metrics)>();
                lock (winnerLock)
                {
                    foreach (var (key, _) in Criteria)
                        if (bestByCriterion.TryGetValue(key, out var w))
                            winners.Add((key, w.Combo, w.Metrics));
                }

                var rowsByJson = new Dictionary<string, OptimizationResult>();
                foreach (var w in winners)
                {
                    var json = JsonSerializer.Serialize(w.Combo);
                    if (!rowsByJson.ContainsKey(json))
                    {
                        rowsByJson[json] = new OptimizationResult
                        {
                            OptimizationRunId = runId,
                            ParamsJson = json,
                            CompositeScore = w.Metrics.CompositeScore,
                            Profit = w.Metrics.Profit,
                            ProfitFactor = w.Metrics.ProfitFactor,
                            WinRate = w.Metrics.WinRate,
                            MaxDrawdownPercent = w.Metrics.MaxDrawdownPercent,
                            TotalTrades = w.Metrics.TotalTrades,
                            SharpeApprox = w.Metrics.SharpeApprox
                        };
                    }
                }
                await _optResultRepo.InsertRangeAsync(rowsByJson.Values.ToList());

                run.CompletedCombinations = combos.Count;
                if (bestByCriterion.TryGetValue("composite", out var best))
                {
                    run.BestCompositeScore = best.Metrics.CompositeScore;
                    run.BestParamsJson = JsonSerializer.Serialize(best.Combo);
                }
                run.Status = "completed";
                await _runRepo.UpdateItemAsync(run);

                _logger.LogInformation(
                    "Optimization completed — Run {RunId}, Session {SessionId}, " +
                    "{TotalCombos} combos evaluated, Best composite score {BestScore:F2}",
                    runId, sessionId, combos.Count,
                    bestByCriterion.GetValueOrDefault("composite").Metrics?.CompositeScore ?? 0);
            }
            catch (Exception ex)
            {
                run.Status = "failed";
                run.ErrorMessage = ex.Message;
                await _runRepo.UpdateItemAsync(run);

                _logger.LogError(ex,
                    "Optimization failed — Run {RunId}, Session {SessionId}: {ErrorMessage}",
                    runId, sessionId, ex.Message);
            }
        }

        // Whether candidate is a strictly better winner than the incumbent for the
        // given criterion. Profit/factor/win-rate/drawdown only consider combos that
        // actually traded, so an idle (0-trade) combo can't win them on a flat metric.
        private static bool IsBetter(string criterion, ComboMetrics candidate, ComboMetrics incumbent)
        {
            switch (criterion)
            {
                case "profit":
                    return candidate.TotalTrades > 0
                        && (incumbent.TotalTrades == 0 || candidate.Profit > incumbent.Profit);
                case "profitFactor":
                    return candidate.TotalTrades > 0
                        && (incumbent.TotalTrades == 0 || candidate.ProfitFactor > incumbent.ProfitFactor);
                case "winRate":
                    return candidate.TotalTrades > 0
                        && (incumbent.TotalTrades == 0 || candidate.WinRate > incumbent.WinRate);
                case "drawdown":
                    // Drawdown is stored as a non-positive percent; closer to 0 is better.
                    return candidate.TotalTrades > 0
                        && (incumbent.TotalTrades == 0 || candidate.MaxDrawdownPercent > incumbent.MaxDrawdownPercent);
                default: // composite
                    return candidate.CompositeScore > incumbent.CompositeScore;
            }
        }

        // ── Apply ─────────────────────────────────────────────────────────

        // Applies a specific result's parameter combination (the user's chosen
        // criterion winner) to the real strategy and its indicators, marks the
        // session as optimized, and re-runs the backtest so the session's results
        // reflect the new parameters immediately. When resultId is null, falls back
        // to the run's composite-best parameters.
        //
        // Note: indicators are shared (N:N) entities, so updating an indicator's
        // Period/Threshold here affects every strategy that uses it.
        public async Task ApplyBestParamsAsync(OptimizationRun run, int? resultId = null)
        {
            string? paramsJson;
            if (resultId.HasValue)
            {
                var chosen = run.Results.FirstOrDefault(r => r.Id == resultId.Value);
                if (chosen == null)
                    throw new InvalidOperationException("The selected optimization result was not found.");
                paramsJson = chosen.ParamsJson;
            }
            else
            {
                paramsJson = run.BestParamsJson;
            }

            if (string.IsNullOrEmpty(paramsJson))
                throw new InvalidOperationException("This optimization run has no parameters to apply.");

            var bestParams = JsonSerializer.Deserialize<Dictionary<string, decimal>>(paramsJson)
                ?? new Dictionary<string, decimal>();

            var session = await _sessionRepo.GetItemAsync(run.BacktestSessionId);
            if (session == null) throw new InvalidOperationException("Session not found.");

            var strategy = await _strategyRepo.GetItemAsync(session.StrategyId);
            if (strategy == null) throw new InvalidOperationException("Strategy not found.");

            foreach (var (key, value) in bestParams)
            {
                switch (key)
                {
                    case "SL": strategy.StopLossPercent = value; break;
                    case "TP": strategy.TakeProfitPercent = value; break;
                    case "TS": strategy.TrailingStopPercent = value; break;
                    case "PS": strategy.PositionSizePercent = value; break;
                    default:
                        if (key.StartsWith("Indicator_"))
                        {
                            var parts = key.Split('_');
                            var indicatorId = int.Parse(parts[1]);
                            var field = parts[2];
                            var indicator = await _indicatorRepo.GetItemAsync(indicatorId);
                            if (indicator != null)
                            {
                                if (field == "Period") indicator.Period = (int)value;
                                else if (field == "Threshold") indicator.Threshold = value;
                                await _indicatorRepo.UpdateItemAsync(indicator);
                            }
                        }
                        break;
                }
            }

            strategy.LastModifiedAt = DateTime.Now;
            await _strategyRepo.UpdateItemAsync(strategy);

            session.Strategy = strategy;
            session.IsOptimized = true;
            await _backtestService.RunBacktestAsync(session);
            await _sessionRepo.UpdateItemAsync(session);

            _logger.LogInformation(
                "Optimization params applied — Run {RunId}, Session {SessionId}, Strategy \"{Strategy}\"",
                run.Id, session.Id, strategy.Name);
        }

        // ── Result presentation ────────────────────────────────────────────

        // Builds the DTO shown on the result card, used both by the polling API and
        // when the Optimize page loads with a previously-completed run. Returns one
        // selectable option per criterion (composite, profit, profit factor, win
        // rate, drawdown), each pointing at the persisted winning result.
        public OptimizationResultDTO BuildResultDto(OptimizationRun run, BacktestSession? session, BacktestStrategy? strategy)
        {
            var dto = new OptimizationResultDTO
            {
                RunId = run.Id,
                TotalCombinations = run.TotalCombinations
            };

            var initialBalance = session?.InitialBalance ?? 0m;

            foreach (var (key, label) in Criteria)
            {
                var result = SelectResultForCriterion(key, run.Results);
                if (result == null) continue;

                var ps = string.IsNullOrEmpty(result.ParamsJson)
                    ? new Dictionary<string, decimal>()
                    : JsonSerializer.Deserialize<Dictionary<string, decimal>>(result.ParamsJson) ?? new Dictionary<string, decimal>();

                dto.Options.Add(new OptimizationOptionDTO
                {
                    Key = key,
                    Label = label,
                    ResultId = result.Id,
                    Score = result.CompositeScore,
                    Profit = result.Profit,
                    ProfitPercent = initialBalance > 0 ? result.Profit / initialBalance * 100m : 0,
                    ProfitFactor = result.ProfitFactor,
                    WinRate = result.WinRate,
                    MaxDrawdownPercent = result.MaxDrawdownPercent,
                    TotalTrades = result.TotalTrades,
                    SharpeApprox = result.SharpeApprox,
                    ParamDisplay = BuildParamDisplay(ps, strategy)
                });
            }

            return dto;
        }

        // Picks, from the persisted winners, the result that best satisfies a
        // criterion. Mirrors IsBetter() but operates over OptimizationResult rows.
        private static OptimizationResult? SelectResultForCriterion(string criterion, ICollection<OptimizationResult> results)
        {
            var traded = results.Where(r => r.TotalTrades > 0).ToList();
            switch (criterion)
            {
                case "profit":
                    return traded.OrderByDescending(r => r.Profit).FirstOrDefault();
                case "profitFactor":
                    return traded.OrderByDescending(r => r.ProfitFactor).FirstOrDefault();
                case "winRate":
                    return traded.OrderByDescending(r => r.WinRate).FirstOrDefault();
                case "drawdown":
                    return traded.OrderByDescending(r => r.MaxDrawdownPercent).FirstOrDefault();
                default: // composite
                    return results.OrderByDescending(r => r.CompositeScore).FirstOrDefault();
            }
        }

        public static List<ParamDisplayItem> BuildParamDisplay(Dictionary<string, decimal> bestParams, BacktestStrategy? strategy)
        {
            var items = new List<ParamDisplayItem>();
            var indicatorNames = new Dictionary<int, string>();

            if (strategy != null)
            {
                foreach (var ind in strategy.Indicators ?? Enumerable.Empty<Indicator>())
                    indicatorNames[ind.Id] = ind.Name ?? $"Indicator {ind.Id}";
                foreach (var c in strategy.Comparisons ?? Enumerable.Empty<IndicatorComparison>())
                {
                    if (c.IndicatorA != null) indicatorNames[c.IndicatorA.Id] = c.IndicatorA.Name ?? $"Indicator {c.IndicatorA.Id}";
                    if (c.IndicatorB != null) indicatorNames[c.IndicatorB.Id] = c.IndicatorB.Name ?? $"Indicator {c.IndicatorB.Id}";
                }
            }

            foreach (var (key, value) in bestParams)
            {
                string label;
                string val = value.ToString("0.##");

                switch (key)
                {
                    case "SL": label = "Stop Loss"; val += "%"; break;
                    case "TP": label = "Take Profit"; val += "%"; break;
                    case "TS": label = "Trailing Stop"; val += "%"; break;
                    case "PS": label = "Position Size"; val += "%"; break;
                    default:
                        if (key.StartsWith("Indicator_"))
                        {
                            var parts = key.Split('_');
                            var indicatorId = int.Parse(parts[1]);
                            var field = parts[2];
                            var name = indicatorNames.TryGetValue(indicatorId, out var n) ? n : $"Indicator {indicatorId}";
                            label = $"{name} {field}";
                        }
                        else
                        {
                            label = key;
                        }
                        break;
                }

                items.Add(new ParamDisplayItem { Label = label, Value = val });
            }

            return items;
        }
    }
}
