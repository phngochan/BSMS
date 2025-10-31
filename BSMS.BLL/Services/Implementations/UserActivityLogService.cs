using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;
using Microsoft.Extensions.Logging;

namespace BSMS.BLL.Services.Implementations;

public class UserActivityLogService : IUserActivityLogService
{
    private readonly IUserActivityLogRepository _activityLogRepo;
    private readonly ILogger<UserActivityLogService> _logger;

    public UserActivityLogService(
        IUserActivityLogRepository activityLogRepo,
        ILogger<UserActivityLogService> logger)
    {
        _activityLogRepo = activityLogRepo;
        _logger = logger;
    }

    public async Task LogActivityAsync(int userId, string activityType, string description, string? ipAddress = null)
    {
        try
        {
            await _activityLogRepo.LogActivityAsync(userId, activityType, description, ipAddress);
            _logger.LogInformation("Activity logged: UserId={UserId}, Type={Type}, IP={IpAddress}",
                userId, activityType, ipAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log activity for UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<UserActivityLog>> GetUserActivitiesAsync(int userId, int pageNumber = 1, int pageSize = 50)
    {
        try
        {
            return await _activityLogRepo.GetUserActivitiesAsync(userId, pageNumber, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get activities for UserId: {UserId}", userId);
            throw;
        }
    }

    public async Task<IEnumerable<UserActivityLog>> GetRecentActivitiesAsync(int count = 100)
    {
        try
        {
            return await _activityLogRepo.GetRecentActivitiesAsync(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent activities");
            throw;
        }
    }

    public async Task<IEnumerable<UserActivityLog>> GetRecentActivitiesAsync(int userId, int count)
    {
        try
        {
            var activities = await _activityLogRepo.GetUserActivitiesAsync(userId, pageNumber: 1, pageSize: count);
            return activities;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get recent activities for UserId: {UserId}", userId);
            throw;
        }
    }
}
