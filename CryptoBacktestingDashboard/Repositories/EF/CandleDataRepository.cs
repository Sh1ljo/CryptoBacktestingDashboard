using CryptoBacktestingDashboard.Data;
using CryptoBacktestingDashboard.Models.Crypto;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Repositories.EF
{
    public class CandleDataRepository
    {
        private readonly ApplicationDbContext _context;

        public CandleDataRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CandleData>> GetItemsAsync()
        {
            return await _context.CandleData
                .Include(c => c.CryptoPair)
                .OrderByDescending(c => c.OpenTime)
                .ToListAsync();
        }

        public async Task<CandleData?> GetItemAsync(int id)
        {
            return await _context.CandleData
                .Include(c => c.CryptoPair)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<CandleData> InsertItemAsync(CandleData item)
        {
            _context.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<CandleData> UpdateItemAsync(CandleData item)
        {
            _context.Update(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<bool> DeleteItemAsync(int id)
        {
            var item = await _context.CandleData.FindAsync(id);
            if (item == null)
            {
                return false;
            }

            _context.CandleData.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<CandleData>> GetByPairIdAndDateRangeAsync(int pairId, DateTime from, DateTime to)
        {
            return await _context.CandleData
                .Where(c => c.CryptoPairId == pairId && c.OpenTime >= from && c.OpenTime <= to)
                .OrderBy(c => c.OpenTime)
                .ToListAsync();
        }

        public async Task DeleteByPairIdAsync(int pairId)
        {
            var candles = await _context.CandleData
                .Where(c => c.CryptoPairId == pairId)
                .ToListAsync();

            _context.CandleData.RemoveRange(candles);
            await _context.SaveChangesAsync();
        }
    }
}
