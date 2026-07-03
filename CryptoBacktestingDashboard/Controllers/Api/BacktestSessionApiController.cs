using CryptoBacktestingDashboard.Models;
using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Models.DTO;
using CryptoBacktestingDashboard.Repositories.EF;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CryptoBacktestingDashboard.Controllers.Api
{
    [Route("api/sessions")]
    [ApiController]
    [Authorize]
    public class BacktestSessionApiController : ControllerBase
    {
        private readonly BacktestSessionRepository _repository;
        private readonly UserManager<AppUser> _userManager;

        public BacktestSessionApiController(BacktestSessionRepository repository, UserManager<AppUser> userManager)
        {
            _repository = repository;
            _userManager = userManager;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BacktestSessionDTO>>> Get(string? q = null)
        {
            var items = await _repository.GetItemsAsync();
            if (!string.IsNullOrEmpty(q))
            {
                items = items.Where(s =>
                    (s.CryptoPair != null && s.CryptoPair.Symbol != null && s.CryptoPair.Symbol.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (s.Strategy != null && s.Strategy.Name != null && s.Strategy.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            return Ok(items.Select(ToDTO));
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<BacktestSessionDTO>> Get(int id)
        {
            var item = await _repository.GetItemAsync(id);
            if (item == null) return NotFound();

            return Ok(ToDTO(item));
        }

        [Authorize(Roles = "Admin,User")]
        [HttpPost]
        public async Task<ActionResult<BacktestSessionDTO>> Post([FromBody] BacktestSessionDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var item = new BacktestSession
            {
                StrategyId = dto.StrategyId,
                CryptoPairId = dto.CryptoPairId,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                InitialBalance = dto.InitialBalance,
                FinalBalance = 0,
                IsOptimized = dto.IsOptimized,
                ExecutedAt = DateTime.Now
            };

            item.AppUserId = _userManager.GetUserId(User);
            await _repository.InsertItemAsync(item);

            var userId = _userManager.GetUserId(User);
            var created = await _repository.GetItemAsync(item.Id, userId);
            return CreatedAtAction(nameof(Get), new { id = item.Id }, ToDTO(created!));
        }

        [Authorize(Roles = "Admin,User")]
        [HttpPut("{id}")]
        public async Task<ActionResult<BacktestSessionDTO>> Put(int id, [FromBody] BacktestSessionDTO dto)
        {
            if (id != dto.Id || !ModelState.IsValid) return BadRequest();

            var item = await _repository.GetItemAsync(id);
            if (item == null) return NotFound();

            item.StrategyId = dto.StrategyId;
            item.CryptoPairId = dto.CryptoPairId;
            item.StartDate = dto.StartDate;
            item.EndDate = dto.EndDate;
            item.InitialBalance = dto.InitialBalance;
            item.IsOptimized = dto.IsOptimized;

            await _repository.UpdateItemAsync(item);

            var updated = await _repository.GetItemAsync(id);
            return Ok(ToDTO(updated!));
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var item = await _repository.GetItemAsync(id);
            if (item == null) return NotFound();

            await _repository.DeleteItemAsync(id);
            return Ok();
        }

        private static BacktestSessionDTO ToDTO(BacktestSession s) => new BacktestSessionDTO
        {
            Id = s.Id,
            StrategyId = s.StrategyId,
            CryptoPairId = s.CryptoPairId,
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            ExecutedAt = s.ExecutedAt,
            InitialBalance = s.InitialBalance,
            FinalBalance = s.FinalBalance,
            IsOptimized = s.IsOptimized,
            Profit = s.GetProfit(),
            ROI = s.GetROI(),
            Strategy = s.Strategy == null ? null : new BacktestStrategyDTO
            {
                Id = s.Strategy.Id,
                Name = s.Strategy.Name,
                Description = s.Strategy.Description,
                IsActive = s.Strategy.IsActive,
                StopLossPercent = s.Strategy.StopLossPercent,
                TakeProfitPercent = s.Strategy.TakeProfitPercent
            },
            CryptoPair = s.CryptoPair == null ? null : new CryptoPairDTO
            {
                Id = s.CryptoPair.Id,
                Symbol = s.CryptoPair.Symbol,
                BaseAsset = s.CryptoPair.BaseAsset,
                QuoteAsset = s.CryptoPair.QuoteAsset,
                CurrentPrice = s.CryptoPair.CurrentPrice
            }
        };
    }
}
