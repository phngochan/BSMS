using BSMS.BLL.Services;
using BSMS.WebApp.Hubs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BSMS.WebApp.Pages.Driver;

[Authorize(Roles = "Driver")]
public class CancelReservationModel : BasePageModel
{
    private readonly IReservationService _reservationService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<CancelReservationModel> _logger;

    public CancelReservationModel(
        IReservationService reservationService,
        IHubContext<NotificationHub> hubContext,
        ILogger<CancelReservationModel> logger,
        IUserActivityLogService activityLogService) : base(activityLogService)
    {
        _reservationService = reservationService;
        _hubContext = hubContext;
        _logger = logger;
    }

    [TempData]
    public string? ErrorMessage { get; set; }
    
    [TempData]
    public string? SuccessMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (CurrentUserId == 0)
        {
            ErrorMessage = "User not found";
            return RedirectToPage("/Driver/MyReservations");
        }

        try
        {
            var reservation = await _reservationService.GetReservationDetailsAsync(id);
            var cancelled = await _reservationService.CancelReservationAsync(id, CurrentUserId);

            if (cancelled)
            {
                await LogActivityAsync("CANCEL_RESERVATION", 
                    $"Đã hủy đặt chỗ #{id} tại trạm {reservation?.Station?.Name ?? "N/A"}");

                try
                {
                    // Gửi notification cho driver
                    await _hubContext.Clients.User(CurrentUserId.ToString()).SendAsync("ReceiveNotification", new
                    {
                        message = $"Đã hủy đặt chỗ #{id} tại trạm {reservation?.Station?.Name ?? "N/A"}",
                        type = "info",
                        timestamp = DateTime.UtcNow
                    });

                    // Gửi notification cho nhân viên trạm
                    if (reservation?.StationId != null)
                    {
                        await _hubContext.Clients.Group($"Station_{reservation.StationId}").SendAsync("ReceiveNotification", new
                        {
                            message = $"Đặt chỗ #{id} đã được hủy bởi khách hàng",
                            type = "warning",
                            timestamp = DateTime.UtcNow
                        });
                    }

                    _logger.LogInformation("SignalR notification sent for cancelled reservation {ReservationId} by user {UserId}", id, CurrentUserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send SignalR notification for cancelled reservation {ReservationId}", id);
                }

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
