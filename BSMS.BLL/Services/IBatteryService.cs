using BSMS.BusinessObjects.DTOs;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;

namespace BSMS.BLL.Services;

public interface IBatteryService
{
    Task<IEnumerable<Battery>> GetBatteriesAsync();
    Task<IEnumerable<Battery>> GetBatteriesByStationAsync(int stationId);
    Task<IEnumerable<Battery>> GetBatteriesByStatusAsync(BatteryStatus status);
    Task<Dictionary<BatteryStatus, int>> GetStatusSummaryAsync(int? stationId = null);
    Task<Battery?> GetBatteryAsync(int batteryId);
    Task<Battery> CreateBatteryAsync(Battery battery);
    Task UpdateBatteryAsync(Battery battery);
    Task DeleteBatteryAsync(int batteryId);

    Task<IEnumerable<Battery>> GetAvailableBatteriesAsync(int stationId);
    Task<IEnumerable<Battery>> GetAvailableBatteriesByModelAsync(int stationId, string model);
    Task<IEnumerable<BatteryModelGroupDto>> GetBatteriesGroupedByModelAsync(int stationId);
    Task<Battery?> GetBatteryDetailsAsync(int batteryId);
    Task UpdateBatteryStatusAsync(int batteryId, BatteryStatus newStatus);
    Task<bool> CheckCompatibilityAsync(string batteryModel, string vehicleModel);
}
