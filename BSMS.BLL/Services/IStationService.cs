using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;

namespace BSMS.BLL.Services;

public interface IStationService
{
    /// <summary>
    /// Get all active stations
    /// </summary>
    Task<IEnumerable<ChangingStation>> GetActiveStationsAsync();

    /// <summary>
    /// Get station details by ID
    /// </summary>
    Task<ChangingStation?> GetStationDetailsAsync(int stationId);

    /// <summary>
    /// Get stations with battery availability
    /// </summary>
    Task<IEnumerable<StationWithAvailabilityDto>> GetStationsWithAvailabilityAsync();

    /// <summary>
    /// Get nearby stations based on coordinates
    /// </summary>
    Task<IEnumerable<StationWithDistanceDto>> GetNearbyStationsAsync(double latitude, double longitude, double radiusKm = 10);

    /// <summary>
    /// Search stations by keyword
    /// </summary>
    Task<IEnumerable<ChangingStation>> SearchStationsAsync(string searchTerm);
}

public class StationWithDistanceDto
{
    public int StationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Capacity { get; set; }
    public string Status { get; set; } = string.Empty;
    public int AvailableBatteries { get; set; }
    public double Distance { get; set; } // in kilometers
}
