using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;

namespace BSMS.DAL.Repositories;

public interface IBatteryRepository : IGenericRepository<Battery>
{
    Task<IEnumerable<Battery>> GetBatteriesWithStationAsync();
    Task<IEnumerable<Battery>> GetByStationAsync(int stationId);
    Task<IEnumerable<Battery>> GetByStatusAsync(BatteryStatus status);
    Task<Dictionary<BatteryStatus, int>> GetStatusSummaryAsync(int? stationId = null);
    Task<Battery?> GetBatteryWithStationAsync(int batteryId);
}
