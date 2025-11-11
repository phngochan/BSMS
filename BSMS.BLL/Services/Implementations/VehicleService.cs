using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace BSMS.BLL.Services.Implementations;

public class VehicleService : IVehicleService
{
    private readonly IVehicleRepository _vehicleRepo;
    private readonly IReservationRepository _reservationRepo;
    private readonly ILogger<VehicleService> _logger;

    public VehicleService(
        IVehicleRepository vehicleRepo,
        IReservationRepository reservationRepo,
        ILogger<VehicleService> logger)
    {
        _vehicleRepo = vehicleRepo;
        _reservationRepo = reservationRepo;
        _logger = logger;
    }

    public async Task<IEnumerable<Vehicle>> GetUserVehiclesAsync(int userId)
    {
        try
        {
            return await _vehicleRepo.GetVehiclesByUserIdAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get vehicles for UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<Vehicle?> GetVehicleByIdAsync(int vehicleId, int userId)
    {
        try
        {
            var vehicle = await _vehicleRepo.GetByIdAsync(vehicleId);
            
            // Nếu userId = 0, cho phép xem tất cả (dành cho Staff/Admin)
            if (vehicle == null || (userId > 0 && vehicle.UserId != userId))
            {
                return null;
            }
            
            return vehicle;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get vehicle {VehicleId} for UserId: {UserId}", vehicleId, userId);
            throw;
        }
    }

    public async Task<Vehicle> CreateVehicleAsync(int userId, string vin, string batteryModel, string batteryType)
    {
        try
        {
            if (await IsVinExistsAsync(vin))
            {
                throw new InvalidOperationException("VIN đã tồn tại trong hệ thống.");
            }

            var vehicle = new Vehicle
            {
                UserId = userId,
                Vin = vin.Trim().ToUpper(),
                BatteryModel = batteryModel.Trim(),
                BatteryType = batteryType.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            var created = await _vehicleRepo.CreateAsync(vehicle);
            _logger.LogInformation("Vehicle created: VehicleId={VehicleId}, VIN={Vin}, UserId={UserId}", 
                created.VehicleId, vin, userId);

            return created;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create vehicle for UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<Vehicle> UpdateVehicleAsync(int vehicleId, int userId, string vin, string batteryModel, string batteryType)
    {
        try
        {
            var vehicle = await GetVehicleByIdAsync(vehicleId, userId);
            if (vehicle == null)
            {
                throw new InvalidOperationException("Xe không tồn tại hoặc không thuộc về bạn.");
            }

            if (await IsVinExistsAsync(vin, vehicleId))
            {
                throw new InvalidOperationException("VIN đã tồn tại trong hệ thống.");
            }

            vehicle.Vin = vin.Trim().ToUpper();
            vehicle.BatteryModel = batteryModel.Trim();
            vehicle.BatteryType = batteryType.Trim();

            var updated = await _vehicleRepo.UpdateAsync(vehicle);
            _logger.LogInformation("Vehicle updated: VehicleId={VehicleId}, VIN={Vin}, UserId={UserId}", 
                vehicleId, vin, userId);

            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update vehicle {VehicleId} for UserId: {UserId}", vehicleId, userId);
            throw;
        }
    }

    public async Task<bool> DeleteVehicleAsync(int vehicleId, int userId)
    {
        try
        {
            var vehicle = await GetVehicleByIdAsync(vehicleId, userId);
            if (vehicle == null)
            {
                return false;
            }

            if (await _reservationRepo.HasActiveReservationByVehicleAsync(vehicleId))
            {
                throw new InvalidOperationException("Không thể xóa xe đang có đặt chỗ hoạt động. Vui lòng hủy đặt chỗ trước.");
            }

            var deleted = await _vehicleRepo.DeleteAsync(vehicle);
            if (deleted)
            {
                _logger.LogInformation("Vehicle deleted: VehicleId={VehicleId}, UserId={UserId}", vehicleId, userId);
            }

            return deleted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete vehicle {VehicleId} for UserId: {UserId}", vehicleId, userId);
            throw;
        }
    }

    public async Task<bool> IsVinExistsAsync(string vin, int? excludeVehicleId = null)
    {
        try
        {
            return await _vehicleRepo.IsVinExistsAsync(vin.Trim().ToUpper(), excludeVehicleId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check VIN existence: {Vin}", vin);
            throw;
        }
    }
}

