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
        public PackageRepository(BSMSDbContext context) : base(context) { }

        public async Task<List<BatteryServicePackage>> GetActivePackagesAsync()
            => await _context.BatteryServicePackages
                .Where(p => p.Active)
                .OrderBy(p => p.Price)
                .ToListAsync();


        public async Task<bool> CanDeleteAsync(int packageId)
        {
            return !await _context.UserPackages.AnyAsync(up => up.PackageId == packageId);
        }

        public async Task<BatteryServicePackage?> GetByIdAsync(int id)
        {
            return await _context.BatteryServicePackages
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PackageId == id);
        }

        public async Task UpdateAsync(BatteryServicePackage pkg)
        {
            _context.BatteryServicePackages.Attach(pkg);
            _context.Entry(pkg).State = EntityState.Modified;

            await _context.SaveChangesAsync();
        }

    }
}
