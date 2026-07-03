using CryptoBacktestingDashboard.Models;
using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Models.DTO;
using CryptoBacktestingDashboard.Repositories.EF;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CryptoBacktestingDashboard.Controllers.Api
{
    [Route("api/strategies")]
    [ApiController]
    [Authorize]
    public class BacktestStrategyApiController : ControllerBase
    {
        private readonly BacktestStrategyRepository _repository;
        private readonly UserManager<AppUser> _userManager;

        public BacktestStrategyApiController(BacktestStrategyRepository repository, UserManager<AppUser> userManager)
        {
            _repository = repository;
            _userManager = userManager;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BacktestStrategyDTO>>> Get(string? q = null)
        {
            var items = await _repository.GetItemsAsync();
            if (!string.IsNullOrEmpty(q))
            {
                items = items.Where(i => i.Name != null && i.Name.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return Ok(items.Select(i => new BacktestStrategyDTO
            {
                Id = i.Id,
                Name = i.Name,
                Description = i.Description,
                IsActive = i.IsActive,
                StopLossPercent = i.StopLossPercent,
                TakeProfitPercent = i.TakeProfitPercent
            }));
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<BacktestStrategyDTO>> Get(int id)
        {
            var item = await _repository.GetItemAsync(id);
            if (item == null) return NotFound();

            return Ok(new BacktestStrategyDTO
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                IsActive = item.IsActive,
                StopLossPercent = item.StopLossPercent,
                TakeProfitPercent = item.TakeProfitPercent
            });
        }

        [Authorize(Roles = "Admin,User")]
        [HttpPost]
        public async Task<ActionResult<BacktestStrategyDTO>> Post([FromBody] BacktestStrategyDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var item = new BacktestStrategy
            {
                AppUserId = _userManager.GetUserId(User),
                Name = dto.Name,
                Description = dto.Description,
                IsActive = dto.IsActive,
                StopLossPercent = dto.StopLossPercent,
                TakeProfitPercent = dto.TakeProfitPercent,
                InitialCapital = 10000,
                PositionSizePercent = 100,
                CreatedAt = DateTime.Now
            };

            await _repository.InsertItemAsync(item);
            dto.Id = item.Id;

            return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
        }

        [Authorize(Roles = "Admin,User")]
        [HttpPut("{id}")]
        public async Task<ActionResult<BacktestStrategyDTO>> Put(int id, [FromBody] BacktestStrategyDTO dto)
        {
            if (id != dto.Id || !ModelState.IsValid) return BadRequest();

            var item = await _repository.GetItemAsync(id);
            if (item == null) return NotFound();

            item.Name = dto.Name;
            item.Description = dto.Description;
            item.IsActive = dto.IsActive;
            item.StopLossPercent = dto.StopLossPercent;
            item.TakeProfitPercent = dto.TakeProfitPercent;

            await _repository.UpdateItemAsync(item);

            return Ok(dto);
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
    }
}