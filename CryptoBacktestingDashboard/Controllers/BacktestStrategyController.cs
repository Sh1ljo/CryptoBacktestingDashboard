using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Repositories.EF;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using CryptoBacktestingDashboard.Data;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Controllers
{
    [Route("strategies")]
    public class BacktestStrategyController : Controller
    {
        private readonly BacktestStrategyRepository _strategyRepository;
        private readonly IndicatorRepository _indicatorRepository;
        private readonly IndicatorComparisonRepository _comparisonRepository;

        public BacktestStrategyController(BacktestStrategyRepository strategyRepository, IndicatorRepository indicatorRepository, IndicatorComparisonRepository comparisonRepository)
        {
            _strategyRepository = strategyRepository;
            _indicatorRepository = indicatorRepository;
            _comparisonRepository = comparisonRepository;
        }

        // Built-in risk fields replaced the old separate RiskManagement rule.
        // Stop Loss and Take Profit are required; trailing stop and position size are optional.
        private void ValidateStrategyRisk(BacktestStrategy model)
        {
            if (model.StopLossPercent <= 0 || model.StopLossPercent >= 100)
                ModelState.AddModelError(nameof(BacktestStrategy.StopLossPercent),
                    "Stop Loss must be between 0.1% and 99%.");
            if (model.TakeProfitPercent <= 0)
                ModelState.AddModelError(nameof(BacktestStrategy.TakeProfitPercent),
                    "Take Profit must be greater than 0%.");
            if (model.TrailingStopPercent.HasValue &&
                (model.TrailingStopPercent.Value <= 0 || model.TrailingStopPercent.Value >= 100))
                ModelState.AddModelError(nameof(BacktestStrategy.TrailingStopPercent),
                    "Trailing Stop must be between 0.1% and 99% (or left blank).");
            if (model.PositionSizePercent.HasValue &&
                (model.PositionSizePercent.Value <= 0 || model.PositionSizePercent.Value > 100))
                ModelState.AddModelError(nameof(BacktestStrategy.PositionSizePercent),
                    "Position Size must be between 1% and 100% (or left blank for 100%).");
        }

        [HttpGet("")]
        public async Task<IActionResult> Index(string? q = null, int page = 1, int pageSize = 8)
        {
            var strategies = await _strategyRepository.GetItemsAsync();

            if (!string.IsNullOrEmpty(q))
            {
                strategies = strategies.Where(s =>
                    (s.Name?.Contains(q, System.StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.Description?.Contains(q, System.StringComparison.OrdinalIgnoreCase) ?? false)
                ).ToList();
            }

            var totalCount = strategies.Count;
            var totalPages = (int)System.Math.Ceiling(totalCount / (double)pageSize);
            page = System.Math.Max(1, System.Math.Min(page, System.Math.Max(1, totalPages)));
            strategies = strategies.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            ViewData["CurrentPage"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["TotalCount"] = totalCount;
            ViewData["Query"] = q;

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return PartialView("_StrategyListPartial", strategies);
            }

            return View(strategies);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var strategy = await _strategyRepository.GetItemAsync(id);
            if (strategy == null)
                return NotFound();

            return View(strategy);
        }

        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            ViewData["AllIndicators"] = await _indicatorRepository.GetItemsAsync();
            ViewData["SelectedIndicatorIds"] = new int[0];
            var model = new BacktestStrategy { CreatedAt = System.DateTime.Now, IsActive = true };
            return View(model);
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BacktestStrategy model, int[] IndicatorIds)
        {
            ViewData["AllIndicators"] = await _indicatorRepository.GetItemsAsync();
            ViewData["SelectedIndicatorIds"] = IndicatorIds ?? new int[0];

            ModelState.Remove("Indicators");
            ModelState.Remove("BacktestSessions");
            ModelState.Remove("Comparisons");
            ModelState.Remove(nameof(BacktestStrategy.StopLossPercent));
            ModelState.Remove(nameof(BacktestStrategy.TakeProfitPercent));
            ModelState.Remove(nameof(BacktestStrategy.TrailingStopPercent));
            ModelState.Remove(nameof(BacktestStrategy.PositionSizePercent));
            ValidateStrategyRisk(model);

            if (ModelState.IsValid)
            {
                if (IndicatorIds != null)
                {
                    model.Indicators = new List<Indicator>();
                    foreach (var indicatorId in IndicatorIds)
                    {
                        var indicator = await _indicatorRepository.GetItemAsync(indicatorId);
                        if (indicator != null)
                            model.Indicators.Add(indicator);
                    }
                }
                await _strategyRepository.InsertItemAsync(model);
                return RedirectToAction(nameof(Index));
            }
            return View(model);
        }

        [HttpGet("edit/{id}")]
        [ActionName("Edit")]
        public async Task<IActionResult> EditGet(int id)
        {
            var strategy = await _strategyRepository.GetItemAsync(id);
            if (strategy == null)
                return NotFound();

            ViewData["AllIndicators"] = await _indicatorRepository.GetItemsAsync();
            ViewData["SelectedIndicatorIds"] = strategy.Indicators?.Select(i => i.Id).ToArray() ?? new int[0];
            return View(strategy);
        }

        [HttpPost("edit/{id}")]
        [ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int id, int[] IndicatorIds)
        {
            var strategy = await _strategyRepository.GetItemAsync(id);
            if (strategy == null)
                return NotFound();

            await TryUpdateModelAsync(strategy);

            ViewData["AllIndicators"] = await _indicatorRepository.GetItemsAsync();
            ViewData["SelectedIndicatorIds"] = IndicatorIds ?? new int[0];

            ModelState.Remove("Indicators");
            ModelState.Remove("BacktestSessions");
            ModelState.Remove("Comparisons");
            ModelState.Remove(nameof(BacktestStrategy.StopLossPercent));
            ModelState.Remove(nameof(BacktestStrategy.TakeProfitPercent));
            ModelState.Remove(nameof(BacktestStrategy.TrailingStopPercent));
            ModelState.Remove(nameof(BacktestStrategy.PositionSizePercent));
            ValidateStrategyRisk(strategy);

            if (ModelState.IsValid)
            {
                strategy.LastModifiedAt = System.DateTime.Now;
                if (IndicatorIds != null)
                {
                    strategy.Indicators = new List<Indicator>();
                    foreach (var indicatorId in IndicatorIds)
                    {
                        var indicator = await _indicatorRepository.GetItemAsync(indicatorId);
                        if (indicator != null)
                            strategy.Indicators.Add(indicator);
                    }
                }
                await _strategyRepository.UpdateItemAsync(strategy);
                return RedirectToAction(nameof(Index));
            }

            return View(strategy);
        }

        [HttpPost("delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            await _strategyRepository.DeleteItemAsync(id);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok();
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{strategyId}/comparisons")]
        public async Task<IActionResult> ManageComparisons(int strategyId)
        {
            var strategy = await _strategyRepository.GetItemAsync(strategyId);
            if (strategy == null)
                return NotFound();

            ViewData["StrategyId"] = strategyId;
            ViewData["StrategyName"] = strategy.Name;
            ViewData["IndicatorIds"] = new SelectList(strategy.Indicators, "Id", "Name");
            ViewData["ComparisonTypes"] = new SelectList(System.Enum.GetValues(typeof(ComparisonType)).Cast<ComparisonType>());
            ViewData["SignalTypes"] = new SelectList(System.Enum.GetValues(typeof(IndicatorSignal)).Cast<IndicatorSignal>());

            var comparisons = await _comparisonRepository.GetByStrategyIdAsync(strategyId);
            return View(comparisons);
        }

        [HttpPost("{strategyId}/comparisons")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateComparison(int strategyId, IndicatorComparison comparison)
        {
            var strategy = await _strategyRepository.GetItemAsync(strategyId);
            if (strategy == null)
                return NotFound();

            comparison.BacktestStrategyId = strategyId;
            comparison.CreatedAt = System.DateTime.Now;

            ModelState.Remove("BacktestStrategyId");
            ModelState.Remove("BacktestStrategy");
            ModelState.Remove("IndicatorA");
            ModelState.Remove("IndicatorB");

            if (ModelState.IsValid)
            {
                await _comparisonRepository.InsertItemAsync(comparison);
                return RedirectToAction("ManageComparisons", new { strategyId });
            }

            ViewData["StrategyId"] = strategyId;
            ViewData["StrategyName"] = strategy.Name;
            ViewData["IndicatorIds"] = new SelectList(strategy.Indicators, "Id", "Name", comparison.IndicatorAId);
            ViewData["ComparisonTypes"] = new SelectList(System.Enum.GetValues(typeof(ComparisonType)).Cast<ComparisonType>());
            ViewData["SignalTypes"] = new SelectList(System.Enum.GetValues(typeof(IndicatorSignal)).Cast<IndicatorSignal>());
            var comparisons = await _comparisonRepository.GetByStrategyIdAsync(strategyId);

            return View("ManageComparisons", comparisons);
        }

        [HttpPost("{strategyId}/comparisons/{comparisonId}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComparison(int strategyId, int comparisonId)
        {
            var comparison = await _comparisonRepository.GetItemAsync(comparisonId);
            if (comparison == null || comparison.BacktestStrategyId != strategyId)
                return NotFound();

            await _comparisonRepository.DeleteItemAsync(comparisonId);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Ok();
            }

            return RedirectToAction("ManageComparisons", new { strategyId });
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search(string query)
        {
            var strategies = await _strategyRepository.GetItemsAsync();
            var results = strategies
                .Where(s => string.IsNullOrEmpty(query) || (s.Name?.Contains(query, System.StringComparison.OrdinalIgnoreCase) ?? false))
                .Take(10)
                .Select(s => new { id = s.Id, text = s.Name })
                .ToList();

            return Json(results);
        }

        [HttpPost("UploadAttachment")]
        public async Task<IActionResult> UploadAttachment(int strategyId, IFormFile file, [FromServices] ApplicationDbContext dbContext)
        {
            var strategy = await _strategyRepository.GetItemAsync(strategyId);
            if (strategy == null) return NotFound();
            if (file == null || file.Length == 0) return BadRequest();

            var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "strategies", strategyId.ToString());
            Directory.CreateDirectory(uploadsPath);

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var attachment = new Attachment
            {
                StrategyId = strategyId,
                FileName = file.FileName,
                FilePath = "/uploads/strategies/" + strategyId + "/" + fileName,
                ContentType = file.ContentType,
                FileSize = file.Length,
                CreatedAt = DateTime.UtcNow
            };

            dbContext.Attachments.Add(attachment);
            await dbContext.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpGet("GetAttachments")]
        public IActionResult GetAttachments(int strategyId, [FromServices] ApplicationDbContext dbContext)
        {
            var attachments = dbContext.Attachments.Where(a => a.StrategyId == strategyId).OrderByDescending(a => a.CreatedAt).ToList();
            return PartialView("_AttachmentList", attachments);
        }

        [HttpPost("DeleteAttachment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttachment(int id, [FromServices] ApplicationDbContext dbContext)
        {
            var attachment = dbContext.Attachments.FirstOrDefault(a => a.Id == id);
            if (attachment == null) return NotFound();

            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", attachment.FilePath.TrimStart('/'));
            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }

            dbContext.Attachments.Remove(attachment);
            await dbContext.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
