using BSMS.BLL.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace BSMS.WebApp.Pages;

public class BasePageModel : PageModel
{
    protected readonly IUserActivityLogService? _activityLogService;

    public BasePageModel(IUserActivityLogService? activityLogService = null)
    {
        _activityLogService = activityLogService;
    }

    protected int CurrentUserId => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

    protected string CurrentUsername => User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

    protected string CurrentUserRole => User.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";

    protected string CurrentUserFullName => User.FindFirst("FullName")?.Value ?? "Unknown";

    protected async Task LogActivityAsync(string activityType, string description)
    {
        if (_activityLogService == null || CurrentUserId == 0)
            return;

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _activityLogService.LogActivityAsync(CurrentUserId, activityType, description, ipAddress);
        }
        catch
        {
        }
    }
}
