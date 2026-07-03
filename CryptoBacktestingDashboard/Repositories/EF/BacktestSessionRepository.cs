using CryptoBacktestingDashboard.Data;
using CryptoBacktestingDashboard.Models.Crypto;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Repositories.EF
{
    public class BacktestSessionRepository
    {
        private readonly ApplicationDbContext _context;

        public BacktestSessionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // For web controllers — filter by user
        public async Task<List<BacktestSession>> GetItemsAsync(string userId)
        {
            return await _context.BacktestSessions
                .Include(s => s.Strategy)
                .Include(s => s.CryptoPair)
                .Include(s => s.Results)
                .Where(s => s.AppUserId == userId)
                .ToListAsync();
        }

        // For background services (optimization, etc.) — no user filter
        public async Task<List<BacktestSession>> GetItemsAsync()
        {
            return await _context.BacktestSessions
                .Include(s => s.Strategy)
                .Include(s => s.CryptoPair)
                .Include(s => s.Results)
                .ToListAsync();
        }

        // For web controllers — filter by user
        public async Task<BacktestSession?> GetItemAsync(int id, string userId)
        {
            return await _context.BacktestSessions
                .Include(s => s.Strategy)
                .Include(s => s.CryptoPair)
                .Include(s => s.Results)
                .FirstOrDefaultAsync(m => m.Id == id && m.AppUserId == userId);
        }

        // For background services — no user filter
        public async Task<BacktestSession?> GetItemAsync(int id)
        {
            return await _context.BacktestSessions
                .Include(s => s.Strategy)
                .Include(s => s.CryptoPair)
                .Include(s => s.Results)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<BacktestSession> InsertItemAsync(BacktestSession item)
        {
            _context.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        // For web controllers — filter by user
        public async Task<bool> DeleteItemAsync(int id, string userId)
        {
            var item = await _context.BacktestSessions
                .Include(s => s.Results)
                .FirstOrDefaultAsync(m => m.Id == id && m.AppUserId == userId);
            if (item == null)
            {
                return false;
            }

            // Delete related results first to avoid FK violation
            if (item.Results.Count > 0)
            {
                _context.BacktestResults.RemoveRange(item.Results);
            }

            _context.BacktestSessions.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        // For background services — no user filter
        public async Task<bool> DeleteItemAsync(int id)
        {
            var item = await _context.BacktestSessions
                .Include(s => s.Results)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (item == null)
            {
                return false;
            }

            // Delete related results first to avoid FK violation
            if (item.Results.Count > 0)
            {
                _context.BacktestResults.RemoveRange(item.Results);
            }

            _context.BacktestSessions.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<BacktestSession> UpdateItemAsync(BacktestSession item)
        {
            _context.Update(item);
            await _context.SaveChangesAsync();
            return item;
        }

    }
}
