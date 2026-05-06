using CryptoBacktestingDashboard.Data;
using CryptoBacktestingDashboard.Models.Crypto;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Repositories.EF
{
    public class CryptoPairRepository
    {
        private readonly ApplicationDbContext _context;

        public CryptoPairRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<CryptoPair>> GetItemsAsync()
        {
            return await _context.CryptoPairs.ToListAsync();
        }

        public async Task<CryptoPair?> GetItemAsync(int id)
        {
            return await _context.CryptoPairs
                .Include(c => c.CandleDataHistory)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<CryptoPair> InsertItemAsync(CryptoPair item)
        {
            _context.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<CryptoPair> UpdateItemAsync(CryptoPair item)
        {
            _context.Update(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<bool> DeleteItemAsync(int id)
        {
            var item = await _context.CryptoPairs.FindAsync(id);
            if (item == null)
            {
                return false;
            }

            _context.CryptoPairs.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
