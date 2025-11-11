using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSMS.DAL.Repositories
{
    public interface IPackageRepository : IGenericRepository<BatteryServicePackage>
    {
        Task<List<BatteryServicePackage>> GetActivePackagesAsync();

        Task<List<BatteryServicePackage>> GetAllPackagesAsync();

        Task<bool> CanDeleteAsync(int packageId);

        Task UpdateAsync(BatteryServicePackage pkg);

        Task<BatteryServicePackage?> GetByIdAsync(int id);
    }
}
