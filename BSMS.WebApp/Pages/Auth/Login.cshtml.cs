using BSMS.BLL.Services;
using BSMS.BusinessObjects.Enums;
using BSMS.WebApp.Mappers;
using BSMS.WebApp.ViewModels.Auth;
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
            var result = await _authService.LoginAsync(Input.Username, Input.Password);

            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return Page();
            }

            var user = result.User!;

            // Create claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("FullName", user.FullName)
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
                user.UserId,
                "Login",
                $"User logged in from IP: {ipAddress}",
                ipAddress);

            var roleName = user.Role;

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
