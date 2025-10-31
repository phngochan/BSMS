using BSMS.BLL.Services;
using BSMS.WebApp.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BSMS.WebApp.Pages.Auth;

public class ForgotPasswordModel : BasePageModel
{
    [BindProperty]
    public ForgotPasswordInput Input { get; set; } = null!;

    public bool EmailSent { get; set; } = false;

    private readonly IUserService _userService;
    private readonly IEmailService _emailService;
    private readonly IOtpService _otpService;

    public ForgotPasswordModel(
        IUserService userService,
        IEmailService emailService,
        IOtpService otpService,
        IUserActivityLogService? activityLogService = null)
        : base(activityLogService)
    {
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

        // Check if email exists (silent on failure for privacy)
        var user = await _userService.GetUserByEmailAsync(Input.Email);
        if (user != null)
        {
            var otp = _otpService.GenerateOtp();
            var otpHash = _otpService.HashOtp(otp);
            
            // Store in session (PageModel responsibility)
            HttpContext.Session.StoreOtp("reset", Input.Email, otpHash, TimeSpan.FromMinutes(5));
            HttpContext.Session.SetResetEmail(Input.Email);

            var html = $@"
                <h2>BSMS Password Reset OTP</h2>
                <p>Your OTP code is:</p>
                <div style='font-size:24px;font-weight:700;letter-spacing:4px;color:#00C853'>{otp}</div>
                <p>This code will expire in 5 minutes.</p>";
            await _emailService.SendEmailAsync(Input.Email, "[BSMS] Password Reset Code", html);

            await LogActivityAsync("ForgotPassword", $"OTP sent for password reset: {Input.Email}");

            return RedirectToPage("/Auth/VerifyOtp", new { purpose = "reset" });
        }

        // Still show generic success to avoid user enumeration
        EmailSent = true;
        TempData["SuccessMessage"] = "If an account exists with this email, you will receive further instructions.";
        return Page();
    }

    public class ForgotPasswordInput
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
    }
}
