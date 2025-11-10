using BSMS.BusinessObjects.Models;

namespace BSMS.BLL.Services;

public interface IReservationService
{
    /// <summary>
    /// Create a new reservation for a driver
    /// </summary>
    /// <returns>Created reservation with QR code data</returns>
    Task<Reservation> CreateReservationAsync(int userId, int stationId, DateTime timeSlot);

    /// <summary>
    /// Cancel a reservation by ID (only if user is owner)
    /// </summary>
    Task<bool> CancelReservationAsync(int reservationId, int userId);

    /// <summary>
    /// Staff confirms/validates the reservation when driver arrives (checks if Active)
    /// Note: Status stays Active, this is just a validation step before swap
    /// </summary>
    Task<bool> ConfirmReservationAsync(int reservationId);

    /// <summary>
    /// Complete the reservation after swap is done (Active → Completed)
    /// </summary>
    Task<bool> CompleteReservationAsync(int reservationId);

    /// <summary>
    /// Get active reservation for a user
    /// </summary>
    Task<Reservation?> GetActiveReservationAsync(int userId);

    /// <summary>
    /// Get reservation by ID with all details
    /// </summary>
    Task<Reservation?> GetReservationDetailsAsync(int reservationId);

    /// <summary>
    /// Get all reservations for a user (with pagination)
    /// </summary>
    Task<IEnumerable<Reservation>> GetMyReservationsAsync(int userId, int pageNumber = 1, int pageSize = 10);

    /// <summary>
    /// Get all reservations for a station on a specific date
    /// </summary>
    Task<IEnumerable<Reservation>> GetStationReservationsAsync(int stationId, DateTime date);

    /// <summary>
    /// Auto-cancel late reservations (background job)
    /// </summary>
    Task<int> AutoCancelLateReservationsAsync();

    /// <summary>
    /// Check if user can create a new reservation (business rules)
    /// </summary>
    Task<(bool CanCreate, string? ErrorMessage)> ValidateReservationAsync(int userId, int stationId, DateTime timeSlot);
}
