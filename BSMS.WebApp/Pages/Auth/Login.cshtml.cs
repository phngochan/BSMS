using BSMS.BLL.Services;
using BSMS.BusinessObjects.DTOs.Auth;
using BSMS.BusinessObjects.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BSMS.WebApp.Pages.Auth;

public class LoginModel : BasePageModel
{
    private readonly IAuthService _authService;

    public LoginModel(
        IAuthService authService,
        IUserActivityLogService activityLogService) : base(activityLogService)
    {
        _authService = authService;
    }

    [BindProperty]
    public LoginRequest Input { get; set; } = new();

    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }

    public void OnGet(string? returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        ReturnUrl = returnUrl ?? Url.Content("~/");
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var result = await _authService.LoginAsync(Input);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return Page();
            }

            // Create claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, result.User!.UserId.ToString()),
                new Claim(ClaimTypes.Name, result.User.Username),
                new Claim(ClaimTypes.Email, result.User.Email),
                new Claim(ClaimTypes.Role, result.User.Role.ToString()),
                new Claim("FullName", result.User.FullName)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = Input.RememberMe,
                ExpiresUtc = Input.RememberMe
                    ? DateTimeOffset.UtcNow.AddDays(7)
                    : DateTimeOffset.UtcNow.AddHours(1),
                AllowRefresh = true
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Log activity to database
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            await _activityLogService!.LogActivityAsync(
                result.User.UserId,
                "Login",
                $"User logged in from IP: {ipAddress}",
                ipAddress);

            var roleName = result.User.Role;

            if (returnUrl != "/" && !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            var target = roleName switch
            {
                UserRole.Admin => Url.Page("/Admin/Dashboard")!,
                UserRole.StationStaff => Url.Page("/Staff/Index")!,
                _ => Url.Page("/Index")!
            };
            return Redirect(target);
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "An error occurred during login. Please try again.");
            return Page();
        }
    }
}
