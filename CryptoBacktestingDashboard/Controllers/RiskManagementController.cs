using CryptoBacktestingDashboard.Repositories.EF;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Controllers
{
    public class RiskManagementController : Controller
    {
        private readonly RiskManagementRepository _riskManagementRepository;

        public RiskManagementController(RiskManagementRepository riskManagementRepository)
        {
            _riskManagementRepository = riskManagementRepository;
        }

        public async Task<IActionResult> Index()
        {
            var riskManagements = await _riskManagementRepository.GetItemsAsync();
            return View(riskManagements);
        }

        public async Task<IActionResult> Details(int id)
        {
            var riskManagement = await _riskManagementRepository.GetItemAsync(id);
            if (riskManagement == null)
                return NotFound();

            return View(riskManagement);
        }
    }
}
