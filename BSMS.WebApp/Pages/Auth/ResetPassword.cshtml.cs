using System.ComponentModel.DataAnnotations;
using BSMS.BLL.Services;
using BSMS.WebApp.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace BSMS.WebApp.Pages.Auth;

public class ResetPasswordModel : BasePageModel
{
    public class ResetInput
    {
        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
        [Required]
        [Compare(nameof(Password))]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    [BindProperty]
    public ResetInput Input { get; set; } = new();

    private readonly IUserService _userService;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordModel(IUserService userService, IPasswordHasher passwordHasher, IUserActivityLogService? activity = null) : base(activity)
    {
        _userService = userService;
        _passwordHasher = passwordHasher;
    }

    public IActionResult OnGet()
    {
        if (!HttpContext.Session.IsResetVerified())
            return RedirectToPage("/Auth/ForgotPassword");
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!HttpContext.Session.IsResetVerified())
            return RedirectToPage("/Auth/ForgotPassword");

        if (!ModelState.IsValid)
            return Page();

        var email = HttpContext.Session.GetResetEmail();
        if (string.IsNullOrEmpty(email))
        {
            TempData["ErrorMessage"] = "Session expired. Please request a new code.";
            return RedirectToPage("/Auth/ForgotPassword");
        }

        var newPasswordHash = _passwordHasher.HashPassword(Input.Password);
        await _userService.UpdatePasswordAsync(email, newPasswordHash);

        // cleanup session
        HttpContext.Session.ClearResetVerified();
        HttpContext.Session.ClearResetEmail();

        TempData["SuccessMessage"] = "Password updated. Please login.";
        return RedirectToPage("/Auth/Login");
    }
}
