using BSMS.BLL.Services;
using BSMS.BusinessObjects.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace BSMS.WebApp.Pages.Staff;

[Authorize(Roles = "StationStaff,Admin")]
public class IndexModel : PageModel
{
    private readonly IBatteryService _batteryService;
    private readonly IUserService _userService;

    public IndexModel(IBatteryService batteryService, IUserService userService)
    {
        _batteryService = batteryService;
        _userService = userService;
    }

    public int FullCount { get; set; }
    public int ChargingCount { get; set; }
    public int MaintenanceCount { get; set; }
    public int BookedCount { get; set; }

    public List<BatteryRow> Batteries { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        var user = await _userService.GetUserWithVehiclesAsync(userId);
        var staffStation = user?.StationStaffs?.FirstOrDefault();

        if (staffStation?.StationId == null)
        {
            return;
        }

        var batteries = await _batteryService.GetBatteriesByStationAsync(staffStation.StationId);

        Batteries = batteries.Select(b => new BatteryRow(
            $"BAT-{b.BatteryId:D3}",
            b.Model,
            $"{b.Capacity} kWh",
            (int)Math.Round(b.Soh),
            b.Status.ToString(),
            b.UpdatedAt
        )).ToList();

        FullCount = Batteries.Count(b => b.Status == BatteryStatus.Full.ToString());
        ChargingCount = Batteries.Count(b => b.Status == BatteryStatus.Charging.ToString());
        MaintenanceCount = Batteries.Count(b => b.Status == BatteryStatus.Defective.ToString());
        BookedCount = Batteries.Count(b => b.Status == BatteryStatus.Booked.ToString());
    }
}

public record BatteryRow(
    string BatteryId,
    string Model,
    string Capacity,
    int Soh,
    string Status,
    DateTime LastUpdated
);
