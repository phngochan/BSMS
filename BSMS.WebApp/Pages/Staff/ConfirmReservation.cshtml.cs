using BSMS.BLL.Services;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.WebApp.Helpers;
using BSMS.WebApp.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace BSMS.WebApp.Pages.Staff;

[Authorize(Roles = "StationStaff,Admin")]
public class ConfirmReservationModel : BasePageModel
{
    private readonly IReservationService _reservationService;
    private readonly IUserService _userService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<ConfirmReservationModel> _logger;

    public ConfirmReservationModel(
        IReservationService reservationService,
        IUserService userService,
        IHubContext<NotificationHub> hubContext,
        ILogger<ConfirmReservationModel> logger,
        IUserActivityLogService activityLogService) : base(activityLogService)
    {
        _reservationService = reservationService;
        _userService = userService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public Reservation? Reservation { get; set; }
    public List<Reservation> PendingReservations { get; set; } = new();
    public int? StationId { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            ErrorMessage = "Không tìm thấy người dùng";
            return Page();
        }

        var user = await _userService.GetUserWithVehiclesAsync(userId);
        var staffStation = user?.StationStaffs?.FirstOrDefault();
        StationId = staffStation?.StationId;

        if (StationId.HasValue)
        {
            var today = DateTime.UtcNow.Date;
            var reservations = await _reservationService.GetStationReservationsAsync(StationId.Value, today);
            PendingReservations = reservations
                .Where(r => r.Status == ReservationStatus.Active)
                .OrderBy(r => r.ScheduledTime)
                .ToList();
        }

        if (id.HasValue)
        {
            Reservation = await _reservationService.GetReservationDetailsAsync(id.Value);

            if (Reservation == null)
            {
                ErrorMessage = "Không tìm thấy đặt chỗ.";
                return Page();
            }

            if (Reservation.Status != ReservationStatus.Active)
            {
                ErrorMessage = $"Đặt chỗ không còn hoạt động. Trạng thái hiện tại: {Reservation.Status}";
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostCancelAsync(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var staffUserId))
        {
            ErrorMessage = "Không tìm thấy người dùng";
            return RedirectToPage(new { id });
        }

        var reservation = await _reservationService.GetReservationDetailsAsync(id);
        if (reservation == null)
        {
            ErrorMessage = "Không tìm thấy đặt chỗ";
            return RedirectToPage(new { id });
        }

        var result = await _reservationService.CancelReservationByStaffAsync(id);

        if (result)
        {
            await LogActivityAsync("CANCEL_RESERVATION_BY_STAFF", 
                $"Nhân viên đã hủy đặt chỗ #{id} của khách hàng {reservation.User?.FullName} (UserId: {reservation.UserId}) tại trạm {reservation.Station?.Name ?? "N/A"}");

            await _hubContext.Clients.User(reservation.UserId.ToString()).SendAsync("ReceiveNotification", new
            {
                message = $"Đặt chỗ tại {reservation.Station?.Name ?? "trạm"} đã bị hủy bởi nhân viên",
                type = "warning",
                timestamp = DateTime.UtcNow
            });

            SuccessMessage = "Đã hủy đặt chỗ thành công!";
        }
        else
        {
            ErrorMessage = "Không thể hủy đặt chỗ. Vui lòng kiểm tra lại trạng thái đặt chỗ.";
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCompleteAsync(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var staffUserId))
        {
            ErrorMessage = "Không tìm thấy người dùng";
            return RedirectToPage(new { id });
        }

        var reservation = await _reservationService.GetReservationDetailsAsync(id);
        if (reservation == null)
        {
            ErrorMessage = "Không tìm thấy đặt chỗ";
            return RedirectToPage(new { id });
        }

        var userId = reservation.UserId;
        var stationId = reservation.StationId;
        var stationName = reservation.Station?.Name ?? "trạm";

        var result = await _reservationService.CompleteReservationAsync(id);

        if (result)
        {
            await LogActivityAsync("COMPLETE_RESERVATION", 
                $"Nhân viên đã hoàn thành đặt chỗ #{id} của khách hàng {reservation.User?.FullName} (UserId: {userId}) tại trạm {stationName}");

            try
            {
                await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
                {
                    message = $"Đặt chỗ tại {stationName} đã hoàn thành",
                    type = "success",
                    timestamp = DateTime.UtcNow
                });

                await _hubContext.Clients.Group($"Station_{stationId}").SendAsync("ReceiveNotification", new
                {
                    message = $"Đặt chỗ #{id} đã hoàn thành",
                    type = "info",
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SignalR notification for completed reservation {ReservationId}", id);
            }

            SuccessMessage = "Đã hoàn thành đặt chỗ thành công!";
        }
        else
        {
            ErrorMessage = "Không thể hoàn thành đặt chỗ. Vui lòng kiểm tra lại trạng thái đặt chỗ.";
        }

        return RedirectToPage(new { id });
    }
}

