using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;

namespace BSMS.BLL.Services;

public interface IChangingStationService
{
    Task<IEnumerable<ChangingStation>> GetStationsAsync();
    Task<IEnumerable<ChangingStation>> GetStationsWithDetailsAsync();
    Task<ChangingStation?> GetStationAsync(int stationId, bool includeDetails = false);
    Task<ChangingStation> CreateStationAsync(ChangingStation station);
    Task UpdateStationAsync(ChangingStation station);
    Task DeleteStationAsync(int stationId);
    Task<int> GetStationCountAsync(StationStatus? status = null);
}
