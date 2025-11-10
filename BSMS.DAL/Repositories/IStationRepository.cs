using BSMS.BusinessObjects.DTOs;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;

namespace BSMS.DAL.Repositories;

public interface IStationRepository : IGenericRepository<ChangingStation>
{
    Task<IEnumerable<ChangingStation>> GetActiveStationsAsync();

    Task<ChangingStation?> GetStationWithDetailsAsync(int stationId);

    Task<IEnumerable<ChangingStation>> GetNearbyStationsAsync(double latitude, double longitude, double radiusKm = 10);

    Task<IEnumerable<StationWithAvailabilityDto>> GetStationsWithAvailabilityAsync();

    Task<IEnumerable<ChangingStation>> SearchStationsAsync(string searchTerm);
}
