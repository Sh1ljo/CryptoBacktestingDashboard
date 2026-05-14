using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Repositories.EF;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Controllers
{
    [Route("pairs")]
    public class CryptoPairController : Controller
    {
        private readonly CryptoPairRepository _pairRepository;

        public CryptoPairController(CryptoPairRepository pairRepository)
        {
            _pairRepository = pairRepository;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? q = null)
        {
            var pairs = await _pairRepository.GetItemsAsync();
            if (!string.IsNullOrEmpty(q))
            {
                pairs = pairs.Where(p => p.Symbol?.Contains(q, System.StringComparison.OrdinalIgnoreCase) ?? false).ToList();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_PairListPartial", pairs);
            }

            return View(pairs);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var pair = await _pairRepository.GetItemAsync(id);
            if (pair == null)
                return NotFound();

            return View(pair);
        }

        // Endpoint for Autocomplete AJAX dropdown
        [HttpGet("search")]
        public async Task<IActionResult> Search(string query)
        {
            var pairs = await _pairRepository.GetItemsAsync();
            var results = pairs
                .Where(p => string.IsNullOrEmpty(query) || (p.Symbol?.Contains(query, System.StringComparison.OrdinalIgnoreCase) ?? false))
                .Take(10)
                .Select(p => new { id = p.Id, text = p.Symbol })
                .ToList();

            return Json(results);
        }

        [HttpGet("create")]
        public IActionResult Create()
        {
            return View(new CryptoPair());
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CryptoPair model)
        {
            ModelState.Remove("CandleDataHistory");
            ModelState.Remove("BacktestSessions");

            if (ModelState.IsValid)
            {
                await _pairRepository.InsertItemAsync(model);
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpGet("edit/{id}")]
        [ActionName("Edit")]
        public async Task<IActionResult> EditGet(int id)
        {
            var pair = await _pairRepository.GetItemAsync(id);
            if (pair == null)
                return NotFound();
            return View(pair);
        }

        [HttpPost("edit/{id}")]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int id)
        {
            var pair = await _pairRepository.GetItemAsync(id);
            if (pair == null)
                return NotFound();

            var ok = await TryUpdateModelAsync(pair);

            ModelState.Remove("CandleDataHistory");
            ModelState.Remove("BacktestSessions");

            if (ok && ModelState.IsValid)
            {
                await _pairRepository.UpdateItemAsync(pair);
                return RedirectToAction(nameof(Index));
            }

            return View(pair);
        }

        [HttpPost("delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _pairRepository.DeleteItemAsync(id);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}

