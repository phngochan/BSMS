using BSMS.BLL.Services;
using BSMS.BusinessObjects.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BSMS.WebApp.Pages.Shared.Components.StaffAlertBell;

public class StaffAlertBellViewComponent : ViewComponent
{
    private readonly IAlertService _alertService;

    public StaffAlertBellViewComponent(IAlertService alertService)
    {
        _alertService = alertService;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        // Only show for authenticated Staff/Admin
        if (!UserClaimsPrincipal.Identity?.IsAuthenticated ?? true)
        {
            return Content(string.Empty);
        }

        var role = UserClaimsPrincipal.FindFirst(ClaimTypes.Role)?.Value;

        // Only show alert bell for Staff and Admin
        if (role != UserRole.Admin.ToString() && role != UserRole.StationStaff.ToString())
        {
            return Content(string.Empty);
        }

        var userIdClaim = UserClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Content(string.Empty);
        }

        // Get unresolved alerts for monitoring
        var alerts = await _alertService.GetAlertsForUserAsync(userId, role);
        var unreadCount = alerts.Count();

        ViewBag.UnreadCount = unreadCount;
        return View(alerts);
    }
}
