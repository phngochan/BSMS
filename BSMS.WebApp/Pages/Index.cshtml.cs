using BSMS.BusinessObjects.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace BSMS.WebApp.Pages;
public class IndexModel : PageModel
{
    public IActionResult OnGet()
    {
        if (!User.Identity?.IsAuthenticated ?? true)
        {
            return RedirectToPage("/Auth/Login");
        }

        var roleString = User.FindFirst(ClaimTypes.Role)?.Value;

        if (!Enum.TryParse<UserRole>(roleString, out var role))
        {
            return RedirectToPage("/Auth/Login");
        }

        // Redirect theo enum
        return role switch
        {
            UserRole.Admin => RedirectToPage("/Admin/Dashboard"),
            UserRole.StationStaff => RedirectToPage("/Staff/Index"),
            UserRole.Driver => RedirectToPage("/Driver/Index"),
            _ => RedirectToPage("/Auth/Login")
        };
    }
}
