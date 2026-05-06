using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Repositories.EF;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Controllers
{
    public class CryptoPairController : Controller
    {
        private readonly CryptoPairRepository _pairRepository;

        public CryptoPairController(CryptoPairRepository pairRepository)
        {
            _pairRepository = pairRepository;
        }

        public async Task<IActionResult> Index()
        {
            var pairs = await _pairRepository.GetItemsAsync();
            return View(pairs);
        }

        public async Task<IActionResult> Details(int id)
        {
            var pair = await _pairRepository.GetItemAsync(id);
            if (pair == null)
                return NotFound();

            return View(pair);
        }
    }
}
