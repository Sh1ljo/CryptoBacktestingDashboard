using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Repositories.EF;
using CryptoBacktestingDashboard.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Controllers
{
    [Route("pairs")]
    [Authorize]
    public class CryptoPairController : Controller
    {
        private readonly CryptoPairRepository _pairRepository;
        private readonly MarketDataService _marketDataService;
        private readonly CandleDataRepository _candleDataRepository;

        public CryptoPairController(CryptoPairRepository pairRepository, MarketDataService marketDataService, CandleDataRepository candleDataRepository)
        {
            _pairRepository = pairRepository;
            _marketDataService = marketDataService;
            _candleDataRepository = candleDataRepository;
        }

        [AllowAnonymous]
        [HttpGet("")]
        public async Task<IActionResult> Index(string? q = null, int page = 1, int pageSize = 9)
        {
            var pairs = await _pairRepository.GetItemsAsync();
            if (!string.IsNullOrEmpty(q))
            {
                pairs = pairs.Where(p => p.Symbol?.Contains(q, System.StringComparison.OrdinalIgnoreCase) ?? false).ToList();
            }

            var totalCount = pairs.Count;
            var totalPages = (int)System.Math.Ceiling(totalCount / (double)pageSize);
            page = System.Math.Max(1, System.Math.Min(page, System.Math.Max(1, totalPages)));
            pairs = pairs.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var priceChanges = new Dictionary<int, decimal?>();
            var candleCounts = new Dictionary<int, int>();
            foreach (var pair in pairs)
            {
                var change = await _marketDataService.GetPriceChangePercentageAsync(pair.Id);
                priceChanges[pair.Id] = change;
                // GetItemsAsync doesn't load candle history; count it explicitly for the card.
                candleCounts[pair.Id] = await _candleDataRepository.CountByPairIdAsync(pair.Id);
            }

            ViewData["PriceChanges"] = priceChanges;
            ViewData["CandleCounts"] = candleCounts;
            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["TotalCount"] = totalCount;
            ViewData["Query"] = q;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_PairListPartial", pairs);
            }

            return View(pairs);
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var pair = await _pairRepository.GetItemAsync(id);
            if (pair == null)
                return NotFound();

            // Calculate price change percentage
            var priceChange = await _marketDataService.GetPriceChangePercentageAsync(id);
            ViewData["PriceChangePercentage"] = priceChange;

            return View(pair);
        }

        // Endpoint for Autocomplete AJAX dropdown
        [AllowAnonymous]
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

        [AllowAnonymous]
        [HttpGet("{id}/candles")]
        public async Task<IActionResult> Candles(int id, DateTime? from = null, DateTime? to = null)
        {
            var effectiveFrom = from ?? DateTime.Today.AddYears(-2);
            var effectiveTo = to ?? DateTime.Today;
            var candles = await _candleDataRepository.GetByPairIdAndDateRangeAsync(id, effectiveFrom, effectiveTo);
            var data = candles.Select(c => new
            {
                time = c.OpenTime.ToString("yyyy-MM-dd"),
                open = c.Open,
                high = c.High,
                low = c.Low,
                close = c.Close
            });
            return Json(data);
        }

        // Lightweight JSON endpoint so forms can warn when a pair has no candle data
        // before the user wastes a run that would fail with "not enough candle data".
        [AllowAnonymous]
        [HttpGet("{id}/data-status")]
        public async Task<IActionResult> DataStatus(int id)
        {
            var count = await _candleDataRepository.CountByPairIdAsync(id);
            var (earliest, latest) = await _candleDataRepository.GetDateRangeByPairIdAsync(id);
            return Json(new
            {
                count,
                hasData = count > 0,
                earliest = earliest?.ToString("yyyy-MM-dd"),
                latest = latest?.ToString("yyyy-MM-dd")
            });
        }

        [Authorize(Roles = "Admin,User")]
        [HttpPost("{id}/fetch-data")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FetchData(int id, string interval = "1d", int daysBack = 365)
        {
            var (inserted, error) = await _marketDataService.FetchCandlesAsync(id, interval, daysBack);

            if (!string.IsNullOrEmpty(error))
                TempData["Error"] = error;
            else
                TempData["Success"] = $"Fetched {inserted} new candles.";

            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = "Admin,User")]
        [HttpPost("{id}/clear-data")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearData(int id)
        {
            var pair = await _pairRepository.GetItemAsync(id);
            if (pair == null)
                return NotFound();

            await _candleDataRepository.DeleteByPairIdAsync(id);
            pair.CurrentPrice = 0;
            await _pairRepository.UpdateItemAsync(pair);

            TempData["Success"] = "Candle data cleared for this pair.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [Authorize(Roles = "Admin,User")]
        [HttpGet("create")]
        public IActionResult Create()
        {
            return View(new CryptoPair());
        }

        [Authorize(Roles = "Admin,User")]
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

        [Authorize(Roles = "Admin,User")]
        [HttpGet("edit/{id}")]
        [ActionName("Edit")]
        public async Task<IActionResult> EditGet(int id)
        {
            var pair = await _pairRepository.GetItemAsync(id);
            if (pair == null)
                return NotFound();
            return View(pair);
        }

        [Authorize(Roles = "Admin,User")]
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

        [Authorize(Roles = "Admin")]
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

