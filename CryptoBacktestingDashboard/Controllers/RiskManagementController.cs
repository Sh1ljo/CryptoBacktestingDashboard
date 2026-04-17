using CryptoBacktestingDashboard.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace CryptoBacktestingDashboard.Controllers
{
    public class RiskManagementController : Controller
    {
        private readonly RiskManagementMockRepository _riskManagementRepository;

        public RiskManagementController(RiskManagementMockRepository riskManagementRepository)
        {
            _riskManagementRepository = riskManagementRepository;
        }

        public IActionResult Index()
        {
            var riskManagements = _riskManagementRepository.GetAll();
            return View(riskManagements);
        }

        public IActionResult Details(int id)
        {
            var riskManagement = _riskManagementRepository.GetById(id);
            if (riskManagement == null)
                return NotFound();

            return View(riskManagement);
        }
    }
}
