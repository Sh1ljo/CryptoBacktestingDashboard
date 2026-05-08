using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Repositories.EF;
using Microsoft.AspNetCore.Mvc;
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
        public async Task<IActionResult> Index()
        {
            var pairs = await _pairRepository.GetItemsAsync();
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
    }
}
