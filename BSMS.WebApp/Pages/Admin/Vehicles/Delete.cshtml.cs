using BSMS.BLL.Services;
using BSMS.WebApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using BSMS.WebApp.Hubs;

namespace BSMS.WebApp.Pages.Admin.Vehicles;

[Authorize(Roles = "Admin")]
public class DeleteModel : BasePageModel
{
    private readonly IVehicleService _vehicleService;
    private readonly IUserService _userService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<DeleteModel> _logger;

    public DeleteModel(
        IVehicleService vehicleService,
        IUserService userService,
        IHubContext<NotificationHub> hubContext,
        ILogger<DeleteModel> logger,
        IUserActivityLogService activityLogService) : base(activityLogService)
    {
        _vehicleService = vehicleService;
        _userService = userService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public VehicleDeleteViewModel? Vehicle { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }
    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var vehicle = await _vehicleService.GetVehicleByIdAsync(id, 0); // Admin có thể xem tất cả
        if (vehicle == null)
        {
            TempData["ErrorMessage"] = "Xe không tồn tại.";
            return RedirectToPage("Index");
        }

        var user = await _userService.GetUserByIdAsync(vehicle.UserId);
        Vehicle = new VehicleDeleteViewModel
        {
            VehicleId = vehicle.VehicleId,
            Vin = vehicle.Vin,
            UserName = user?.FullName ?? "N/A",
            BatteryModel = vehicle.BatteryModel,
            BatteryType = vehicle.BatteryType,
            CreatedAt = vehicle.CreatedAt
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var vehicle = await _vehicleService.GetVehicleByIdAsync(id, 0);
        if (vehicle == null)
        {
            TempData["ErrorMessage"] = "Xe không tồn tại.";
            return RedirectToPage("Index");
        }

        try
        {
            var deleted = await _vehicleService.DeleteVehicleAsync(id, vehicle.UserId);
            if (deleted)
            {
                var user = await _userService.GetUserByIdAsync(vehicle.UserId);
                await LogActivityAsync("DELETE_VEHICLE",
                    $"Đã xóa xe: VIN {vehicle.Vin} của user {vehicle.UserId}");

                try
                {
                    // Gửi notification cho chủ xe
                    await _hubContext.Clients.User(vehicle.UserId.ToString()).SendAsync("ReceiveNotification", new
                    {
                        message = $"Xe với VIN {vehicle.Vin} đã được xóa khỏi tài khoản của bạn",
                        type = "warning",
                        timestamp = DateTime.UtcNow
                    });

                    // Gửi notification cho Admin
                    await _hubContext.Clients.Group("Admin").SendAsync("ReceiveNotification", new
                    {
                        message = $"Đã xóa xe: VIN {vehicle.Vin} của {user?.FullName ?? "N/A"}",
                        type = "info",
                        timestamp = DateTime.UtcNow
                    });

                    _logger.LogInformation("SignalR notification sent for deleted vehicle {VehicleId} for user {UserId}", id, vehicle.UserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send SignalR notification for deleted vehicle {VehicleId}", id);
                }

                TempData["SuccessMessage"] = "Xóa xe thành công!";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể xóa xe. Vui lòng thử lại.";
            }
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = ex.Message;
        }

        return RedirectToPage("Index");
    }
}
