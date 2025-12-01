using BSMS.BusinessObjects.DTOs;
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

    public async Task<IEnumerable<Battery>> GetAvailableBatteriesAsync(int stationId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(b => b.StationId == stationId && b.Status == BatteryStatus.Full)
            .OrderBy(b => b.Model)
            .ThenBy(b => b.BatteryId)
            .ToListAsync();
    }

    public async Task<IEnumerable<Battery>> GetAvailableBatteriesByModelAsync(int stationId, string model)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(b => b.StationId == stationId
                        && b.Status == BatteryStatus.Full
                        && b.Model == model)
            .OrderBy(b => b.BatteryId)
            .ToListAsync();
    }

    public async Task<IEnumerable<BatteryModelGroupDto>> GetBatteriesGroupedByModelAsync(int stationId)
    {
        var batteries = await _dbSet
            .AsNoTracking()
            .Where(b => b.StationId == stationId)
            .ToListAsync();

        return batteries
            .GroupBy(b => new { b.Model, b.Capacity })
            .Select(g => new BatteryModelGroupDto
            {
                Model = g.Key.Model,
                Capacity = g.Key.Capacity,
                TotalCount = g.Count(),
                AvailableCount = g.Count(b => b.Status == BatteryStatus.Full),
                ChargingCount = g.Count(b => b.Status == BatteryStatus.Charging),
                MaintenanceCount = g.Count(b => b.Status == BatteryStatus.Defective)
            })
            .OrderBy(dto => dto.Model)
            .ToList();
    }

    public async Task UpdateBatteryStatusAsync(int batteryId, BatteryStatus newStatus)
    {
        var battery = await _dbSet.FindAsync(batteryId);
        if (battery == null)
        {
            return;
        }

        battery.Status = newStatus;
        battery.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<bool> IsBatteryCompatibleAsync(string batteryModel, string vehicleModel)
    {
        await Task.CompletedTask;
        return true;
    }
    public async Task<IEnumerable<Battery>> GetBatteriesByStationAsync(int stationId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(b => b.StationId == stationId)
            .OrderBy(b => b.Model)
            .ThenBy(b => b.BatteryId)
            .ToListAsync();
    }
}
