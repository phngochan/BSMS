using BSMS.BLL.Services;
using BSMS.WebApp.Helpers;
using BSMS.WebApp.ViewModels.Driver;
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
    private readonly IUserService _userService;

    public StationDetailsModel(
        IReservationService reservationService, 
        IStationService stationService, 
        IBatteryService batteryService,
        IUserService userService)
    {
        _reservationService = reservationService;
        _stationService = stationService;
        _batteryService = batteryService;
        _userService = userService;
    }

    public StationDetailViewModel? Station { get; set; }
    public List<BatteryModelInfo> AvailableBatteryModels { get; set; } = new();
    public List<BusinessObjects.Models.Vehicle> UserVehicles { get; set; } = new();

    [TempData]
    public string? ErrorMessage { get; set; }
    
    [TempData]
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var station = await _stationService.GetStationDetailsAsync(id);
        if (station == null) return NotFound();

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            var user = await _userService.GetUserWithVehiclesAsync(userId);
            if (user?.Vehicles != null)
            {
                UserVehicles = user.Vehicles.ToList();
            }
        }

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

    public async Task<IActionResult> OnPostAsync(int stationId, int vehicleId, DateTime reservationDate, string reservationTime)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            ErrorMessage = "Không tìm thấy người dùng";
            return RedirectToPage(new { id = stationId });
        }

        if (vehicleId <= 0)
        {
            ErrorMessage = "Vui lòng chọn xe";
            return RedirectToPage(new { id = stationId });
        }

        try
        {
            if (!TimeSpan.TryParse(reservationTime, out var time))
            {
                ErrorMessage = "Định dạng giờ không hợp lệ";
                return RedirectToPage(new { id = stationId });
            }

            var scheduledTimeLocal = reservationDate.Date.Add(time);
            var scheduledTimeUtc = scheduledTimeLocal.ToUtcTime();

            var (canCreate, errorMessage) = await _reservationService.ValidateReservationAsync(userId, vehicleId, stationId, scheduledTimeUtc);
            
            if (!canCreate)
            {
                ErrorMessage = errorMessage;
                return RedirectToPage(new { id = stationId });
            }

            var reservation = await _reservationService.CreateReservationAsync(userId, vehicleId, stationId, scheduledTimeUtc);

            SuccessMessage = $"Đặt chỗ thành công cho {scheduledTimeLocal:dd/MM/yyyy HH:mm}!";
            return RedirectToPage("/Driver/MyReservations");
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return RedirectToPage(new { id = stationId });
        }
    }

}
