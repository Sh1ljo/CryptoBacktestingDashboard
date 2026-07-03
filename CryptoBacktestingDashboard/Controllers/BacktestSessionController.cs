using CryptoBacktestingDashboard.Models;
using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Repositories.EF;
using CryptoBacktestingDashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Controllers
{
    [Route("backtests")]
    [Authorize]
    public class BacktestSessionController : Controller
    {
        private readonly BacktestSessionRepository _sessionRepository;
        private readonly BacktestStrategyRepository _strategyRepository;
        private readonly BacktestService _backtestService;
        private readonly OptimizationRunRepository _optimizationRunRepository;
        private readonly OptimizationService _optimizationService;
        private readonly UserManager<AppUser> _userManager;

        public BacktestSessionController(
            BacktestSessionRepository sessionRepository,
            BacktestStrategyRepository strategyRepository,
            BacktestService backtestService,
            OptimizationRunRepository optimizationRunRepository,
            OptimizationService optimizationService,
            UserManager<AppUser> userManager)
        {
            _sessionRepository = sessionRepository;
            _strategyRepository = strategyRepository;
            _backtestService = backtestService;
            _optimizationRunRepository = optimizationRunRepository;
            _optimizationService = optimizationService;
            _userManager = userManager;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? q = null, int page = 1, int pageSize = 9)
        {
            var userId = _userManager.GetUserId(User);
            var sessions = await _sessionRepository.GetItemsAsync(userId);

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
            var userId = _userManager.GetUserId(User);
            var session = await _sessionRepository.GetItemAsync(id, userId);
            if (session == null)
                return NotFound();

            return View(session);
        }

        [Authorize(Roles = "Admin,User")]
        [HttpGet("create")]
        public IActionResult Create()
        {
            return View(new BacktestSession { StartDate = System.DateTime.Today, EndDate = System.DateTime.Today.AddDays(30) });
        }

        [Authorize(Roles = "Admin,User")]
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BacktestSession model)
        {
            // Set server-side props first, then re-validate so they don't cause errors
            model.AppUserId = _userManager.GetUserId(User);
            ModelState.Clear();
            TryValidateModel(model);

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

        [Authorize(Roles = "Admin,User")]
        [HttpGet("edit/{id}")]
        [ActionName("Edit")]
        public async Task<IActionResult> EditGet(int id)
        {
            var userId = _userManager.GetUserId(User);
            var session = await _sessionRepository.GetItemAsync(id, userId);
            if (session == null)
                return NotFound();
            return View(session);
        }

        [Authorize(Roles = "Admin,User")]
        [HttpPost("edit/{id}")]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int id)
        {
            var userId = _userManager.GetUserId(User);
            var session = await _sessionRepository.GetItemAsync(id, userId);
            if (session == null)
                return NotFound();

            // TryUpdateModelAsync returns false if there are any validation errors
            // (including navigational properties missing). We will ignore its return 
            // value since we manually scrub the ModelState before checking IsValid.
            await TryUpdateModelAsync(session);

            // Set server-side props, clear binding errors, re-validate with values set
            session.AppUserId = userId;
            ModelState.Clear();
            TryValidateModel(session);

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

        [HttpGet("{id}/optimize")]
        public async Task<IActionResult> Optimize(int id)
        {
            var userId = _userManager.GetUserId(User);
            var session = await _sessionRepository.GetItemAsync(id, userId);
            if (session == null)
                return NotFound();

            var strategy = await _strategyRepository.GetItemAsync(session.StrategyId);
            if (strategy == null)
                return NotFound();

            session.Strategy = strategy;

            // Show the most recent completed run, if any, when the page loads.
            var runs = await _optimizationRunRepository.GetBySessionIdAsync(id);
            var latestCompleted = runs.FirstOrDefault(r => r.Status == "completed");
            if (latestCompleted != null)
                ViewBag.LatestResult = _optimizationService.BuildResultDto(latestCompleted, session, strategy);

            return View(session);
        }

        [Authorize(Roles = "Admin,User")]
        [HttpPost("{id}/run")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Run(int id)
        {
            var userId = _userManager.GetUserId(User);
            var session = await _sessionRepository.GetItemAsync(id, userId);
            if (session == null)
                return NotFound();

            // Load the full strategy with indicators and risk management
            var strategy = await _strategyRepository.GetItemAsync(session.StrategyId, userId);
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

        [Authorize(Roles = "Admin,User")]
        [HttpPost("delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userManager.GetUserId(User);

            // Admins may delete any session; regular users only their own.
            if (User.IsInRole("Admin"))
                await _sessionRepository.DeleteItemAsync(id);
            else
                await _sessionRepository.DeleteItemAsync(id, userId);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
