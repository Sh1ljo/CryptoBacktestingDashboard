using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Repositories.EF;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Controllers
{
    [Route("strategies")]
    public class BacktestStrategyController : Controller
    {
        private readonly BacktestStrategyRepository _strategyRepository;
        private readonly RiskManagementRepository _riskManagementRepository;
        private readonly IndicatorRepository _indicatorRepository;
        private readonly IndicatorComparisonRepository _comparisonRepository;

        public BacktestStrategyController(BacktestStrategyRepository strategyRepository, RiskManagementRepository riskManagementRepository, IndicatorRepository indicatorRepository, IndicatorComparisonRepository comparisonRepository)
        {
            _strategyRepository = strategyRepository;
            _riskManagementRepository = riskManagementRepository;
            _indicatorRepository = indicatorRepository;
            _comparisonRepository = comparisonRepository;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? q = null)
        {
            var strategies = await _strategyRepository.GetItemsAsync();

            if (!string.IsNullOrEmpty(q))
            {
                strategies = strategies.Where(s =>
                    (s.Name?.Contains(q, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.Description?.Contains(q, System.StringComparison.OrdinalIgnoreCase) ?? false)
                ).ToList();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_StrategyListPartial", strategies);
            }

            return View(strategies);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var strategy = await _strategyRepository.GetItemAsync(id);
            if (strategy == null)
                return NotFound();

            return View(strategy);
        }

        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            ViewData["RiskManagementId"] = new SelectList(await _riskManagementRepository.GetItemsAsync(), "Id", "Name");
            ViewData["IndicatorIds"] = new MultiSelectList(await _indicatorRepository.GetItemsAsync(), "Id", "Name");
            var model = new BacktestStrategy { CreatedAt = System.DateTime.Now, IsActive = true };
            return View(model);
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BacktestStrategy model, int[] IndicatorIds)
        {
            ViewData["RiskManagementId"] = new SelectList(await _riskManagementRepository.GetItemsAsync(), "Id", "Name", model.RiskManagementId);
            ViewData["IndicatorIds"] = new MultiSelectList(await _indicatorRepository.GetItemsAsync(), "Id", "Name", IndicatorIds);

            ModelState.Remove("Indicators");
            ModelState.Remove("BacktestSessions");
            ModelState.Remove("Comparisons");

            if (ModelState.IsValid)
            {
                if (IndicatorIds != null)
                {
                    model.Indicators = new List<Indicator>();
                    foreach (var indicatorId in IndicatorIds)
                    {
                        var indicator = await _indicatorRepository.GetItemAsync(indicatorId);
                        if (indicator != null)
                            model.Indicators.Add(indicator);
                    }
                }
                await _strategyRepository.InsertItemAsync(model);
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpGet("edit/{id}")]
        [ActionName("Edit")]
        public async Task<IActionResult> EditGet(int id)
        {
            var strategy = await _strategyRepository.GetItemAsync(id);
            if (strategy == null)
                return NotFound();

            ViewData["RiskManagementId"] = new SelectList(await _riskManagementRepository.GetItemsAsync(), "Id", "Name", strategy.RiskManagementId);
            ViewData["IndicatorIds"] = new MultiSelectList(await _indicatorRepository.GetItemsAsync(), "Id", "Name", strategy.Indicators?.Select(i => i.Id).ToArray());
            return View(strategy);
        }

        [HttpPost("edit/{id}")]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int id, int[] IndicatorIds)
        {
            var strategy = await _strategyRepository.GetItemAsync(id);
            if (strategy == null)
                return NotFound();

            ViewData["RiskManagementId"] = new SelectList(await _riskManagementRepository.GetItemsAsync(), "Id", "Name", strategy.RiskManagementId);
            ViewData["IndicatorIds"] = new MultiSelectList(await _indicatorRepository.GetItemsAsync(), "Id", "Name", IndicatorIds);

            await TryUpdateModelAsync(strategy);

            ModelState.Remove("Indicators");
            ModelState.Remove("BacktestSessions");
            ModelState.Remove("Comparisons");

            if (ModelState.IsValid)
            {
                strategy.LastModifiedAt = System.DateTime.Now;
                if (IndicatorIds != null)
                {
                    strategy.Indicators = new List<Indicator>();
                    foreach (var indicatorId in IndicatorIds)
                    {
                        var indicator = await _indicatorRepository.GetItemAsync(indicatorId);
                        if (indicator != null)
                            strategy.Indicators.Add(indicator);
                    }
                }
                await _strategyRepository.UpdateItemAsync(strategy);
                return RedirectToAction(nameof(Index));
            }

            return View(strategy);
        }

        [HttpPost("delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _strategyRepository.DeleteItemAsync(id);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(string query)
        {
            var strategies = await _strategyRepository.GetItemsAsync();
            var results = strategies
                .Where(s => string.IsNullOrEmpty(query) || (s.Name?.Contains(query, System.StringComparison.OrdinalIgnoreCase) ?? false))
                .Take(10)
                .Select(s => new { id = s.Id, text = s.Name })
                .ToList();

            return Json(results);
        }
    }
}
