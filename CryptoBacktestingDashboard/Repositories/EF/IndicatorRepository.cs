using CryptoBacktestingDashboard.Data;
using CryptoBacktestingDashboard.Models.Crypto;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Repositories.EF
{
    public class IndicatorRepository
    {
        private readonly ApplicationDbContext _context;

        public IndicatorRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Indicator>> GetItemsAsync()
        {
            return await _context.Indicators.ToListAsync();
        }

        public async Task<Indicator?> GetItemAsync(int id)
        {
            return await _context.Indicators
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<Indicator> InsertItemAsync(Indicator item)
        {
            _context.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<Indicator> UpdateItemAsync(Indicator item)
        {
            _context.Update(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<bool> DeleteItemAsync(int id)
        {
            var item = await _context.Indicators.FindAsync(id);
            if (item == null)
            {
                return false;
            }

            _context.Indicators.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
