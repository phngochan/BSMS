using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace BSMS.BLL.Services.Implementations;

public class ReservationService : IReservationService
{
    private readonly IReservationRepository _reservationRepo;
    private readonly IStationService _stationService;
    private readonly IBatteryService _batteryService;
    private readonly IUserService _userService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReservationService> _logger;

    public ReservationService(
        IReservationRepository reservationRepo,
        IStationService stationService,
        IBatteryService batteryService,
        IUserService userService,
        IServiceProvider serviceProvider,
        ILogger<ReservationService> logger)
    {
        _reservationRepo = reservationRepo;
        _stationService = stationService;
        _batteryService = batteryService;
        _userService = userService;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<Reservation> CreateReservationAsync(int userId, int vehicleId, int stationId, DateTime timeSlot)
    {
        try
        {
            if (timeSlot.Kind != DateTimeKind.Utc)
            {
                timeSlot = timeSlot.ToUniversalTime();
            }

            var (canCreate, errorMessage) = await ValidateReservationAsync(userId, vehicleId, stationId, timeSlot);
            if (!canCreate)
            {
                throw new InvalidOperationException(errorMessage);
            }

            var user = await _userService.GetUserWithVehiclesAsync(userId);
            var vehicle = user?.Vehicles.FirstOrDefault(v => v.VehicleId == vehicleId);
            
            if (vehicle == null)
            {
                throw new InvalidOperationException("Xe không tồn tại.");
            }

            var availableBatteries = await _batteryService.GetAvailableBatteriesAsync(stationId);
            var compatibleBatteries = new List<Battery>();
            
            foreach (var battery in availableBatteries)
            {
                if (battery.Model == vehicle.BatteryModel)
                {
                    compatibleBatteries.Add(battery);
                }
                else if (await _batteryService.CheckCompatibilityAsync(battery.Model, vehicle.BatteryModel))
                {
                    compatibleBatteries.Add(battery);
                }
            }

            if (!compatibleBatteries.Any())
            {
                throw new InvalidOperationException("Không có pin sẵn có tương thích với xe của bạn.");
            }

            var selectedBattery = compatibleBatteries.First();
            await _batteryService.UpdateBatteryStatusAsync(selectedBattery.BatteryId, BatteryStatus.Booked);

            var reservation = new Reservation
            {
                UserId = userId,
                VehicleId = vehicleId,
                StationId = stationId,
                BatteryId = selectedBattery.BatteryId,
                ScheduledTime = timeSlot,
                Status = ReservationStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            await _reservationRepo.CreateAsync(reservation);

            // Tạo swap transaction với status Pending
            var swapTransactionService = _serviceProvider.GetRequiredService<ISwapTransactionService>();
            await swapTransactionService.CreatePendingSwapTransactionAsync(
                userId,
                vehicleId,
                stationId,
                selectedBattery.BatteryId);

            _logger.LogInformation("Reservation created: UserId={UserId}, StationId={StationId}, BatteryId={BatteryId}, TimeSlot={TimeSlot}",
                userId, stationId, selectedBattery.BatteryId, timeSlot);

            return reservation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create reservation for UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> CancelReservationAsync(int reservationId, int userId)
    {
        try
        {
            var reservation = await _reservationRepo.GetReservationWithDetailsAsync(reservationId);

            if (reservation == null)
            {
                _logger.LogWarning("Reservation not found: {ReservationId}", reservationId);
                return false;
            }

            if (reservation.UserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to cancel reservation {ReservationId} owned by {OwnerId}",
                    userId, reservationId, reservation.UserId);
                return false;
            }

            if (reservation.Status != ReservationStatus.Active)
            {
                _logger.LogWarning("Cannot cancel reservation {ReservationId} with status {Status}",
                    reservationId, reservation.Status);
                return false;
            }

            var oneHourBefore = reservation.ScheduledTime.AddHours(-1);
            if (DateTime.UtcNow >= oneHourBefore)
            {
                _logger.LogWarning("Cannot cancel reservation {ReservationId} - less than 1 hour before time slot",
                    reservationId);
                return false;
            }

            await _reservationRepo.UpdateStatusAsync(reservationId, ReservationStatus.Cancelled);

            if (reservation.BatteryId.HasValue)
            {
                await _batteryService.UpdateBatteryStatusAsync(reservation.BatteryId.Value, BatteryStatus.Full);
                _logger.LogInformation("Battery status reset to Full: BatteryId={BatteryId}", reservation.BatteryId.Value);
            }

            // Cập nhật swap transaction thành Cancelled
            if (reservation.BatteryId.HasValue)
            {
                var swapTransactionService = _serviceProvider.GetRequiredService<ISwapTransactionService>();
                await swapTransactionService.CancelSwapTransactionAsync(
                    reservation.UserId,
                    reservation.VehicleId,
                    reservation.StationId,
                    reservation.BatteryId.Value);
            }

            _logger.LogInformation("Reservation cancelled: {ReservationId} by User {UserId}",
                reservationId, userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel reservation {ReservationId}", reservationId);
            throw;
        }
    }

    public async Task<bool> CancelReservationByStaffAsync(int reservationId)
    {
        try
        {
            var reservation = await _reservationRepo.GetReservationWithDetailsAsync(reservationId);

            if (reservation == null)
            {
                _logger.LogWarning("Reservation not found: {ReservationId}", reservationId);
                return false;
            }

            if (reservation.Status != ReservationStatus.Active)
            {
                _logger.LogWarning("Cannot cancel reservation {ReservationId} with status {Status}",
                    reservationId, reservation.Status);
                return false;
            }

            await _reservationRepo.UpdateStatusAsync(reservationId, ReservationStatus.Cancelled);

            if (reservation.BatteryId.HasValue)
            {
                await _batteryService.UpdateBatteryStatusAsync(reservation.BatteryId.Value, BatteryStatus.Full);
                _logger.LogInformation("Battery status reset to Full: BatteryId={BatteryId}", reservation.BatteryId.Value);

                // Cập nhật swap transaction thành Cancelled
                var swapTransactionService = _serviceProvider.GetRequiredService<ISwapTransactionService>();
                await swapTransactionService.CancelSwapTransactionAsync(
                    reservation.UserId,
                    reservation.VehicleId,
                    reservation.StationId,
                    reservation.BatteryId.Value);
            }

            _logger.LogInformation("Reservation cancelled by staff: {ReservationId}, UserId: {UserId}",
                reservationId, reservation.UserId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel reservation by staff: {ReservationId}", reservationId);
            throw;
        }
    }

    public async Task<bool> ConfirmReservationAsync(int reservationId)
    {
        try
        {
            var reservation = await _reservationRepo.GetReservationWithDetailsAsync(reservationId);

            if (reservation == null || reservation.Status != ReservationStatus.Active)
            {
                return false;
            }

            _logger.LogInformation("Reservation confirmed by staff: {ReservationId}", reservationId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm reservation {ReservationId}", reservationId);
            throw;
        }
    }

    public async Task<bool> CompleteReservationAsync(int reservationId)
    {
        try
        {
            var reservation = await _reservationRepo.GetReservationWithDetailsAsync(reservationId);

            if (reservation == null || reservation.Status != ReservationStatus.Active)
            {
                return false;
            }

            await _reservationRepo.UpdateStatusAsync(reservationId, ReservationStatus.Completed);

            _logger.LogInformation("Reservation completed: {ReservationId}", reservationId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to complete reservation {ReservationId}", reservationId);
            throw;
        }
    }

    public async Task<Reservation?> GetActiveReservationAsync(int userId)
    {
        try
        {
            return await _reservationRepo.GetActiveReservationByUserAsync(userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active reservation for UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<Reservation?> GetReservationDetailsAsync(int reservationId)
    {
        try
        {
            return await _reservationRepo.GetReservationWithDetailsAsync(reservationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get reservation details: {ReservationId}", reservationId);
            throw;
        }
    }

    public async Task<IEnumerable<Reservation>> GetMyReservationsAsync(int userId, int pageNumber = 1, int pageSize = 10)
    {
        try
        {
            return await _reservationRepo.GetReservationsByUserAsync(userId, pageNumber, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get reservations for UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<Reservation>> GetStationReservationsAsync(int stationId, DateTime date)
    {
        try
        {
            return await _reservationRepo.GetReservationsByStationAsync(stationId, date);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get reservations for StationId: {StationId}, Date: {Date}",
                stationId, date);
            throw;
        }
    }

    public async Task<int> AutoCancelLateReservationsAsync()
    {
        try
        {
            var lateReservations = await _reservationRepo.GetLateReservationsAsync();
            int cancelledCount = 0;

            foreach (var reservation in lateReservations)
            {
                await _reservationRepo.UpdateStatusAsync(reservation.ReservationId, ReservationStatus.Cancelled);

                if (reservation.BatteryId.HasValue)
                {
                    await _batteryService.UpdateBatteryStatusAsync(reservation.BatteryId.Value, BatteryStatus.Full);
                    _logger.LogInformation("Battery status reset to Full after auto-cancel: BatteryId={BatteryId}", 
                        reservation.BatteryId.Value);

                    // Cập nhật swap transaction thành Cancelled
                    var swapTransactionService = _serviceProvider.GetRequiredService<ISwapTransactionService>();
                    await swapTransactionService.CancelSwapTransactionAsync(
                        reservation.UserId,
                        reservation.VehicleId,
                        reservation.StationId,
                        reservation.BatteryId.Value);
                }

                cancelledCount++;

                _logger.LogInformation("Auto-cancelled late reservation: {ReservationId}, UserId: {UserId}",
                    reservation.ReservationId, reservation.UserId);
            }

            return cancelledCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to auto-cancel late reservations");
            throw;
        }
    }

    public async Task<(bool CanCreate, string? ErrorMessage)> ValidateReservationAsync(int userId, int vehicleId, int stationId, DateTime timeSlot)
    {
        try
        {
            if (await _reservationRepo.HasActiveReservationAsync(userId))
            {
                return (false, "Bạn đã có một đặt chỗ đang hoạt động. Vui lòng hủy trước khi tạo mới.");
            }

            if (timeSlot <= DateTime.UtcNow)
            {
                return (false, "Thời gian đặt chỗ phải trong tương lai.");
            }

            if (timeSlot < DateTime.UtcNow.AddMinutes(30))
            {
                return (false, "Đặt chỗ phải trước ít nhất 30 phút.");
            }

            var station = await _stationService.GetStationDetailsAsync(stationId);
            if (station == null)
            {
                return (false, "Trạm không tồn tại.");
            }

            if (station.Status != StationStatus.Active)
            {
                return (false, "Trạm hiện không hoạt động.");
            }

            var availableBatteries = await _batteryService.GetAvailableBatteriesAsync(stationId);
            if (!availableBatteries.Any())
            {
                return (false, "Trạm hiện không có pin sẵn có.");
            }

            var user = await _userService.GetUserWithVehiclesAsync(userId);
            var vehicle = user?.Vehicles.FirstOrDefault(v => v.VehicleId == vehicleId);
            
            if (vehicle == null)
            {
                return (false, "Xe không tồn tại.");
            }

            var compatibleBatteries = new List<Battery>();
            foreach (var battery in availableBatteries)
            {
                if (battery.Model == vehicle.BatteryModel)
                {
                    compatibleBatteries.Add(battery);
                }
                else if (await _batteryService.CheckCompatibilityAsync(battery.Model, vehicle.BatteryModel))
                {
                    compatibleBatteries.Add(battery);
                }
            }

            if (!compatibleBatteries.Any())
            {
                return (false, $"Trạm không có pin tương thích với xe của bạn (Model: {vehicle.BatteryModel}).");
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate reservation for UserId: {UserId}", userId);
            throw;
        }
    }
}
