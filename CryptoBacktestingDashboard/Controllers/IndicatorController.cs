using CryptoBacktestingDashboard.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CryptoBacktestingDashboard.Controllers
{
    public class IndicatorController : Controller
    {
        private readonly IndicatorMockRepository _indicatorRepository;

        public IndicatorController(IndicatorMockRepository indicatorRepository)
        {
            _indicatorRepository = indicatorRepository;
        }

        public IActionResult Index()
        {
            var indicators = _indicatorRepository.GetAll();
            return View(indicators);
        }

        public IActionResult Details(int id)
        {
            var indicator = _indicatorRepository.GetById(id);
            if (indicator == null)
                return NotFound();

            return View(indicator);
        }
    }
}
