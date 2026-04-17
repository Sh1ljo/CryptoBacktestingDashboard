using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CryptoBacktestingDashboard.Controllers
{
    public class BacktestSessionController : Controller
    {
        private readonly BacktestSessionMockRepository _sessionRepository;

        public BacktestSessionController(BacktestSessionMockRepository sessionRepository)
        {
            _sessionRepository = sessionRepository;
        }

        public IActionResult Index()
        {
            var sessions = _sessionRepository.GetAll();
            return View(sessions);
        }

        public IActionResult Details(int id)
        {
            var session = _sessionRepository.GetById(id);
            if (session == null)
                return NotFound();

            return View(session);
        }
    }
}
