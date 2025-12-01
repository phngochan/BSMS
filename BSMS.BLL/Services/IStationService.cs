using BSMS.BusinessObjects.DTOs;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;

namespace BSMS.BLL.Services;

public interface IStationService
{
    Task<IEnumerable<ChangingStation>> GetActiveStationsAsync();
    Task<ChangingStation?> GetStationDetailsAsync(int stationId);
    Task<IEnumerable<StationWithAvailabilityDto>> GetStationsWithAvailabilityAsync();
    Task<IEnumerable<StationWithDistanceDto>> GetNearbyStationsAsync(double latitude, double longitude, double radiusKm = 10);
    Task<IEnumerable<ChangingStation>> SearchStationsAsync(string searchTerm);
}
