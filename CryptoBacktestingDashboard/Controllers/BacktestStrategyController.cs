using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Repositories.EF;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Controllers
{
    [Route("strategies")]
    public class BacktestStrategyController : Controller
    {
        private readonly BacktestStrategyRepository _strategyRepository;

        public BacktestStrategyController(BacktestStrategyRepository strategyRepository)
        {
            _strategyRepository = strategyRepository;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var strategies = await _strategyRepository.GetItemsAsync();
            return View(strategies);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var strategy = await _strategyRepository.GetItemAsync(id);
            if (strategy == null)
                return NotFound();

            return View(strategy);
        }
    }
}
