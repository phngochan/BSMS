using BSMS.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace BSMS.WebApp.Pages.Driver;

[Authorize(Roles = "Driver")]
public class StationDetailsModel : PageModel
{
    private readonly IReservationService _reservationService;
    private readonly IStationService _stationService;
    private readonly IBatteryService _batteryService;

    public StationDetailsModel(IReservationService reservationService, IStationService stationService, IBatteryService batteryService)
    {
        _reservationService = reservationService;
        _stationService = stationService;
        _batteryService = batteryService;
    }

    public StationDetailViewModel? Station { get; set; }
    public List<BatteryModelInfo> AvailableBatteryModels { get; set; } = new();

    [TempData]
    public string? ErrorMessage { get; set; }
    
    [TempData]
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var station = await _stationService.GetStationDetailsAsync(id);
        if (station == null) return NotFound();

        // availability via grouped batteries
        var grouped = await _batteryService.GetBatteriesGroupedByModelAsync(id);
        var availableCount = grouped.Sum(g => g.AvailableCount);

        Station = new StationDetailViewModel
        {
            StationId = station.StationId,
            Name = station.Name,
            Address = station.Address,
            Capacity = station.Capacity,
            AvailableBatteries = availableCount,
            Status = station.Status.ToString(),
            Latitude = station.Latitude,
            Longitude = station.Longitude
        };

        AvailableBatteryModels = grouped
            .Where(g => g.AvailableCount > 0)
            .Select(g => new BatteryModelInfo
            {
                Model = g.Model,
                Capacity = g.Capacity,
                Count = g.AvailableCount
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int stationId, DateTime reservationDate, string reservationTime)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            ErrorMessage = "User not found";
            return RedirectToPage(new { id = stationId });
        }

        try
        {
            // Parse time
            if (!TimeSpan.TryParse(reservationTime, out var time))
            {
                ErrorMessage = "Invalid time format";
                return RedirectToPage(new { id = stationId });
            }

            // Combine date and time
            var scheduledTime = reservationDate.Date.Add(time);

            // Validate first
            var (canCreate, errorMessage) = await _reservationService.ValidateReservationAsync(userId, stationId, scheduledTime);
            
            if (!canCreate)
            {
                ErrorMessage = errorMessage;
                return RedirectToPage(new { id = stationId });
            }

            // Create reservation
            var reservation = await _reservationService.CreateReservationAsync(userId, stationId, scheduledTime);

            SuccessMessage = $"Reservation confirmed for {scheduledTime:MMM dd, yyyy HH:mm}!";
            return RedirectToPage("/Driver/MyReservations");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return RedirectToPage(new { id = stationId });
        }
    }

}

public class StationDetailViewModel
{
    public int StationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int AvailableBatteries { get; set; }
    public string Status { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class BatteryModelInfo
{
    public string Model { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int Count { get; set; }
}
