using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;

namespace BSMS.DAL.Repositories;

public interface IReservationRepository : IGenericRepository<Reservation>
{
    Task<Reservation?> GetActiveReservationByUserAsync(int userId);

    Task<Reservation?> GetReservationWithDetailsAsync(int reservationId);

    Task<IEnumerable<Reservation>> GetReservationsByUserAsync(int userId, int pageNumber = 1, int pageSize = 10);

    Task<IEnumerable<Reservation>> GetReservationsByStationAsync(int stationId, DateTime date);

    Task<IEnumerable<Reservation>> GetLateReservationsAsync();

    Task UpdateStatusAsync(int reservationId, ReservationStatus status);

    Task<bool> HasActiveReservationAsync(int userId);
    Task<bool> HasActiveReservationByVehicleAsync(int vehicleId);
}
