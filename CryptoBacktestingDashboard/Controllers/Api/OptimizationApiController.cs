using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Models.DTO;
using CryptoBacktestingDashboard.Repositories.EF;
using CryptoBacktestingDashboard.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Controllers.Api
{
    [Route("api/optimization")]
    [ApiController]
    public class OptimizationApiController : ControllerBase
    {
        private readonly BacktestSessionRepository _sessionRepo;
        private readonly BacktestStrategyRepository _strategyRepo;
        private readonly OptimizationRunRepository _runRepo;
        private readonly OptimizationService _optimizationService;
        private readonly IServiceScopeFactory _scopeFactory;

        public OptimizationApiController(
            BacktestSessionRepository sessionRepo,
            BacktestStrategyRepository strategyRepo,
            OptimizationRunRepository runRepo,
            OptimizationService optimizationService,
            IServiceScopeFactory scopeFactory)
        {
            _sessionRepo = sessionRepo;
            _strategyRepo = strategyRepo;
            _runRepo = runRepo;
            _optimizationService = optimizationService;
            _scopeFactory = scopeFactory;
        }

        [HttpPost("{sessionId}/start")]
        public async Task<IActionResult> Start(int sessionId, [FromBody] OptimizationProfileDTO dto)
        {
            var session = await _sessionRepo.GetItemAsync(sessionId);
            if (session == null) return NotFound(new { error = "Session not found." });

            var strategy = await _strategyRepo.GetItemAsync(session.StrategyId);
            if (strategy == null) return NotFound(new { error = "Strategy not found." });
            session.Strategy = strategy;

            var hasIndicators = (strategy.Indicators?.Count ?? 0) > 0;
            var hasComparisons = (strategy.Comparisons?.Count ?? 0) > 0;
            if (!hasIndicators && !hasComparisons)
                return BadRequest(new { error = "Strategy has no indicators to optimize." });

            var profile = _optimizationService.BuildProfile(dto);
            var combos = _optimizationService.ExpandParameterGrid(profile, strategy);

            if (combos.Count <= 1)
                return BadRequest(new { error = "Configure at least one parameter range to sweep." });

            var run = new OptimizationRun
            {
                BacktestSessionId = sessionId,
                ParameterGridJson = JsonSerializer.Serialize(profile),
                TotalCombinations = combos.Count,
                Status = "running"
            };
            await _runRepo.InsertItemAsync(run);

            var runId = run.Id;
            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<OptimizationService>();
                await service.RunOptimizationLoopAsync(runId, sessionId, combos);
            });

            return Ok(new { runId, totalCombinations = combos.Count });
        }

        [HttpGet("{runId}/status")]
        public async Task<IActionResult> Status(int runId)
        {
            var run = await _runRepo.GetItemAsync(runId);
            if (run == null) return NotFound();

            return Ok(new OptimizationStatusDTO
            {
                RunId = run.Id,
                Status = run.Status,
                Completed = run.CompletedCombinations,
                Total = run.TotalCombinations,
                BestScoreSoFar = run.BestCompositeScore,
                ErrorMessage = run.ErrorMessage
            });
        }

        [HttpGet("{runId}/result")]
        public async Task<IActionResult> Result(int runId)
        {
            var run = await _runRepo.GetItemAsync(runId);
            if (run == null) return NotFound();
            if (run.Status != "completed") return BadRequest(new { error = "Optimization is not complete yet." });

            var session = await _sessionRepo.GetItemAsync(run.BacktestSessionId);
            var strategy = session != null ? await _strategyRepo.GetItemAsync(session.StrategyId) : null;

            return Ok(_optimizationService.BuildResultDto(run, session, strategy));
        }

        public class ApplyRequest
        {
            // The OptimizationResult to apply (the user's chosen criterion winner).
            // When null, the run's composite-best parameters are applied.
            public int? ResultId { get; set; }
        }

        [HttpPost("{runId}/apply")]
        public async Task<IActionResult> Apply(int runId, [FromBody] ApplyRequest? body)
        {
            var run = await _runRepo.GetItemAsync(runId);
            if (run == null) return NotFound();

            try
            {
                await _optimizationService.ApplyBestParamsAsync(run, body?.ResultId);
                return Ok(new { success = true });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
