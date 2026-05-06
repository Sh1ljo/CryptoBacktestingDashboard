using CryptoBacktestingDashboard.Data;
using CryptoBacktestingDashboard.Models.Crypto;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CryptoBacktestingDashboard.Repositories.EF
{
    public class RiskManagementRepository
    {
        private readonly ApplicationDbContext _context;

        public RiskManagementRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<RiskManagement>> GetItemsAsync()
        {
            return await _context.RiskManagements.ToListAsync();
        }

        public async Task<RiskManagement?> GetItemAsync(int id)
        {
            return await _context.RiskManagements
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<RiskManagement> InsertItemAsync(RiskManagement item)
        {
            _context.Add(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<RiskManagement> UpdateItemAsync(RiskManagement item)
        {
            _context.Update(item);
            await _context.SaveChangesAsync();
            return item;
        }

        public async Task<bool> DeleteItemAsync(int id)
        {
            var item = await _context.RiskManagements.FindAsync(id);
            if (item == null)
            {
                return false;
            }

            _context.RiskManagements.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
