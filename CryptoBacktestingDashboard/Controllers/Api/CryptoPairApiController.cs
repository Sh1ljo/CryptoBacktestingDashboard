using CryptoBacktestingDashboard.Models;
using CryptoBacktestingDashboard.Models.Crypto;
using CryptoBacktestingDashboard.Models.DTO;
using CryptoBacktestingDashboard.Repositories.EF;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CryptoBacktestingDashboard.Controllers.Api
{
    [Route("api/pairs")]
    [ApiController]
    [Authorize]
    public class CryptoPairApiController : ControllerBase
    {
        private readonly CryptoPairRepository _repository;

        public CryptoPairApiController(CryptoPairRepository repository)
        {
            _repository = repository;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CryptoPairDTO>>> Get(string? q = null)
        {
            var pairs = await _repository.GetItemsAsync();
            if (!string.IsNullOrEmpty(q))
            {
                pairs = pairs.Where(p => p.Symbol != null && p.Symbol.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            return Ok(pairs.Select(p => new CryptoPairDTO
            {
                Id = p.Id,
                Symbol = p.Symbol,
                BaseAsset = p.BaseAsset,
                QuoteAsset = p.QuoteAsset,
                CurrentPrice = p.CurrentPrice
            }));
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<CryptoPairDTO>> Get(int id)
        {
            var pair = await _repository.GetItemAsync(id);
            if (pair == null) return NotFound();

            return Ok(new CryptoPairDTO
            {
                Id = pair.Id,
                Symbol = pair.Symbol,
                BaseAsset = pair.BaseAsset,
                QuoteAsset = pair.QuoteAsset,
                CurrentPrice = pair.CurrentPrice
            });
        }

        [Authorize(Roles = "Admin,User")]
        [HttpPost]
        public async Task<ActionResult<CryptoPairDTO>> Post([FromBody] CryptoPairDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var pair = new CryptoPair
            {
                Symbol = dto.Symbol,
                BaseAsset = dto.BaseAsset,
                QuoteAsset = dto.QuoteAsset,
                CurrentPrice = dto.CurrentPrice,
                CreatedAt = DateTime.Now
            };

            await _repository.InsertItemAsync(pair);
            dto.Id = pair.Id;

            return CreatedAtAction(nameof(Get), new { id = dto.Id }, dto);
        }

        [Authorize(Roles = "Admin,User")]
        [HttpPut("{id}")]
        public async Task<ActionResult<CryptoPairDTO>> Put(int id, [FromBody] CryptoPairDTO dto)
        {
            if (id != dto.Id || !ModelState.IsValid) return BadRequest();

            var pair = await _repository.GetItemAsync(id);
            if (pair == null) return NotFound();

            pair.Symbol = dto.Symbol;
            pair.BaseAsset = dto.BaseAsset;
            pair.QuoteAsset = dto.QuoteAsset;
            pair.CurrentPrice = dto.CurrentPrice;

            await _repository.UpdateItemAsync(pair);

            return Ok(dto);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var pair = await _repository.GetItemAsync(id);
            if (pair == null) return NotFound();

            await _repository.DeleteItemAsync(id);
            return Ok();
        }
    }
}