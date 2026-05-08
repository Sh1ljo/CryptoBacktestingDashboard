using CryptoBacktestingDashboard.Repositories.EF;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Controllers
{
    [Route("indicators")]
    public class IndicatorController : Controller
    {
        private readonly IndicatorRepository _indicatorRepository;

        public IndicatorController(IndicatorRepository indicatorRepository)
        {
            _indicatorRepository = indicatorRepository;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var indicators = await _indicatorRepository.GetItemsAsync();
            return View(indicators);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var indicator = await _indicatorRepository.GetItemAsync(id);
            if (indicator == null)
                return NotFound();

            return View(indicator);
        }
    }
}
