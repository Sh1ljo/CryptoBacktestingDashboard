using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CryptoBacktestingDashboard.Controllers
{
    public class BacktestStrategyController : Controller
    {
        private readonly BacktestStrategyMockRepository _strategyRepository;

        public BacktestStrategyController(BacktestStrategyMockRepository strategyRepository)
        {
            _strategyRepository = strategyRepository;
        }

        public IActionResult Index()
        {
            var strategies = _strategyRepository.GetAll();
            return View(strategies);
        }

        public IActionResult Details(int id)
        {
            var strategy = _strategyRepository.GetById(id);
            if (strategy == null)
                return NotFound();

            return View(strategy);
        }
    }
}
