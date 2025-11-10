using BSMS.BLL.Services;
using BSMS.BusinessObjects.Enums;
using BSMS.WebApp.ViewModels.Driver;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace BSMS.WebApp.Pages.Driver;

[Authorize(Roles = "Driver")]
public class MyReservationsModel : PageModel
{
    private readonly IReservationService _reservationService;

    public MyReservationsModel(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    public ReservationViewModel? ActiveReservation { get; set; }
    public List<ReservationViewModel> PastReservations { get; set; } = new();
    public bool CanCancel { get; set; }

    public async Task OnGetAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        var activeReservation = await _reservationService.GetActiveReservationAsync(userId);
        if (activeReservation != null)
        {
            ActiveReservation = new ReservationViewModel
            {
                ReservationId = activeReservation.ReservationId,
                StationName = activeReservation.Station?.Name ?? "Unknown Station",
                StationAddress = activeReservation.Station?.Address ?? "",
                StationLatitude = activeReservation.Station?.Latitude ?? 0,
                StationLongitude = activeReservation.Station?.Longitude ?? 0,
                ScheduledTime = activeReservation.ScheduledTime,
                Status = activeReservation.Status.ToString(),
                CreatedAt = activeReservation.CreatedAt
            };

            CanCancel = activeReservation.ScheduledTime > DateTime.UtcNow.AddHours(1);
        }

        var allReservations = await _reservationService.GetMyReservationsAsync(userId, pageNumber: 1, pageSize: 20);
        PastReservations = allReservations
            .Where(r => r.Status != ReservationStatus.Active)
            .Select(r => new ReservationViewModel
            {
                ReservationId = r.ReservationId,
                StationName = r.Station?.Name ?? "Unknown Station",
                StationAddress = r.Station?.Address ?? "",
                ScheduledTime = r.ScheduledTime,
                Status = r.Status.ToString(),
                CreatedAt = r.CreatedAt
            })
            .ToList();
    }
}
