using BSMS.BLL.Services;
using BSMS.BusinessObjects.Enums;
using BSMS.WebApp.ViewModels.Driver;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace BSMS.WebApp.Pages.Driver;

[Authorize(Roles = "Driver")]
public class ReservationDetailsModel : PageModel
{
    private readonly IReservationService _reservationService;
    private readonly ILogger<ReservationDetailsModel> _logger;

    public ReservationDetailsModel(
        IReservationService reservationService,
        ILogger<ReservationDetailsModel> logger)
    {
        _reservationService = reservationService;
        _logger = logger;
    }

    public ReservationDetailViewModel? Reservation { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var reservation = await _reservationService.GetReservationDetailsAsync(id);
        if (reservation == null)
        {
            return NotFound();
        }

        if (reservation.UserId != userId)
        {
            return Forbid();
        }

        Reservation = new ReservationDetailViewModel
        {
            ReservationId = reservation.ReservationId,
            StationName = reservation.Station?.Name ?? "Chưa xác định",
            StationAddress = reservation.Station?.Address ?? "",
            StationLatitude = reservation.Station?.Latitude ?? 0,
            StationLongitude = reservation.Station?.Longitude ?? 0,
            ScheduledTime = reservation.ScheduledTime,
            Status = reservation.Status.ToString(),
            CreatedAt = reservation.CreatedAt,
            VehicleVin = reservation.Vehicle?.Vin ?? "",
            BatteryModel = reservation.Vehicle?.BatteryModel ?? "",
            BatteryType = reservation.Vehicle?.BatteryType ?? "",
            BatteryId = reservation.BatteryId,
            CanCancel = reservation.Status == ReservationStatus.Active &&
                       reservation.ScheduledTime > DateTime.UtcNow.AddHours(1)
        };

        return Page();
    }
}

