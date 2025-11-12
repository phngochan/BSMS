using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;
using BSMS.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace BSMS.DAL.Repositories.Implementations;

public class SupportRepository : GenericRepository<Support>, ISupportRepository
{
    public SupportRepository(BSMSDbContext context) : base(context)
    {
    }

    public async Task<int> CountByStatusAsync(SupportStatus status)
    {
        return await _dbSet.CountAsync(s => s.Status == status);
    }

    public async Task<IEnumerable<Support>> GetOpenSupportsAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Where(s => s.Status != SupportStatus.Closed)
            .Include(s => s.User)
            .Include(s => s.Station)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<Support?> GetSupportWithDetailsAsync(int supportId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Station)
            .FirstOrDefaultAsync(s => s.SupportId == supportId);
    }

    public async Task<IEnumerable<Support>> GetSupportsByStationAsync(int stationId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(s => s.StationId == stationId)
            .Include(s => s.User)
            .Include(s => s.Station)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Support>> GetSupportsByUserAsync(int userId)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(s => s.UserId == userId)
            .Include(s => s.User)
            .Include(s => s.Station)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Support>> GetAllSupportsAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Station)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }
}
