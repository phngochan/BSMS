using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;
using BSMS.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace BSMS.DAL.Repositories.Implementations;

public class ChangingStationRepository : GenericRepository<ChangingStation>, IChangingStationRepository
{
    public ChangingStationRepository(BSMSDbContext context) : base(context)
    {
    }

    public async Task<int> CountByStatusAsync(StationStatus? status = null)
    {
        var query = _dbSet.AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(s => s.Status == status);
        }

        return await query.CountAsync();
    }

    public async Task<ChangingStation?> GetStationWithDetailsAsync(int stationId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(s => s.Batteries)
            .Include(s => s.StationStaffs)
                .ThenInclude(ss => ss.User)
            .Include(s => s.Supports)
            .FirstOrDefaultAsync(s => s.StationId == stationId);
    }

    public async Task<IEnumerable<ChangingStation>> GetStationsWithDetailsAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Include(s => s.Batteries)
            .Include(s => s.StationStaffs)
                .ThenInclude(ss => ss.User)
            .Include(s => s.Supports)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }
}
