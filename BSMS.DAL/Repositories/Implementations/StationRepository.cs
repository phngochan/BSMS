using BSMS.BusinessObjects.DTOs;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;
using BSMS.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace BSMS.DAL.Repositories.Implementations;

public class StationRepository : GenericRepository<ChangingStation>, IStationRepository
{
    public StationRepository(BSMSDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<ChangingStation>> GetActiveStationsAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Where(s => s.Status == StationStatus.Active)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<ChangingStation?> GetStationWithDetailsAsync(int stationId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(s => s.Batteries)
            .Include(s => s.StationStaffs)
                .ThenInclude(ss => ss.User)
            .Include(s => s.Reservations)
            .Include(s => s.Supports)
            .Include(s => s.SwapTransactions)
            .Include(s => s.StationStatistics)
            .Include(s => s.Alerts)
            .FirstOrDefaultAsync(s => s.StationId == stationId);
    }

    public async Task<IEnumerable<ChangingStation>> GetNearbyStationsAsync(double latitude, double longitude, double radiusKm = 10)
    {
        var stations = await _dbSet
            .AsNoTracking()
            .Where(s => s.Status == StationStatus.Active)
            .ToListAsync();

        return stations
            .Select(s => new
            {
                Station = s,
                Distance = CalculateDistance(latitude, longitude, s.Latitude, s.Longitude)
            })
            .Where(x => x.Distance <= radiusKm)
            .OrderBy(x => x.Distance)
            .Select(x => x.Station)
            .ToList();
    }

    public async Task<IEnumerable<StationWithAvailabilityDto>> GetStationsWithAvailabilityAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Where(s => s.Status == StationStatus.Active)
            .Select(s => new StationWithAvailabilityDto
            {
                StationId = s.StationId,
                Name = s.Name,
                Address = s.Address,
                Latitude = s.Latitude,
                Longitude = s.Longitude,
                Capacity = s.Capacity,
                Status = s.Status.ToString(),
                AvailableBatteries = s.Batteries.Count(b => b.Status == BatteryStatus.Full)
            })
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<ChangingStation>> SearchStationsAsync(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return await GetActiveStationsAsync();
        }

        var lowerSearchTerm = searchTerm.Trim().ToLower();
        
        return await _dbSet
            .AsNoTracking()
            .Where(s => s.Status == StationStatus.Active
                && (s.Name.ToLower().Contains(lowerSearchTerm) 
                    || s.Address.ToLower().Contains(lowerSearchTerm)))
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;

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
