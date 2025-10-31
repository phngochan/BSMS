using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;
using BSMS.DAL.Context;
using Microsoft.EntityFrameworkCore;

namespace BSMS.DAL.Repositories.Implementations;

public class UserActivityLogRepository : GenericRepository<UserActivityLog>, IUserActivityLogRepository
{
    public UserActivityLogRepository(BSMSDbContext context) : base(context) { }

    public async Task<IEnumerable<UserActivityLog>> GetUserActivitiesAsync(int userId, int pageNumber = 1, int pageSize = 50)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(log => log.UserId == userId)
            .OrderByDescending(log => log.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<UserActivityLog>> GetRecentActivitiesAsync(int count = 100)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(log => log.User)
            .OrderByDescending(log => log.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task LogActivityAsync(int userId, string activityType, string description, string? ipAddress = null)
    {
        var log = new UserActivityLog
        {
            UserId = userId,
            Action = $"{activityType}: {description}",
            IpAddress = ipAddress,
            CreatedAt = DateTime.Now
        };

        await CreateAsync(log);
    }
}
