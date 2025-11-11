using BSMS.BusinessObjects.Models;

namespace BSMS.BLL.Services;

public interface IVehicleService
{
    Task<IEnumerable<Vehicle>> GetUserVehiclesAsync(int userId);
    Task<Vehicle?> GetVehicleByIdAsync(int vehicleId, int userId);
    Task<Vehicle> CreateVehicleAsync(int userId, string vin, string batteryModel, string batteryType);
    Task<Vehicle> UpdateVehicleAsync(int vehicleId, int userId, string vin, string batteryModel, string batteryType);
    Task<bool> DeleteVehicleAsync(int vehicleId, int userId);
    Task<bool> IsVinExistsAsync(string vin, int? excludeVehicleId = null);
}

