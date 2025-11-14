using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;
using BSMS.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace BSMS.DAL.Repositories.Implementations;

public class StationStaffRepository : GenericRepository<StationStaff>, IStationStaffRepository
{
    public StationStaffRepository(BSMSDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<StationStaff>> GetAssignmentsAsync()
    {
        return await _context.StationStaffs
            .Include(ss => ss.User)
            .Include(ss => ss.Station)
            .OrderByDescending(ss => ss.AssignedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<StationStaff>> GetAssignmentsByStationAsync(int stationId)
    {
        return await _context.StationStaffs
            .Include(ss => ss.User)
            .Include(ss => ss.Station)
            .Where(ss => ss.StationId == stationId)
            .OrderBy(ss => ss.ShiftStart)
            .ToListAsync();
    }

    public async Task<StationStaff?> GetAssignmentByUserAsync(int userId)
    {
        return await _context.StationStaffs
            .Include(ss => ss.User)
            .Include(ss => ss.Station)
            .Where(ss => ss.UserId == userId && ss.IsActive)
            .OrderByDescending(ss => ss.AssignedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<StationStaff>> GetAssignmentsByUserAsync(int userId)
    {
        return await _context.StationStaffs
            .Include(ss => ss.Station)
            .Where(ss => ss.UserId == userId)
            .OrderByDescending(ss => ss.AssignedAt)
            .ToListAsync();
    }

    public async Task<StationStaff?> GetAssignmentWithDetailsAsync(int staffId)
    {
        return await _context.StationStaffs
            .Include(ss => ss.User)
            .Include(ss => ss.Station)
            .FirstOrDefaultAsync(ss => ss.StaffId == staffId);
    }

    public async Task<IEnumerable<StationStaff>> GetActiveStaffByStationAsync(int stationId, TimeSpan currentTime)
    {
        return await _context.StationStaffs
            .Include(ss => ss.User)
            .Include(ss => ss.Station)
            .Where(ss =>
                ss.StationId == stationId &&
                ss.IsActive &&
                ss.ShiftStart <= currentTime &&
                ss.ShiftEnd >= currentTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<StationStaff>> GetActiveAssignmentsByUserAsync(int userId)
    {
        return await _context.StationStaffs
            .Include(ss => ss.User)
            .Include(ss => ss.Station)
            .Where(ss => ss.UserId == userId && ss.IsActive)
            .ToListAsync();
    }


    public async Task<IEnumerable<StationStaff>> GetOverlappingShiftsAsync(
        int stationId,
        TimeSpan shiftStart,
        TimeSpan shiftEnd,
        int? excludeStaffId = null)
    {
        var query = _context.StationStaffs
            .Include(ss => ss.User)
            .Include(ss => ss.Station)
            .Where(ss =>
                ss.StationId == stationId &&
                ss.IsActive &&
              
                !(ss.ShiftEnd <= shiftStart || ss.ShiftStart >= shiftEnd));

        if (excludeStaffId.HasValue)
        {
            query = query.Where(ss => ss.StaffId != excludeStaffId.Value);
        }

        return await query.ToListAsync();
    }

  
    public async Task<int> CountActiveStaffInShiftAsync(
        int stationId,
        TimeSpan shiftStart,
        TimeSpan shiftEnd)
    {
        return await _context.StationStaffs
            .Where(ss =>
                ss.StationId == stationId &&
                ss.IsActive &&
                !(ss.ShiftEnd <= shiftStart || ss.ShiftStart >= shiftEnd))
            .CountAsync();
    }
}
