using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace BSMS.BLL.Services.Implementations;

public class StationService : IStationService
{
    private readonly IStationRepository _stationRepo;
    private readonly ILogger<StationService> _logger;

    public StationService(IStationRepository stationRepo, ILogger<StationService> logger)
    {
        _stationRepo = stationRepo;
        _logger = logger;
    }

    public async Task<IEnumerable<ChangingStation>> GetActiveStationsAsync()
    {
        try
        {
            return await _stationRepo.GetActiveStationsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active stations");
            throw;
        }
    }

    public async Task<ChangingStation?> GetStationDetailsAsync(int stationId)
    {
        try
        {
            return await _stationRepo.GetStationWithDetailsAsync(stationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get station details for StationId: {StationId}", stationId);
            throw;
        }
    }

    public async Task<IEnumerable<StationWithAvailabilityDto>> GetStationsWithAvailabilityAsync()
    {
        try
        {
            return await _stationRepo.GetStationsWithAvailabilityAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get stations with availability");
            throw;
        }
    }

    public async Task<IEnumerable<StationWithDistanceDto>> GetNearbyStationsAsync(double latitude, double longitude, double radiusKm = 10)
    {
        try
        {
            var stations = await _stationRepo.GetNearbyStationsAsync(latitude, longitude, radiusKm);
            var stationsWithAvailability = await _stationRepo.GetStationsWithAvailabilityAsync();

            var result = stations
                .Join(stationsWithAvailability,
                    s => s.StationId,
                    sa => sa.StationId,
                    (s, sa) => new StationWithDistanceDto
                    {
                        StationId = s.StationId,
                        Name = s.Name,
                        Address = s.Address,
                        Latitude = s.Latitude,
                        Longitude = s.Longitude,
                        Capacity = s.Capacity,
                        Status = s.Status.ToString(),
                        AvailableBatteries = sa.AvailableBatteries,
                        Distance = CalculateDistance(latitude, longitude, s.Latitude, s.Longitude)
                    })
                .OrderBy(s => s.Distance)
                .ToList();

            _logger.LogInformation("Found {Count} stations within {Radius}km of ({Lat}, {Lon})",
                result.Count, radiusKm, latitude, longitude);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get nearby stations for coordinates: ({Lat}, {Lon})",
                latitude, longitude);
            throw;
        }
    }

    public async Task<IEnumerable<ChangingStation>> SearchStationsAsync(string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetActiveStationsAsync();
            }

            return await _stationRepo.SearchStationsAsync(searchTerm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search stations with term: {SearchTerm}", searchTerm);
            throw;
        }
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Earth radius in kilometers

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }

    private double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
}
