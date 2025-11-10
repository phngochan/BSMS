using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace BSMS.BLL.Services.Implementations;

public class BatteryService : IBatteryService
{
    private readonly IBatteryRepository _batteryRepo;
    private readonly ILogger<BatteryService> _logger;

    public BatteryService(IBatteryRepository batteryRepo, ILogger<BatteryService> logger)
    {
        _batteryRepo = batteryRepo;
        _logger = logger;
    }

    public async Task<IEnumerable<Battery>> GetAvailableBatteriesAsync(int stationId)
    {
        try
        {
            return await _batteryRepo.GetAvailableBatteriesAsync(stationId);
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
            return await _batteryRepo.GetAvailableBatteriesByModelAsync(stationId, model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available batteries for StationId: {StationId}, Model: {Model}",
                stationId, model);
            throw;
        }
    }

    public async Task<IEnumerable<BatteryModelGroupDto>> GetBatteriesGroupedByModelAsync(int stationId)
    {
        try
        {
            return await _batteryRepo.GetBatteriesGroupedByModelAsync(stationId);
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
            return await _batteryRepo.GetBatteryWithStationAsync(batteryId);
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
            await _batteryRepo.UpdateBatteryStatusAsync(batteryId, newStatus);
            
            _logger.LogInformation("Battery status updated: BatteryId={BatteryId}, NewStatus={Status}",
                batteryId, newStatus);
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
            return await _batteryRepo.IsBatteryCompatibleAsync(batteryModel, vehicleModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check battery compatibility: Battery={BatteryModel}, Vehicle={VehicleModel}",
                batteryModel, vehicleModel);
            throw;
        }
    }
}
