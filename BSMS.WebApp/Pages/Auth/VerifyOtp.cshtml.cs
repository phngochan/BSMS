using BSMS.BLL.Services;
using BSMS.BusinessObjects.DTOs.Auth;
using BSMS.WebApp.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BSMS.WebApp.Pages.Auth;

public class VerifyOtpModel : BasePageModel
{
    [BindProperty]
    [Required, StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public string Purpose { get; set; } = string.Empty; // "register" or "reset"

    private readonly IOtpService _otpService;
    private readonly IAuthService _authService;

    public VerifyOtpModel(IOtpService otpService, IAuthService authService, IUserActivityLogService? activity = null) : base(activity)
    {
        _otpService = otpService;
        _authService = authService;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        if (Purpose == "register")
        {
            var pending = HttpContext.Session.GetPendingRegistration<RegisterRequest>();
            if (pending == null)
            {
                ModelState.AddModelError(string.Empty, "Registration session expired. Please register again.");
                return RedirectToPage("/Auth/Register");
            }

            // Validate OTP
            var otpData = HttpContext.Session.GetOtp("register");
            if (otpData == null || DateTime.UtcNow > otpData.ExpiresAt)
            {
                ModelState.AddModelError(string.Empty, "OTP expired. Please request a new code.");
                return Page();
            }

            if (!otpData.Email.Equals(pending.Email, StringComparison.OrdinalIgnoreCase) ||
                !_otpService.ValidateOtp(Code, otpData.CodeHash))
            {
                ModelState.AddModelError(string.Empty, "Invalid OTP code.");
                return Page();
            }

            // Consume OTP
            HttpContext.Session.RemoveOtp("register");

            // Proceed to register
            var result = await _authService.RegisterAsync(pending);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return RedirectToPage("/Auth/Register");
            }

            HttpContext.Session.ClearPendingRegistration();
            TempData["SuccessMessage"] = "Email verified. Registration completed. Please login.";
            return RedirectToPage("/Auth/Login");
        }
        else if (Purpose == "reset")
        {
            var email = HttpContext.Session.GetResetEmail();
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "Session expired. Please request a new code.");
                return RedirectToPage("/Auth/ForgotPassword");
            }

            // Validate OTP
            var otpData = HttpContext.Session.GetOtp("reset");
            if (otpData == null || DateTime.UtcNow > otpData.ExpiresAt)
            {
                ModelState.AddModelError(string.Empty, "OTP expired. Please request a new code.");
                return Page();
            }

            if (!otpData.Email.Equals(email, StringComparison.OrdinalIgnoreCase) ||
                !_otpService.ValidateOtp(Code, otpData.CodeHash))
            {
                ModelState.AddModelError(string.Empty, "Invalid OTP code.");
                return Page();
            }

            // Consume OTP
            HttpContext.Session.RemoveOtp("reset");
            HttpContext.Session.MarkResetVerified();

            return RedirectToPage("/Auth/ResetPassword");
        }

        ModelState.AddModelError(string.Empty, "Unknown verification purpose.");
        return Page();
    }
}
