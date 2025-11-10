using BSMS.BusinessObjects.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSMS.BLL.Services
{
    public interface IPackageService
    {
        Task<List<BatteryServicePackage>> GetAllAsync();
        Task<BatteryServicePackage?> GetByIdAsync(int id);
        Task CreateAsync(BatteryServicePackage package);
        Task UpdateAsync(BatteryServicePackage package);
        Task DeleteAsync(int id);
    }
}
