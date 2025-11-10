using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace BSMS.BLL.Services.Implementations;

public class ReservationService : IReservationService
{
    private readonly IReservationRepository _reservationRepo;
    private readonly ILogger<ReservationService> _logger;

    public ReservationService(
        IReservationRepository reservationRepo,
        ILogger<ReservationService> logger)
    {
        _reservationRepo = reservationRepo;
        _logger = logger;
    }

    public async Task<Reservation> CreateReservationAsync(int userId, int stationId, DateTime timeSlot)
    {
        try
        {
            // Validate first
            var (canCreate, errorMessage) = await ValidateReservationAsync(userId, stationId, timeSlot);
            if (!canCreate)
            {
                throw new InvalidOperationException(errorMessage);
            }

            var reservation = new Reservation
            {
                UserId = userId,
                StationId = stationId,
                ScheduledTime = timeSlot,
                Status = ReservationStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            await _reservationRepo.CreateAsync(reservation);

            _logger.LogInformation("Reservation created: UserId={UserId}, StationId={StationId}, TimeSlot={TimeSlot}",
                userId, stationId, timeSlot);

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

            // Check ownership
            if (reservation.UserId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to cancel reservation {ReservationId} owned by {OwnerId}",
                    userId, reservationId, reservation.UserId);
                return false;
            }

            // Check status
            if (reservation.Status != ReservationStatus.Active)
            {
                _logger.LogWarning("Cannot cancel reservation {ReservationId} with status {Status}",
                    reservationId, reservation.Status);
                return false;
            }

            // Check time constraint (cannot cancel if < 1 hour before time slot)
            var oneHourBefore = reservation.ScheduledTime.AddHours(-1);
            if (DateTime.UtcNow >= oneHourBefore)
            {
                _logger.LogWarning("Cannot cancel reservation {ReservationId} - less than 1 hour before time slot",
                    reservationId);
                return false;
            }

            await _reservationRepo.UpdateStatusAsync(reservationId, ReservationStatus.Cancelled);

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

    public async Task<(bool CanCreate, string? ErrorMessage)> ValidateReservationAsync(int userId, int stationId, DateTime timeSlot)
    {
        try
        {
            // Rule 1: User can only have 1 active reservation
            if (await _reservationRepo.HasActiveReservationAsync(userId))
            {
                return (false, "You already have an active reservation. Please cancel it before creating a new one.");
            }

            // Rule 2: Time slot must be in the future
            if (timeSlot <= DateTime.UtcNow)
            {
                return (false, "Time slot must be in the future.");
            }

            // Rule 3: Time slot should be at least 30 minutes from now
            if (timeSlot < DateTime.UtcNow.AddMinutes(30))
            {
                return (false, "Reservation must be at least 30 minutes in advance.");
            }

            // Additional validation can be added here:
            // - Check if station exists and is active
            // - Check if station has available batteries
            // - Check battery compatibility with user's vehicle

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate reservation for UserId: {UserId}", userId);
            throw;
        }
    }
}
