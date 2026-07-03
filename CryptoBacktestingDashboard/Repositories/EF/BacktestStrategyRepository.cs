using CryptoBacktestingDashboard.Data;
using CryptoBacktestingDashboard.Models.Crypto;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Repositories.EF
{
    public class BacktestStrategyRepository
    {
        private readonly ApplicationDbContext _context;

        public BacktestStrategyRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // For web controllers — filter by user
        public async Task<List<BacktestStrategy>> GetItemsAsync(string userId)
        {
            return await _context.BacktestStrategies
                .Include(s => s.Indicators)
                .Include(s => s.Comparisons)
                    .ThenInclude(c => c.IndicatorA)
                .Include(s => s.Comparisons)
                    .ThenInclude(c => c.IndicatorB)
                .Where(s => s.AppUserId == userId)
                .ToListAsync();
        }

        // For background services — no user filter
        public async Task<List<BacktestStrategy>> GetItemsAsync()
        {
            return await _context.BacktestStrategies
                .Include(s => s.Indicators)
                .Include(s => s.Comparisons)
                    .ThenInclude(c => c.IndicatorA)
                .Include(s => s.Comparisons)
                    .ThenInclude(c => c.IndicatorB)
                .ToListAsync();
        }

        // For web controllers — filter by user
        public async Task<BacktestStrategy?> GetItemAsync(int id, string userId)
        {
            return await _context.BacktestStrategies
                .Include(s => s.Indicators)
                .Include(s => s.Comparisons)
                    .ThenInclude(c => c.IndicatorA)
                .Include(s => s.Comparisons)
                    .ThenInclude(c => c.IndicatorB)
                .Include(s => s.BacktestSessions)
                .FirstOrDefaultAsync(m => m.Id == id && m.AppUserId == userId);
        }

        // For background services — no user filter
        public async Task<BacktestStrategy?> GetItemAsync(int id)
        {
            return await _context.BacktestStrategies
                .Include(s => s.Indicators)
                .Include(s => s.Comparisons)
                    .ThenInclude(c => c.IndicatorA)
                .Include(s => s.Comparisons)
                    .ThenInclude(c => c.IndicatorB)
                .Include(s => s.BacktestSessions)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<BacktestStrategy> InsertItemAsync(BacktestStrategy item)
        {
            _context.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<BacktestStrategy> UpdateItemAsync(BacktestStrategy item)
        {
            _context.Update(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<bool> DeleteItemAsync(int id)
        {
            var item = await _context.BacktestStrategies.FindAsync(id);
            if (item == null)
            {
                return false;
            }

            _context.BacktestStrategies.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
