using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;
using BSMS.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace BSMS.DAL.Repositories.Implementations;

public class AlertRepository : GenericRepository<Alert>, IAlertRepository
{
    public AlertRepository(BSMSDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Alert>> GetUnresolvedAlertsAsync()
    {
        return await _dbSet
            .Where(a => !a.Resolved)
            .Include(a => a.Station)
            .Include(a => a.Battery)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Alert>> GetAlertsByUserRoleAsync(string role, int? userId = null)
    {
        var query = _dbSet
            .Where(a => !a.Resolved)
            .Include(a => a.Station)
            .Include(a => a.Battery)
            .AsQueryable();
        
        return await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountByUserAsync(int userId)
    {
        return await _dbSet.CountAsync(a => !a.Resolved);
    }

    public async Task MarkAsResolvedAsync(int alertId)
    {
        var alert = await _dbSet.FindAsync(alertId);
        if (alert != null)
        {
            alert.Resolved = true;
            await _context.SaveChangesAsync();
        }
    }
}
