using BSMS.BusinessObjects.Models;

namespace BSMS.BLL.Services;

public interface IReservationService
{
    Task<Reservation> CreateReservationAsync(int userId, int vehicleId, int stationId, DateTime timeSlot);
    Task<bool> CancelReservationAsync(int reservationId, int userId);
    Task<bool> CancelReservationByStaffAsync(int reservationId);
    Task<bool> ConfirmReservationAsync(int reservationId);
    Task<bool> CompleteReservationAsync(int reservationId);
    Task<Reservation?> GetActiveReservationAsync(int userId);
    Task<Reservation?> GetReservationDetailsAsync(int reservationId);
    Task<IEnumerable<Reservation>> GetMyReservationsAsync(int userId, int pageNumber = 1, int pageSize = 10);
    Task<IEnumerable<Reservation>> GetStationReservationsAsync(int stationId, DateTime date);
    Task<int> AutoCancelLateReservationsAsync();
    Task<(bool CanCreate, string? ErrorMessage)> ValidateReservationAsync(int userId, int vehicleId, int stationId, DateTime timeSlot);

    Task<IEnumerable<Reservation>> GetUpcomingReservationsAsync(DateTime fromUtc, DateTime toUtc);
}
