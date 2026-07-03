using CryptoBacktestingDashboard.Data;
using CryptoBacktestingDashboard.Models.Crypto;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Repositories.EF
{
    public class OptimizationResultRepository
    {
        private readonly ApplicationDbContext _context;

        public OptimizationResultRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<OptimizationResult>> GetByRunIdAsync(int runId)
        {
            return await _context.OptimizationResults
                .Where(r => r.OptimizationRunId == runId)
                .OrderByDescending(r => r.CompositeScore)
                .ToListAsync();
        }

        public async Task<OptimizationResult> InsertItemAsync(OptimizationResult item)
        {
            _context.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        // Bulk-inserts in a single SaveChanges so a run that only persists its
        // winning combos doesn't pay a round-trip per row.
        public async Task<List<OptimizationResult>> InsertRangeAsync(List<OptimizationResult> items)
        {
            _context.AddRange(items);
            await _context.SaveChangesAsync();
            return items;
        }
    }
}
