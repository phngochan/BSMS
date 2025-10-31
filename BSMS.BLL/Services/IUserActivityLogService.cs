using BSMS.BusinessObjects.Models;

namespace BSMS.BLL.Services;

public interface IUserActivityLogService
{
    Task LogActivityAsync(int userId, string activityType, string description, string? ipAddress = null);
    Task<IEnumerable<UserActivityLog>> GetUserActivitiesAsync(int userId, int pageNumber = 1, int pageSize = 50);
    Task<IEnumerable<UserActivityLog>> GetRecentActivitiesAsync(int count = 100);
    Task<IEnumerable<UserActivityLog>> GetRecentActivitiesAsync(int userId, int count);
}
