using BSMS.BLL.Services;
using BSMS.WebApp.Helpers;
using BSMS.WebApp.ViewModels.Driver;
using BSMS.WebApp.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BSMS.WebApp.Pages.Driver;

[Authorize(Roles = "Driver")]
public class StationDetailsModel : BasePageModel
{
    private readonly IReservationService _reservationService;
    private readonly IStationService _stationService;
    private readonly IBatteryService _batteryService;
    private readonly IUserService _userService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<StationDetailsModel> _logger;

    public StationDetailsModel(
        IReservationService reservationService, 
        IStationService stationService, 
        IBatteryService batteryService,
        IUserService userService,
        IHubContext<NotificationHub> hubContext,
        ILogger<StationDetailsModel> logger,
        IUserActivityLogService activityLogService) : base(activityLogService)
    {
        _reservationService = reservationService;
        _stationService = stationService;
        _batteryService = batteryService;
        _userService = userService;
        _hubContext = hubContext;
        _logger = logger;
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

        if (CurrentUserId > 0)
        {
            var user = await _userService.GetUserWithVehiclesAsync(CurrentUserId);
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
        if (CurrentUserId == 0)
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

            var station = await _stationService.GetStationDetailsAsync(stationId);
            var (canCreate, errorMessage) = await _reservationService.ValidateReservationAsync(CurrentUserId, vehicleId, stationId, scheduledTimeUtc);
            
            if (!canCreate)
            {
                ErrorMessage = errorMessage;
                return RedirectToPage(new { id = stationId });
            }

            var reservation = await _reservationService.CreateReservationAsync(CurrentUserId, vehicleId, stationId, scheduledTimeUtc);

            await LogActivityAsync("CREATE_RESERVATION", 
                $"Đã tạo đặt chỗ #{reservation.ReservationId} tại trạm {station?.Name ?? "N/A"} cho {scheduledTimeLocal:dd/MM/yyyy HH:mm}");

            try
            {
                // Gửi notification cho driver
                await _hubContext.Clients.User(CurrentUserId.ToString()).SendAsync("ReceiveNotification", new
                {
                    message = $"Đã tạo đặt chỗ #{reservation.ReservationId} tại trạm {station?.Name ?? "N/A"} cho {scheduledTimeLocal:dd/MM/yyyy HH:mm}",
                    type = "success",
                    timestamp = DateTime.UtcNow
                });

                // Gửi notification cho nhân viên trạm
                await _hubContext.Clients.Group($"Station_{stationId}").SendAsync("ReceiveNotification", new
                {
                    message = $"Có đặt chỗ mới #{reservation.ReservationId} tại trạm {station?.Name ?? "N/A"} cho {scheduledTimeLocal:dd/MM/yyyy HH:mm}",
                    type = "info",
                    timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("SignalR notification sent for created reservation {ReservationId} by user {UserId}", reservation.ReservationId, CurrentUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SignalR notification for created reservation {ReservationId}", reservation.ReservationId);
            }

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
