using BSMS.BusinessObjects.DTOs;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;

namespace BSMS.BLL.Services;

public interface IBatteryService
{
    Task<IEnumerable<Battery>> GetAvailableBatteriesAsync(int stationId);
    Task<IEnumerable<Battery>> GetAvailableBatteriesByModelAsync(int stationId, string model);
    Task<IEnumerable<BatteryModelGroupDto>> GetBatteriesGroupedByModelAsync(int stationId);
    Task<Battery?> GetBatteryDetailsAsync(int batteryId);
    Task UpdateBatteryStatusAsync(int batteryId, BatteryStatus newStatus);
    Task<bool> CheckCompatibilityAsync(string batteryModel, string vehicleModel);
}
