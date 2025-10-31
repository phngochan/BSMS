using BSMS.BLL.Services;
using BSMS.BusinessObjects.DTOs.Auth;
using BSMS.WebApp.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace BSMS.WebApp.Pages.Auth;

public class RegisterModel : BasePageModel
{
    private readonly IAuthService _authService;
    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private readonly IOtpService _otpService;

    [BindProperty]
    public RegisterRequest Input { get; set; } = null!;

    public RegisterModel(
        IAuthService authService,
        IUserService userService,
        IEmailService emailService,
        IOtpService otpService,
        IUserActivityLogService? activityLogService = null)
        : base(activityLogService)
    {
        _authService = authService;
        _userService = userService;
        _emailService = emailService;
        _otpService = otpService;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Pre-validate username/email uniqueness
        if (await _userService.IsUsernameExistsAsync(Input.Username))
        {
            ModelState.AddModelError("Input.Username", "Username already exists");
            return Page();
        }
        if (await _userService.IsEmailExistsAsync(Input.Email))
        {
            ModelState.AddModelError("Input.Email", "Email already exists");
            return Page();
        }

        // Generate OTP and send email
        var otp = _otpService.GenerateOtp();
        var otpHash = _otpService.HashOtp(otp);

        // Store OTP hash in session (handled by PageModel, not service)
        HttpContext.Session.StoreOtp("register", Input.Email, otpHash, TimeSpan.FromMinutes(5));
        HttpContext.Session.SetPendingRegistration(Input);

        var html = $@"
            <h2>BSMS Registration OTP</h2>
            <p>Your OTP code is:</p>
            <div style='font-size:24px;font-weight:700;letter-spacing:4px;color:#00C853'>{otp}</div>
            <p>This code will expire in 5 minutes.</p>";
        await _emailService.SendEmailAsync(Input.Email, "[BSMS] Verify your email", html);

        TempData["SuccessMessage"] = "We have sent a 6-digit code to your email. Please enter it to verify.";
        return RedirectToPage("/Auth/VerifyOtp", new { purpose = "register" });
    }
}
