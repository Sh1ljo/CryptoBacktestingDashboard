using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Repositories.EF;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Controllers
{
    [Route("indicators")]
    [Authorize]
    public class IndicatorController : Controller
    {
        private readonly IndicatorRepository _indicatorRepository;

        public IndicatorController(IndicatorRepository indicatorRepository)
        {
            _indicatorRepository = indicatorRepository;
        }

        [AllowAnonymous]
        [HttpGet("")]
        public async Task<IActionResult> Index(string? q = null, int page = 1, int pageSize = 9)
        {
            var indicators = await _indicatorRepository.GetItemsAsync();

            if (!string.IsNullOrEmpty(q))
            {
                indicators = indicators.Where(i =>
                    (i.Name?.Contains(q, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (i.Description?.Contains(q, System.StringComparison.OrdinalIgnoreCase) ?? false)
                ).ToList();
            }

            var totalCount = indicators.Count;
            var totalPages = (int)System.Math.Ceiling(totalCount / (double)pageSize);
            page = System.Math.Max(1, System.Math.Min(page, System.Math.Max(1, totalPages)));
            indicators = indicators.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["TotalCount"] = totalCount;
            ViewData["Query"] = q;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_IndicatorListPartial", indicators);
            }

            return View(indicators);
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var indicator = await _indicatorRepository.GetItemAsync(id);
            if (indicator == null)
                return NotFound();

            return View(indicator);
        }

        [Authorize(Roles = "Admin,User")]
        [HttpGet("create")]
        public IActionResult Create()
        {
            return View(new Indicator { CreatedAt = System.DateTime.Now });
        }

        [Authorize(Roles = "Admin,User")]
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Indicator model)
        {
            ModelState.Remove("Strategies");
            ValidateIndicator(model);

            if (ModelState.IsValid)
            {
                await _indicatorRepository.InsertItemAsync(model);
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        // Per-type sanity checks for the overloaded Period/Threshold fields.
        private void ValidateIndicator(Indicator model)
        {
            if (model.Period < 1)
                ModelState.AddModelError(nameof(Indicator.Period), "Period must be at least 1.");

            switch (model.Type)
            {
                case IndicatorType.RSI:
                case IndicatorType.Stochastic:
                    if (model.Threshold < 1 || model.Threshold > 99)
                        ModelState.AddModelError(nameof(Indicator.Threshold),
                            "Overbought level must be between 1 and 99 (e.g. 70 for RSI, 80 for Stochastic).");
                    break;

                case IndicatorType.MACD:
                    if (model.Threshold < 2)
                        ModelState.AddModelError(nameof(Indicator.Threshold),
                            "Slow EMA period must be at least 2 (standard 26).");
                    else if (model.Threshold <= model.Period)
                        ModelState.AddModelError(nameof(Indicator.Threshold),
                            "Slow EMA period should be larger than the fast EMA period.");
                    break;

                case IndicatorType.BollingerBands:
                    if (model.Threshold < 0.5m || model.Threshold > 5m)
                        ModelState.AddModelError(nameof(Indicator.Threshold),
                            "Std-dev multiplier should be between 0.5 and 5 (standard 2).");
                    break;
            }
        }

        [Authorize(Roles = "Admin,User")]
        [HttpGet("edit/{id}")]
        [ActionName("Edit")]
        public async Task<IActionResult> EditGet(int id)
        {
            var indicator = await _indicatorRepository.GetItemAsync(id);
            if (indicator == null)
                return NotFound();
            return View(indicator);
        }

        [Authorize(Roles = "Admin,User")]
        [HttpPost("edit/{id}")]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int id)
        {
            var indicator = await _indicatorRepository.GetItemAsync(id);
            if (indicator == null)
                return NotFound();

            var ok = await TryUpdateModelAsync(indicator);

            ModelState.Remove("Strategies");
            ValidateIndicator(indicator);

            if (ok && ModelState.IsValid)
            {
                await _indicatorRepository.UpdateItemAsync(indicator);
                return RedirectToAction(nameof(Index));
            }

            return View(indicator);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost("delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _indicatorRepository.DeleteItemAsync(id);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
