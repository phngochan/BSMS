using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;
using BSMS.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace BSMS.DAL.Repositories.Implementations;

public class ReservationRepository : GenericRepository<Reservation>, IReservationRepository
{
    public ReservationRepository(BSMSDbContext context) : base(context)
    {
    }

    public async Task<Reservation?> GetActiveReservationByUserAsync(int userId)
    {
        return await _dbSet
            .Include(r => r.Station)
            .Include(r => r.User)
            .Include(r => r.Battery)
            .Include(r => r.Vehicle)
            .Where(r => r.UserId == userId
                && (r.Status == ReservationStatus.Active))
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<Reservation?> GetReservationWithDetailsAsync(int reservationId)
    {
        return await _dbSet
            .Include(r => r.User)
            .Include(r => r.Vehicle)
            .Include(r => r.Station)
            .Include(r => r.Battery)
            .FirstOrDefaultAsync(r => r.ReservationId == reservationId);
    }

    public async Task<IEnumerable<Reservation>> GetReservationsByUserAsync(int userId, int pageNumber = 1, int pageSize = 10)
    {
        return await _dbSet
            .Include(r => r.Station)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reservation>> GetReservationsByStationAsync(int stationId, DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = date.Date.AddDays(1);

        return await _dbSet
            .Include(r => r.User)
            .Where(r => r.StationId == stationId
                && r.ScheduledTime >= startOfDay
                && r.ScheduledTime < endOfDay
                && (r.Status == ReservationStatus.Active))
            .OrderBy(r => r.ScheduledTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reservation>> GetLateReservationsAsync()
    {
        var cutoffTime = DateTime.UtcNow.AddMinutes(-30);

        return await _dbSet
            .Include(r => r.Battery)
            .Where(r => (r.Status == ReservationStatus.Active)
                && r.ScheduledTime < cutoffTime)
            .ToListAsync();
    }

    public async Task UpdateStatusAsync(int reservationId, ReservationStatus status)
    {
        var reservation = await _dbSet.FindAsync(reservationId);
        if (reservation != null)
        {
            reservation.Status = status;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> HasActiveReservationAsync(int userId)
    {
        return await _dbSet
            .AnyAsync(r => r.UserId == userId
                && (r.Status == ReservationStatus.Active));
    }

    public async Task<bool> HasActiveReservationByVehicleAsync(int vehicleId)
    {
        return await _dbSet
            .AnyAsync(r => r.VehicleId == vehicleId
                && (r.Status == ReservationStatus.Active));
    }
}
