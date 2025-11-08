using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;

namespace BSMS.BLL.Services.Implementations;

public class BatteryService : IBatteryService
{
    private readonly IBatteryRepository _batteryRepository;
    private readonly IChangingStationRepository _stationRepository;

    public BatteryService(
        IBatteryRepository batteryRepository,
        IChangingStationRepository stationRepository)
    {
        _batteryRepository = batteryRepository;
        _stationRepository = stationRepository;
    }

    public async Task<Battery> CreateBatteryAsync(Battery battery)
    {
        var station = await _stationRepository.GetSingleAsync(s => s.StationId == battery.StationId);
        if (station == null)
        {
            throw new InvalidOperationException("Station not found");
        }

        battery.UpdatedAt = DateTime.UtcNow;
        return await _batteryRepository.CreateAsync(battery);
    }

    public async Task DeleteBatteryAsync(int batteryId)
    {
        var battery = await _batteryRepository.GetSingleAsync(b => b.BatteryId == batteryId);
        if (battery == null)
        {
            throw new InvalidOperationException("Battery not found");
        }

        await _batteryRepository.DeleteAsync(battery);
    }

    public async Task<IEnumerable<Battery>> GetBatteriesAsync()
    {
        return await _batteryRepository.GetBatteriesWithStationAsync();
    }

    public async Task<IEnumerable<Battery>> GetBatteriesByStationAsync(int stationId)
    {
        return await _batteryRepository.GetByStationAsync(stationId);
    }

    public async Task<IEnumerable<Battery>> GetBatteriesByStatusAsync(BatteryStatus status)
    {
        return await _batteryRepository.GetByStatusAsync(status);
    }

    public async Task<Battery?> GetBatteryAsync(int batteryId)
    {
        return await _batteryRepository.GetBatteryWithStationAsync(batteryId);
    }

    public async Task<Dictionary<BatteryStatus, int>> GetStatusSummaryAsync(int? stationId = null)
    {
        return await _batteryRepository.GetStatusSummaryAsync(stationId);
    }

    public async Task UpdateBatteryAsync(Battery battery)
    {
        var existing = await _batteryRepository.GetSingleAsync(b => b.BatteryId == battery.BatteryId);
        if (existing == null)
        {
            throw new InvalidOperationException("Battery not found");
        }

        if (existing.StationId != battery.StationId)
        {
            var station = await _stationRepository.GetSingleAsync(s => s.StationId == battery.StationId);
            if (station == null)
            {
                throw new InvalidOperationException("Station not found");
            }
        }

        existing.Model = battery.Model;
        existing.Capacity = battery.Capacity;
        existing.Soh = battery.Soh;
        existing.Status = battery.Status;
        existing.StationId = battery.StationId;
        existing.LastMaintenance = battery.LastMaintenance;
        existing.UpdatedAt = DateTime.UtcNow;

        await _batteryRepository.UpdateAsync(existing);
    }
}
