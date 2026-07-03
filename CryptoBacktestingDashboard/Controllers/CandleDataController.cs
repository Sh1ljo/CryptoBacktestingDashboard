using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Repositories.EF;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Controllers
{
    [Route("candles")]
    [Authorize]
    public class CandleDataController : Controller
    {
        private readonly CandleDataRepository _candleRepository;
        private readonly CryptoPairRepository _pairRepository;

        public CandleDataController(CandleDataRepository candleRepository, CryptoPairRepository pairRepository)
        {
            _candleRepository = candleRepository;
            _pairRepository = pairRepository;
        }

        [AllowAnonymous]
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var candles = await _candleRepository.GetItemsAsync();
            return View(candles);
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var candle = await _candleRepository.GetItemAsync(id);
            if (candle == null)
                return NotFound();

            return View(candle);
        }

        [Authorize(Roles = "Admin,User")]
        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            var pairs = await _pairRepository.GetItemsAsync();
            ViewData["CryptoPairs"] = pairs;
            return View(new CandleData());
        }

        [Authorize(Roles = "Admin,User")]
        [HttpPost("create")]
        public async Task<IActionResult> Create(CandleData candle)
        {
            if (!ModelState.IsValid)
            {
                var pairs = await _pairRepository.GetItemsAsync();
                ViewData["CryptoPairs"] = pairs;
                return View(candle);
            }

            await _candleRepository.InsertItemAsync(candle);
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "Admin,User")]
        [HttpGet("{id}/edit")]
        public async Task<IActionResult> Edit(int id)
        {
            var candle = await _candleRepository.GetItemAsync(id);
            if (candle == null)
                return NotFound();

            var pairs = await _pairRepository.GetItemsAsync();
            ViewData["CryptoPairs"] = pairs;
            return View(candle);
        }

        [Authorize(Roles = "Admin,User")]
        [HttpPost("{id}/edit")]
        public async Task<IActionResult> Edit(int id, CandleData candle)
        {
            if (id != candle.Id)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                var pairs = await _pairRepository.GetItemsAsync();
                ViewData["CryptoPairs"] = pairs;
                return View(candle);
            }

            await _candleRepository.UpdateItemAsync(candle);
            return RedirectToAction("Details", new { id = candle.Id });
        }
    }
}
