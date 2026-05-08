using CryptoBacktestingDashboard.Repositories.EF;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Controllers
{
    [Route("risk")]
    public class RiskManagementController : Controller
    {
        private readonly RiskManagementRepository _riskManagementRepository;

        public RiskManagementController(RiskManagementRepository riskManagementRepository)
        {
            _riskManagementRepository = riskManagementRepository;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var riskManagements = await _riskManagementRepository.GetItemsAsync();
            return View(riskManagements);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var riskManagement = await _riskManagementRepository.GetItemAsync(id);
            if (riskManagement == null)
                return NotFound();

            return View(riskManagement);
        }
    }
}
