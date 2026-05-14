using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Repositories.EF;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Controllers
{
    [Route("indicators")]
    public class IndicatorController : Controller
    {
        private readonly IndicatorRepository _indicatorRepository;

        public IndicatorController(IndicatorRepository indicatorRepository)
        {
            _indicatorRepository = indicatorRepository;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? q = null)
        {
            var indicators = await _indicatorRepository.GetItemsAsync();

            if (!string.IsNullOrEmpty(q))
            {
                indicators = indicators.Where(i =>
                    (i.Name?.Contains(q, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (i.Description?.Contains(q, System.StringComparison.OrdinalIgnoreCase) ?? false)
                ).ToList();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_IndicatorListPartial", indicators);
            }

            return View(indicators);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var indicator = await _indicatorRepository.GetItemAsync(id);
            if (indicator == null)
                return NotFound();

            return View(indicator);
        }

        [HttpGet("create")]
        public IActionResult Create()
        {
            return View(new Indicator { CreatedAt = System.DateTime.Now });
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Indicator model)
        {
            ModelState.Remove("Strategies");

            if (ModelState.IsValid)
            {
                await _indicatorRepository.InsertItemAsync(model);
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpGet("edit/{id}")]
        [ActionName("Edit")]
        public async Task<IActionResult> EditGet(int id)
        {
            var indicator = await _indicatorRepository.GetItemAsync(id);
            if (indicator == null)
                return NotFound();
            return View(indicator);
        }

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

            if (ok && ModelState.IsValid)
            {
                await _indicatorRepository.UpdateItemAsync(indicator);
                return RedirectToAction(nameof(Index));
            }

            return View(indicator);
        }

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
