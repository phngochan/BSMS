using BSMS.BusinessObjects.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSMS.BLL.Services
{
    public interface IUserPackageService
    {
        Task<List<BatteryServicePackage>> GetAvailablePackagesAsync();

        Task<List<BatteryServicePackage>> GetAllPackagesAsync();

        Task<UserPackage?> GetActivePackageAsync(int userId);
        Task CreateUserPackageAsync(int userId, int packageId, int paymentId);
        Task<bool> CanSwapBatteryAsync(int userId, int vehicleId, int stationId, int batteryTakenId, int batteryReturnedId);
        Task<UserPackage?> GetCurrentPackageAsync(int userId);
        Task ExpirePackagesAsync();

        Task<BatteryServicePackage?> GetByIdAsync(int id);
        Task CreateAsync(BatteryServicePackage pkg);
        Task UpdateAsync(BatteryServicePackage pkg);
        Task<bool> DeleteAsync(int id);
        Task<List<UserPackage>> GetUserPackagesAsync(int userId);
    }
}
