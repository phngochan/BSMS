using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;

namespace BSMS.DAL.Repositories;

public interface IStationRepository : IGenericRepository<ChangingStation>
{
    /// <summary>
    /// Get all active stations
    /// </summary>
    Task<IEnumerable<ChangingStation>> GetActiveStationsAsync();

    /// <summary>
    /// Get station by ID with related data (batteries, staff)
    /// </summary>
    Task<ChangingStation?> GetStationWithDetailsAsync(int stationId);

    /// <summary>
    /// Get stations within a radius from coordinates
    /// </summary>
    Task<IEnumerable<ChangingStation>> GetNearbyStationsAsync(double latitude, double longitude, double radiusKm = 10);

    /// <summary>
    /// Get station with available battery count
    /// </summary>
    Task<IEnumerable<StationWithAvailabilityDto>> GetStationsWithAvailabilityAsync();

    /// <summary>
    /// Search stations by name or address
    /// </summary>
    Task<IEnumerable<ChangingStation>> SearchStationsAsync(string searchTerm);
}

public class StationWithAvailabilityDto
{
    public int StationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Capacity { get; set; }
    public string Status { get; set; } = string.Empty;
    public int AvailableBatteries { get; set; }
}
