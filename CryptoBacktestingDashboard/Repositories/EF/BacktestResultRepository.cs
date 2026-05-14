using CryptoBacktestingDashboard.Data;
using CryptoBacktestingDashboard.Models.Crypto;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Repositories.EF
{
    public class BacktestResultRepository
    {
        private readonly ApplicationDbContext _context;

        public BacktestResultRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<BacktestResult>> GetBySessionIdAsync(int sessionId)
        {
            return await _context.BacktestResults
                .Where(r => r.BacktestSessionId == sessionId)
                .OrderBy(r => r.EntryTime)
                .ToListAsync();
        }

        public async Task<BacktestResult> InsertItemAsync(BacktestResult item)
        {
            _context.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task InsertRangeAsync(List<BacktestResult> items)
        {
            _context.AddRange(items);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteBySessionIdAsync(int sessionId)
        {
            var results = await _context.BacktestResults
                .Where(r => r.BacktestSessionId == sessionId)
                .ToListAsync();

            _context.BacktestResults.RemoveRange(results);
            await _context.SaveChangesAsync();
        }
    }
}