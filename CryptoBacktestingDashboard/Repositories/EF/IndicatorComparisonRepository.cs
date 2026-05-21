using CryptoBacktestingDashboard.Data;
using CryptoBacktestingDashboard.Models.Crypto;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Repositories.EF
{
    public class IndicatorComparisonRepository
    {
        private readonly ApplicationDbContext _context;

        public IndicatorComparisonRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<IndicatorComparison>> GetItemsAsync()
        {
            return await _context.IndicatorComparisons
                .Include(c => c.IndicatorA)
                .Include(c => c.IndicatorB)
                .ToListAsync();
        }

        public async Task<IndicatorComparison?> GetItemAsync(int id)
        {
            return await _context.IndicatorComparisons
                .Include(c => c.IndicatorA)
                .Include(c => c.IndicatorB)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<List<IndicatorComparison>> GetByStrategyIdAsync(int strategyId)
        {
            return await _context.IndicatorComparisons
                .Where(c => c.BacktestStrategyId == strategyId)
                .Include(c => c.IndicatorA)
                .Include(c => c.IndicatorB)
                .ToListAsync();
        }

        public async Task<IndicatorComparison> InsertItemAsync(IndicatorComparison item)
        {
            _context.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<IndicatorComparison> UpdateItemAsync(IndicatorComparison item)
        {
            _context.Update(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<bool> DeleteItemAsync(int id)
        {
            var item = await _context.IndicatorComparisons.FindAsync(id);
            if (item == null)
            {
                return false;
            }

            _context.IndicatorComparisons.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task DeleteByStrategyIdAsync(int strategyId)
        {
            var comparisons = await _context.IndicatorComparisons
                .Where(c => c.BacktestStrategyId == strategyId)
                .ToListAsync();

            _context.IndicatorComparisons.RemoveRange(comparisons);
            await _context.SaveChangesAsync();
        }
    }
}
