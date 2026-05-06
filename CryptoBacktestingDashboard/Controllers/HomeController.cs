using CryptoBacktestingDashboard.Models;
using CryptoBacktestingDashboard.Repositories.EF;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CryptoBacktestingDashboard.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BacktestSessionRepository _sessionRepository;
        private readonly BacktestStrategyRepository _strategyRepository;
        private readonly CryptoPairRepository _pairRepository;

        public HomeController(
            ILogger<HomeController> logger,
            BacktestSessionRepository sessionRepository,
            BacktestStrategyRepository strategyRepository,
            CryptoPairRepository pairRepository)
        {
            _logger = logger;
            _sessionRepository = sessionRepository;
            _strategyRepository = strategyRepository;
            _pairRepository = pairRepository;
        }

        public async Task<IActionResult> Index()
        {
            var sessions = await _sessionRepository.GetItemsAsync();
            var strategies = await _strategyRepository.GetItemsAsync();
            var pairs = await _pairRepository.GetItemsAsync();

            ViewData["TotalSessions"] = sessions.Count;
            ViewData["TotalStrategies"] = strategies.Count;
            ViewData["TotalPairs"] = pairs.Count;
            ViewData["TotalTrades"] = sessions.Sum(s => s.Results.Count);

            var totalProfit = sessions.Sum(s => s.GetProfit());
            ViewData["TotalProfit"] = totalProfit;
            ViewData["AverageROI"] = sessions.Count > 0 ? (decimal)sessions.Average(s => s.GetROI()) : 0m;
            ViewData["Sessions"] = sessions;
            ViewData["Strategies"] = strategies;

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
