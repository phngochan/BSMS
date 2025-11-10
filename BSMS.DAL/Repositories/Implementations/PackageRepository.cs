using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;
using BSMS.DAL.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSMS.DAL.Repositories.Implementations
{
    public class PackageRepository : GenericRepository<BatteryServicePackage>, IPackageRepository
    {

        public PackageRepository(BSMSDbContext context) : base(context)
        {
        }

        public async Task<List<BatteryServicePackage>> GetAllAsync()
        {
            return await _context.BatteryServicePackages.ToListAsync();
        }

        public async Task<BatteryServicePackage?> GetByIdAsync(int id)
        {
            return await _context.BatteryServicePackages.FindAsync(id);
        }

        public async Task AddAsync(BatteryServicePackage package)
        {
            _context.BatteryServicePackages.Add(package);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(BatteryServicePackage package)
        {
            _context.BatteryServicePackages.Update(package);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var package = await GetByIdAsync(id);
            if (package != null)
            {
                _context.BatteryServicePackages.Remove(package);
                await _context.SaveChangesAsync();
            }
        }
    }
}
