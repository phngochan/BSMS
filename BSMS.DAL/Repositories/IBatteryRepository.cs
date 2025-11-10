using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;

namespace BSMS.DAL.Repositories;

public interface IBatteryRepository : IGenericRepository<Battery>
{
    Task<IEnumerable<Battery>> GetAvailableBatteriesAsync(int stationId);

    Task<IEnumerable<Battery>> GetAvailableBatteriesByModelAsync(int stationId, string model);

    Task<Battery?> GetBatteryWithStationAsync(int batteryId);

    Task<IEnumerable<BatteryModelGroupDto>> GetBatteriesGroupedByModelAsync(int stationId);

    Task UpdateBatteryStatusAsync(int batteryId, BatteryStatus newStatus);

    Task<bool> IsBatteryCompatibleAsync(string batteryModel, string vehicleModel);
}

public class BatteryModelGroupDto
{
    public string Model { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int TotalCount { get; set; }
    public int AvailableCount { get; set; }
    public int ChargingCount { get; set; }
    public int MaintenanceCount { get; set; }
}
