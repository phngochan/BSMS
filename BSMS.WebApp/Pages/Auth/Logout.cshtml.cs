using BSMS.BLL.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BSMS.WebApp.Pages.Auth;

[Authorize]
public class LogoutModel : BasePageModel
{
    public LogoutModel(IUserActivityLogService activityLogService) : base(activityLogService)
    {
    }

    public async Task<IActionResult> OnGetAsync()
    {
        return await LogoutUserAsync();
    }

    private async Task<IActionResult> LogoutUserAsync()
    {
        if (User.Identity?.IsAuthenticated ?? false)
        {
            // Log activity before signing out
            await LogActivityAsync("Logout", $"User logged out from IP: {HttpContext.Connection.RemoteIpAddress}");

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        return RedirectToPage("/Auth/Login");
    }
}
