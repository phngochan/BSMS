using BSMS.BusinessObjects.Enums;
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
    public class UserPackageRepository : GenericRepository<UserPackage>, IUserPackageRepository
    {

        public UserPackageRepository(BSMSDbContext context) : base(context)
        {
        }

        public async Task<List<UserPackage>> GetExpiringSoonAsync(int daysBefore)
        {
            var now = DateTime.Now;
            var targetDate = now.AddDays(daysBefore);

            return await _context.UserPackages
                .Include(up => up.Package)
                .Include(up => up.User)
                .Where(up => up.Status == PackageStatus.Active &&
                             up.EndDate <= targetDate &&
                             up.EndDate > now)
                .ToListAsync();
        }

        public async Task<List<UserPackage>> GetExpiredAsync()
        {
            var now = DateTime.Now; 
            return await _context.UserPackages
                .Where(up => up.Status == PackageStatus.Active &&
                             up.EndDate < now)
                .ToListAsync();
        }

        public async Task UpdateAsync(UserPackage package)
        {
            _context.UserPackages.Update(package);
            await _context.SaveChangesAsync();
        }
    }
}