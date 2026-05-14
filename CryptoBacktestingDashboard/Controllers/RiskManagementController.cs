using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Repositories.EF;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
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
        public async Task<IActionResult> Index(string? q = null)
        {
            var riskManagements = await _riskManagementRepository.GetItemsAsync();

            if (!string.IsNullOrEmpty(q))
            {
                riskManagements = riskManagements.Where(r =>
                    (r.Name?.Contains(q, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.Description?.Contains(q, System.StringComparison.OrdinalIgnoreCase) ?? false)
                ).ToList();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_RiskManagementListPartial", riskManagements);
            }

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

        [HttpGet("create")]
        public IActionResult Create()
        {
            return View(new RiskManagement { CreatedAt = System.DateTime.Now });
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(RiskManagement model)
        {
            ModelState.Remove("Strategies");

            if (ModelState.IsValid)
            {
                await _riskManagementRepository.InsertItemAsync(model);
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpGet("edit/{id}")]
        [ActionName("Edit")]
        public async Task<IActionResult> EditGet(int id)
        {
            var riskManagement = await _riskManagementRepository.GetItemAsync(id);
            if (riskManagement == null)
                return NotFound();
            return View(riskManagement);
        }

        [HttpPost("edit/{id}")]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int id)
        {
            var riskManagement = await _riskManagementRepository.GetItemAsync(id);
            if (riskManagement == null)
                return NotFound();

            var ok = await TryUpdateModelAsync(riskManagement);

            ModelState.Remove("Strategies");

            if (ok && ModelState.IsValid)
            {
                await _riskManagementRepository.UpdateItemAsync(riskManagement);
                return RedirectToAction(nameof(Index));
            }

            return View(riskManagement);
        }

        [HttpPost("delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _riskManagementRepository.DeleteItemAsync(id);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok();
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
