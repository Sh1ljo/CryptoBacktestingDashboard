using CryptoBacktestingDashboard.Data;
using CryptoBacktestingDashboard.Models.Crypto;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Repositories.EF
{
    public class OptimizationRunRepository
    {
        private readonly ApplicationDbContext _context;

        public OptimizationRunRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<OptimizationRun>> GetBySessionIdAsync(int sessionId)
        {
            return await _context.OptimizationRuns
                .Where(r => r.BacktestSessionId == sessionId)
                .OrderByDescending(r => r.RanAt)
                .ToListAsync();
        }

        public async Task<OptimizationRun?> GetItemAsync(int id)
        {
            return await _context.OptimizationRuns
                .Include(r => r.Results)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<OptimizationRun> InsertItemAsync(OptimizationRun item)
        {
            _context.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<OptimizationRun> UpdateItemAsync(OptimizationRun item)
        {
            _context.Update(item);
            await _context.SaveChangesAsync();
            return item;
        }
    }
}
