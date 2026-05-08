using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Repositories.EF;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Controllers
{
    [Route("candles")]
    public class CandleDataController : Controller
    {
        private readonly CandleDataRepository _candleRepository;
        private readonly CryptoPairRepository _pairRepository;

        public CandleDataController(CandleDataRepository candleRepository, CryptoPairRepository pairRepository)
        {
            _candleRepository = candleRepository;
            _pairRepository = pairRepository;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var candles = await _candleRepository.GetItemsAsync();
            return View(candles);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var candle = await _candleRepository.GetItemAsync(id);
            if (candle == null)
                return NotFound();

            return View(candle);
        }

        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            var pairs = await _pairRepository.GetItemsAsync();
            ViewData["CryptoPairs"] = pairs;
            return View(new CandleData());
        }

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
