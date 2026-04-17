using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CryptoBacktestingDashboard.Controllers
{
    public class CryptoPairController : Controller
    {
        private readonly CryptoPairMockRepository _pairRepository;

        public CryptoPairController(CryptoPairMockRepository pairRepository)
        {
            _pairRepository = pairRepository;
        }

        public IActionResult Index()
        {
            var pairs = _pairRepository.GetAll();
            return View(pairs);
        }

        public IActionResult Details(int id)
        {
            var pair = _pairRepository.GetById(id);
            if (pair == null)
                return NotFound();

            return View(pair);
        }
    }
}
