using CryptoBacktestingDashboard.Models;
using CryptoBacktestingDashboard.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace CryptoBacktestingDashboard.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly BacktestSessionMockRepository _sessionRepository;
        private readonly BacktestStrategyMockRepository _strategyRepository;
        private readonly CryptoPairMockRepository _pairRepository;

        public HomeController(
            ILogger<HomeController> logger,
            BacktestSessionMockRepository sessionRepository,
            BacktestStrategyMockRepository strategyRepository,
            CryptoPairMockRepository pairRepository)
        {
            _logger = logger;
            _sessionRepository = sessionRepository;
            _strategyRepository = strategyRepository;
            _pairRepository = pairRepository;
        }

        public IActionResult Index()
        {
            var sessions = _sessionRepository.GetAll();
            var strategies = _strategyRepository.GetAll();
            var pairs = _pairRepository.GetAll();

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
