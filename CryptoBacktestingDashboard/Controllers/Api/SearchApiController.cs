using CryptoBacktestingDashboard.Data;
using CryptoBacktestingDashboard.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CryptoBacktestingDashboard.Controllers.Api
{
    [Route("api/search")]
    [ApiController]
    [Authorize]
    public class SearchApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Models.AppUser> _userManager;

        public SearchApiController(ApplicationDbContext context, UserManager<Models.AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// Global search across menus, pages, and data entities.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<SearchResultDTO>> Get([FromQuery] string q = "")
        {
            var result = new SearchResultDTO();
            var query = (q ?? "").Trim();

            if (string.IsNullOrEmpty(query))
            {
                return Ok(result);
            }

            // ── 1. Static menu & page navigation results ──────────
            AddMenuResults(result, query);

            // ── 2. Data entity results ────────────────────────────
            await AddDataResults(result, query);

            result.TotalCount = result.Results.Count;
            return Ok(result);
        }

        private static void AddMenuResults(SearchResultDTO result, string query)
        {
            var lower = query.ToLowerInvariant();

            var menuItems = new List<SearchResultItem>
            {
                new() { Type = "Menu", Label = "Dashboard", Description = "Analytics dashboard overview", Url = "/", Badge = "Home" },
                new() { Type = "Menu", Label = "Backtest Sessions", Description = "All backtest sessions", Url = "/BacktestSession", Badge = "Sessions" },
                new() { Type = "Menu", Label = "Trading Strategies", Description = "All trading strategies", Url = "/BacktestStrategy", Badge = "Strategies" },
                new() { Type = "Menu", Label = "Crypto Pairs", Description = "All cryptocurrency pairs", Url = "/CryptoPair", Badge = "Pairs" },
                new() { Type = "Menu", Label = "Indicators", Description = "All technical indicators", Url = "/Indicator", Badge = "Indicators" },
            };

            var pageItems = new List<SearchResultItem>
            {
                new() { Type = "Page", Label = "New Session", Description = "Create a new backtest session", Url = "/BacktestSession/Create", Badge = "Create" },
                new() { Type = "Page", Label = "New Strategy", Description = "Create a new trading strategy", Url = "/BacktestStrategy/Create", Badge = "Create" },
                new() { Type = "Page", Label = "New Pair", Description = "Add a new crypto pair", Url = "/CryptoPair/Create", Badge = "Create" },
                new() { Type = "Page", Label = "New Indicator", Description = "Create a new indicator", Url = "/Indicator/Create", Badge = "Create" },
                new() { Type = "Page", Label = "Privacy Policy", Description = "Privacy information", Url = "/Home/Privacy", Badge = "Info" },
            };

            // Filter menus
            foreach (var item in menuItems)
            {
                if (item.Label.Contains(lower, StringComparison.OrdinalIgnoreCase) ||
                    (item.Description != null && item.Description.Contains(lower, StringComparison.OrdinalIgnoreCase)))
                {
                    result.Results.Add(item);
                }
            }

            // Filter pages
            foreach (var item in pageItems)
            {
                if (item.Label.Contains(lower, StringComparison.OrdinalIgnoreCase) ||
                    (item.Description != null && item.Description.Contains(lower, StringComparison.OrdinalIgnoreCase)))
                {
                    result.Results.Add(item);
                }
            }
        }

        private async Task AddDataResults(SearchResultDTO result, string query)
        {
            var lower = query.ToLowerInvariant();
            var userId = _userManager.GetUserId(User);

            // ── Strategies ────────────────────────────────────────
            var strategies = await _context.BacktestStrategies
                .Where(s => s.AppUserId == userId && s.Name != null && s.Name.Contains(query))
                .Take(5)
                .ToListAsync();

            foreach (var s in strategies)
            {
                result.Results.Add(new SearchResultItem
                {
                    Type = "Strategy",
                    Label = s.Name ?? "",
                    Description = s.Description,
                    Url = $"/BacktestStrategy/Details/{s.Id}",
                    Badge = s.IsActive ? "Active" : "Inactive"
                });
            }

            // ── Crypto Pairs ──────────────────────────────────────
            var pairs = await _context.CryptoPairs
                .Where(p => (p.Symbol != null && p.Symbol.Contains(query)) ||
                            (p.BaseAsset != null && p.BaseAsset.Contains(query)) ||
                            (p.QuoteAsset != null && p.QuoteAsset.Contains(query)))
                .Take(5)
                .ToListAsync();

            foreach (var p in pairs)
            {
                result.Results.Add(new SearchResultItem
                {
                    Type = "Pair",
                    Label = p.Symbol ?? "",
                    Description = $"{p.BaseAsset}/{p.QuoteAsset}",
                    Url = $"/CryptoPair/Details/{p.Id}",
                    Badge = p.CurrentPrice.HasValue ? $"${p.CurrentPrice:F2}" : null
                });
            }

            // ── Backtest Sessions (search by strategy name or pair symbol) ──
            var sessions = await _context.BacktestSessions
                .Include(s => s.Strategy)
                .Include(s => s.CryptoPair)
                .Where(s => s.AppUserId == userId &&
                            ((s.Strategy != null && s.Strategy.Name != null && s.Strategy.Name.Contains(query)) ||
                             (s.CryptoPair != null && s.CryptoPair.Symbol != null && s.CryptoPair.Symbol.Contains(query))))
                .Take(5)
                .ToListAsync();

            foreach (var s in sessions)
            {
                var profit = s.GetProfit();
                result.Results.Add(new SearchResultItem
                {
                    Type = "Session",
                    Label = $"{s.Strategy?.Name ?? "?"} on {s.CryptoPair?.Symbol ?? "?"}",
                    Description = $"{s.StartDate:yyyy-MM-dd} → {s.EndDate:yyyy-MM-dd} ({s.Results.Count} trades)",
                    Url = $"/BacktestSession/Details/{s.Id}",
                    Badge = profit >= 0 ? $"+${profit:F0}" : $"-${Math.Abs(profit):F0}"
                });
            }

            // ── Indicators ────────────────────────────────────────
            var indicators = await _context.Indicators
                .Where(i => (i.Name != null && i.Name.Contains(query)) ||
                            (i.Description != null && i.Description.Contains(query)))
                .Take(5)
                .ToListAsync();

            foreach (var i in indicators)
            {
                result.Results.Add(new SearchResultItem
                {
                    Type = "Indicator",
                    Label = i.Name ?? "",
                    Description = i.Description,
                    Url = $"/Indicator/Details/{i.Id}",
                    Badge = i.Type.ToString()
                });
            }
        }
    }
}
