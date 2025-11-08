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

    public async Task<StationStaff?> GetAssignmentWithDetailsAsync(int staffId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(ss => ss.User)
            .Include(ss => ss.Station)
            .FirstOrDefaultAsync(ss => ss.StaffId == staffId);
    }

    public async Task<IEnumerable<StationStaff>> GetAssignmentsAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Include(ss => ss.User)
            .Include(ss => ss.Station)
            .OrderBy(ss => ss.Station.Name)
            .ThenBy(ss => ss.User.FullName)
            .ToListAsync();
    }

    public async Task<IEnumerable<StationStaff>> GetAssignmentsByStationAsync(int stationId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(ss => ss.StationId == stationId)
            .Include(ss => ss.User)
            .Include(ss => ss.Station)
            .OrderBy(ss => ss.User.FullName)
            .ToListAsync();
    }

    public async Task<bool> IsUserAssignedAsync(int userId, int stationId)
    {
        return await _dbSet.AnyAsync(ss => ss.UserId == userId && ss.StationId == stationId);
    }
}
