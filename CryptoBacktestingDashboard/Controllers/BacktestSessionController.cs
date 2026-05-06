using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Repositories.EF;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Controllers
{
    [Route("backtests")]
    public class BacktestSessionController : Controller
    {
        private readonly BacktestSessionRepository _sessionRepository;

        public BacktestSessionController(BacktestSessionRepository sessionRepository)
        {
            _sessionRepository = sessionRepository;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var sessions = await _sessionRepository.GetItemsAsync();
            return View(sessions);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var session = await _sessionRepository.GetItemAsync(id);
            if (session == null)
                return NotFound();

            return View(session);
        }
    }
}
