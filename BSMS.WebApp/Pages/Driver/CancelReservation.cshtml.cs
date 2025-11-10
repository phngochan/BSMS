using BSMS.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace BSMS.WebApp.Pages.Driver;

[Authorize(Roles = "Driver")]
public class CancelReservationModel : PageModel
{
    private readonly IReservationService _reservationService;

    public CancelReservationModel(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    [TempData]
    public string? ErrorMessage { get; set; }
    
    [TempData]
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            ErrorMessage = "User not found";
            return RedirectToPage("/Driver/MyReservations");
        }

        try
        {
            var cancelled = await _reservationService.CancelReservationAsync(id, userId);

            if (cancelled)
            {
                SuccessMessage = "Reservation cancelled successfully";
            }
            else
            {
                ErrorMessage = "Failed to cancel reservation. It may not exist, not belong to you, or be too close to the scheduled time.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
        }

        return RedirectToPage("/Driver/MyReservations");
    }
}
