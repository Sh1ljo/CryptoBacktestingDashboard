using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Models.DTO;
using CryptoBacktestingDashboard.Repositories.EF;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CryptoBacktestingDashboard.Controllers.Api
{
    [Route("api/indicators")]
    [ApiController]
    [Authorize]
    public class IndicatorApiController : ControllerBase
    {
        private readonly IndicatorRepository _repository;

        public IndicatorApiController(IndicatorRepository repository)
        {
            _repository = repository;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<IndicatorDTO>>> Get(string? q = null)
        {
            var items = await _repository.GetItemsAsync();
            if (!string.IsNullOrEmpty(q))
            {
                items = items.Where(i =>
                    (i.Name != null && i.Name.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    (i.Description != null && i.Description.Contains(q, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            return Ok(items.Select(ToDTO));
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<IndicatorDTO>> Get(int id)
        {
            var item = await _repository.GetItemAsync(id);
            if (item == null) return NotFound();

            return Ok(ToDTO(item));
        }

        [Authorize(Roles = "Admin,User")]
        [HttpPost]
        public async Task<ActionResult<IndicatorDTO>> Post([FromBody] IndicatorDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var item = new Indicator
            {
                Name = dto.Name,
                Type = dto.Type,
                Period = dto.Period,
                Threshold = dto.Threshold,
                Description = dto.Description,
                CreatedAt = DateTime.Now
            };

            await _repository.InsertItemAsync(item);
            dto.Id = item.Id;
            dto.CreatedAt = item.CreatedAt;

            return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
        }

        [Authorize(Roles = "Admin,User")]
        [HttpPut("{id}")]
        public async Task<ActionResult<IndicatorDTO>> Put(int id, [FromBody] IndicatorDTO dto)
        {
            if (id != dto.Id || !ModelState.IsValid) return BadRequest();

            var item = await _repository.GetItemAsync(id);
            if (item == null) return NotFound();

            item.Name = dto.Name;
            item.Type = dto.Type;
            item.Period = dto.Period;
            item.Threshold = dto.Threshold;
            item.Description = dto.Description;

            await _repository.UpdateItemAsync(item);

            return Ok(ToDTO(item));
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

        private static IndicatorDTO ToDTO(Indicator i) => new IndicatorDTO
        {
            Id = i.Id,
            Name = i.Name,
            Type = i.Type,
            Period = i.Period,
            Threshold = i.Threshold,
            Description = i.Description,
            CreatedAt = i.CreatedAt
        };
    }
}
