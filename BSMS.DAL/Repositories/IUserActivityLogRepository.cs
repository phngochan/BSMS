using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;

namespace BSMS.DAL.Repositories;

public interface IUserActivityLogRepository : IGenericRepository<UserActivityLog>
{
    Task<IEnumerable<UserActivityLog>> GetUserActivitiesAsync(int userId, int pageNumber = 1, int pageSize = 50);
    Task<IEnumerable<UserActivityLog>> GetRecentActivitiesAsync(int count = 100);
    Task LogActivityAsync(int userId, string activityType, string description, string? ipAddress = null);
}
