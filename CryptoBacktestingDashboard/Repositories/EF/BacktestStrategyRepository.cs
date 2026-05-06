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

        public async Task<List<BacktestStrategy>> GetItemsAsync()
        {
            return await _context.BacktestStrategies
                .Include(s => s.RiskManagement)
                .Include(s => s.Indicators)
                .ToListAsync();
        }

        public async Task<BacktestStrategy?> GetItemAsync(int id)
        {
            return await _context.BacktestStrategies
                .Include(s => s.RiskManagement)
                .Include(s => s.Indicators)
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
