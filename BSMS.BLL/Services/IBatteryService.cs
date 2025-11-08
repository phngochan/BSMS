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
}
