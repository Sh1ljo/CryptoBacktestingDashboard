using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Repositories.EF;
using CryptoBacktestingDashboard.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Controllers
{
    [Route("backtests")]
    public class BacktestSessionController : Controller
    {
        private readonly BacktestSessionRepository _sessionRepository;
        private readonly BacktestStrategyRepository _strategyRepository;
        private readonly BacktestService _backtestService;

        public BacktestSessionController(
            BacktestSessionRepository sessionRepository,
            BacktestStrategyRepository strategyRepository,
            BacktestService backtestService)
        {
            _sessionRepository = sessionRepository;
            _strategyRepository = strategyRepository;
            _backtestService = backtestService;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? q = null, int page = 1, int pageSize = 9)
        {
            var sessions = await _sessionRepository.GetItemsAsync();

            if (!string.IsNullOrEmpty(q))
            {
                sessions = sessions.Where(s =>
                    (s.CryptoPair?.Symbol?.Contains(q, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.Strategy?.Name?.Contains(q, System.StringComparison.OrdinalIgnoreCase) ?? false)
                ).ToList();
            }

            sessions = sessions.OrderByDescending(s => s.GetProfit()).ToList();

            var totalCount = sessions.Count;
            var totalPages = (int)System.Math.Ceiling(totalCount / (double)pageSize);
            page = System.Math.Max(1, System.Math.Min(page, System.Math.Max(1, totalPages)));
            sessions = sessions.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["TotalCount"] = totalCount;
            ViewData["Query"] = q;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_SessionListPartial", sessions);
            }

            return View(sessions);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var session = await _sessionRepository.GetItemAsync(id);
            if (session == null)
                return NotFound();

            return View(session);
        }

        [HttpGet("create")]
        public IActionResult Create()
        {
            return View(new BacktestSession { StartDate = System.DateTime.Today, EndDate = System.DateTime.Today.AddDays(30) });
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BacktestSession model)
        {
            // The un-bound navigational properties shouldn't prevent session creation
            ModelState.Remove("Strategy");
            ModelState.Remove("CryptoPair");
            ModelState.Remove("Results");

            if (ModelState.IsValid)
            {
                await _sessionRepository.InsertItemAsync(model);
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpGet("edit/{id}")]
        [ActionName("Edit")]
        public async Task<IActionResult> EditGet(int id)
        {
            var session = await _sessionRepository.GetItemAsync(id);
            if (session == null)
                return NotFound();
            return View(session);
        }

        [HttpPost("edit/{id}")]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int id)
        {
            var session = await _sessionRepository.GetItemAsync(id);
            if (session == null)
                return NotFound();

            // TryUpdateModelAsync returns false if there are any validation errors
            // (including navigational properties missing). We will ignore its return 
            // value since we manually scrub the ModelState before checking IsValid.
            await TryUpdateModelAsync(session);

            ModelState.Remove("Strategy");
            ModelState.Remove("CryptoPair");
            ModelState.Remove("Results");

            if (ModelState.IsValid)
            {
                await _sessionRepository.UpdateItemAsync(session);
                return RedirectToAction(nameof(Index));
            }

            return View(session);
        }

        [HttpPost("{id}/run")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Run(int id)
        {
            var session = await _sessionRepository.GetItemAsync(id);
            if (session == null)
                return NotFound();

            // Load the full strategy with indicators and risk management
            var strategy = await _strategyRepository.GetItemAsync(session.StrategyId);
            if (strategy == null)
                return BadRequest("Strategy not found.");

            session.Strategy = strategy;

            try
            {
                await _backtestService.RunBacktestAsync(session);
                TempData["Success"] = "Backtest completed successfully.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost("delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _sessionRepository.DeleteItemAsync(id);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
