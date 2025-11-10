using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;

namespace BSMS.BLL.Services;

public interface IBatteryService
{
    /// <summary>
    /// Get available batteries at a station
    /// </summary>
    Task<IEnumerable<Battery>> GetAvailableBatteriesAsync(int stationId);

    /// <summary>
    /// Get available batteries by model
    /// </summary>
    Task<IEnumerable<Battery>> GetAvailableBatteriesByModelAsync(int stationId, string model);

    /// <summary>
    /// Get batteries grouped by model
    /// </summary>
    Task<IEnumerable<BatteryModelGroupDto>> GetBatteriesGroupedByModelAsync(int stationId);

    /// <summary>
    /// Get battery details
    /// </summary>
    Task<Battery?> GetBatteryDetailsAsync(int batteryId);

    /// <summary>
    /// Update battery status (for staff)
    /// </summary>
    Task UpdateBatteryStatusAsync(int batteryId, BatteryStatus newStatus);

    /// <summary>
    /// Check battery compatibility with vehicle
    /// </summary>
    Task<bool> CheckCompatibilityAsync(string batteryModel, string vehicleModel);
}
