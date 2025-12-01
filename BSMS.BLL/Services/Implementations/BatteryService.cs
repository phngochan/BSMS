using BSMS.BusinessObjects.DTOs;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace BSMS.BLL.Services.Implementations;

public class BatteryService : IBatteryService
{
    private readonly IBatteryRepository _batteryRepository;
    private readonly IChangingStationRepository _stationRepository;
    private readonly ILogger<BatteryService> _logger;

    public BatteryService(
        IBatteryRepository batteryRepository,
        IChangingStationRepository stationRepository,
        ILogger<BatteryService> logger)
    {
        _batteryRepository = batteryRepository;
        _stationRepository = stationRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<Battery>> GetBatteriesAsync()
    {
        return await _batteryRepository.GetBatteriesWithStationAsync();
    }

    public async Task<IEnumerable<Battery>> GetBatteriesByStatusAsync(BatteryStatus status)
    {
        return await _batteryRepository.GetByStatusAsync(status);
    }

    public async Task<Dictionary<BatteryStatus, int>> GetStatusSummaryAsync(int? stationId = null)
    {
        return await _batteryRepository.GetStatusSummaryAsync(stationId);
    }

    public async Task<Battery?> GetBatteryAsync(int batteryId)
    {
        return await _batteryRepository.GetBatteryWithStationAsync(batteryId);
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
        existing.DefectNote = battery.DefectNote;
        existing.UpdatedAt = DateTime.UtcNow;

        await _batteryRepository.UpdateAsync(existing);
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

    public async Task<IEnumerable<Battery>> GetAvailableBatteriesAsync(int stationId)
    {
        try
        {
            return await _batteryRepository.GetAvailableBatteriesAsync(stationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available batteries for StationId: {StationId}", stationId);
            throw;
        }
    }

    public async Task<IEnumerable<Battery>> GetAvailableBatteriesByModelAsync(int stationId, string model)
    {
        try
        {
            return await _batteryRepository.GetAvailableBatteriesByModelAsync(stationId, model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to get available batteries for StationId: {StationId}, Model: {Model}",
                stationId,
                model);
            throw;
        }
    }

    public async Task<IEnumerable<Battery>> GetBatteriesByStationAsync(int stationId)
    {
        try
        {
            return await _batteryRepository.GetBatteriesByStationAsync(stationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get batteries for StationId: {StationId}", stationId);
            throw;
        }
    }

    public async Task<IEnumerable<BatteryModelGroupDto>> GetBatteriesGroupedByModelAsync(int stationId)
    {
        try
        {
            return await _batteryRepository.GetBatteriesGroupedByModelAsync(stationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get grouped batteries for StationId: {StationId}", stationId);
            throw;
        }
    }

    public async Task<Battery?> GetBatteryDetailsAsync(int batteryId)
    {
        try
        {
            return await _batteryRepository.GetBatteryWithStationAsync(batteryId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get battery details for BatteryId: {BatteryId}", batteryId);
            throw;
        }
    }

    public async Task UpdateBatteryStatusAsync(int batteryId, BatteryStatus newStatus)
    {
        try
        {
            await _batteryRepository.UpdateBatteryStatusAsync(batteryId, newStatus);
            _logger.LogInformation("Battery status updated: BatteryId={BatteryId}, NewStatus={Status}",
                batteryId,
                newStatus);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update battery status for BatteryId: {BatteryId}", batteryId);
            throw;
        }
    }

    public async Task<bool> CheckCompatibilityAsync(string batteryModel, string vehicleModel)
    {
        try
        {
            return await _batteryRepository.IsBatteryCompatibleAsync(batteryModel, vehicleModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to check battery compatibility: Battery={BatteryModel}, Vehicle={VehicleModel}",
                batteryModel,
                vehicleModel);
            throw;
        }
    }
}

