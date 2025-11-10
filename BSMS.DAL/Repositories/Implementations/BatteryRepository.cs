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

    public async Task<Battery?> GetBatteryWithStationAsync(int batteryId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(b => b.Station)
            .FirstOrDefaultAsync(b => b.BatteryId == batteryId);
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
        if (battery != null)
        {
            battery.Status = newStatus;
            battery.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> IsBatteryCompatibleAsync(string batteryModel, string vehicleModel)
    {
        // TODO: Implement actual compatibility logic
        // For now, simple model matching
        // In production, you'd have a compatibility matrix table

        await Task.CompletedTask; // Placeholder for async operation

        // Simple rule: Standard batteries work with all vehicles
        if (batteryModel.Contains("Standard"))
            return true;

        // Extended and Premium require specific vehicle types
        // This is placeholder logic - implement based on actual requirements
        return true;
    }
}
