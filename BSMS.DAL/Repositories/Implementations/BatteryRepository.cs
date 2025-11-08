using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;
using BSMS.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace BSMS.DAL.Repositories.Implementations;

public class BatteryRepository : GenericRepository<Battery>, IBatteryRepository
{
    public BatteryRepository(BSMSDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Battery>> GetBatteriesWithStationAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Include(b => b.Station)
            .OrderBy(b => b.BatteryId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Battery>> GetByStationAsync(int stationId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(b => b.StationId == stationId)
            .Include(b => b.Station)
            .OrderByDescending(b => b.UpdatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Battery>> GetByStatusAsync(BatteryStatus status)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(b => b.Status == status)
            .Include(b => b.Station)
            .OrderBy(b => b.StationId)
            .ToListAsync();
    }

    public async Task<Battery?> GetBatteryWithStationAsync(int batteryId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(b => b.Station)
            .FirstOrDefaultAsync(b => b.BatteryId == batteryId);
    }

    public async Task<Dictionary<BatteryStatus, int>> GetStatusSummaryAsync(int? stationId = null)
    {
        var query = _dbSet.AsNoTracking();

        if (stationId.HasValue)
        {
            query = query.Where(b => b.StationId == stationId.Value);
        }

        var result = await query
            .GroupBy(b => b.Status)
            .Select(group => new
            {
                group.Key,
                Count = group.Count()
            })
            .ToListAsync();

        return result.ToDictionary(k => k.Key, v => v.Count);
    }
}
