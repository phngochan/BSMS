using BSMS.BLL.Services;
using BSMS.WebApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using BSMS.WebApp.Hubs;

namespace BSMS.WebApp.Pages.Admin.Vehicles;

[Authorize(Roles = "Admin")]
public class EditModel : BasePageModel
{
    private readonly IVehicleService _vehicleService;
    private readonly IUserService _userService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<EditModel> _logger;

    public EditModel(
        IVehicleService vehicleService,
        IUserService userService,
        IHubContext<NotificationHub> hubContext,
        ILogger<EditModel> logger,
        IUserActivityLogService activityLogService) : base(activityLogService)
    {
        _vehicleService = vehicleService;
        _userService = userService;
        _hubContext = hubContext;
        _logger = logger;
    }

    [BindProperty]
    public VehicleEditViewModel Input { get; set; } = new();

    public VehicleDeleteViewModel? Vehicle { get; set; }
    public List<DriverOptionViewModel> Drivers { get; set; } = new();

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
        var allDrivers = await _userService.GetUsersByRoleAsync(BusinessObjects.Enums.UserRole.Driver);
        Drivers = allDrivers
            .OrderBy(u => u.FullName)
            .Select(u => new DriverOptionViewModel
            {
                UserId = u.UserId,
                FullName = u.FullName,
                Email = u.Email
            })
            .ToList();

        Vehicle = new VehicleDeleteViewModel
        {
            UserId = vehicle.UserId,
            VehicleId = vehicle.VehicleId,
            Vin = vehicle.Vin,
            UserName = user?.FullName ?? "N/A",
            BatteryModel = vehicle.BatteryModel,
            BatteryType = vehicle.BatteryType,
            CreatedAt = vehicle.CreatedAt
        };

        Input.VehicleId = vehicle.VehicleId;
        Input.UserId = vehicle.UserId;
        Input.Vin = vehicle.Vin;
        Input.BatteryModel = vehicle.BatteryModel;
        Input.BatteryType = vehicle.BatteryType;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id, 0);
            if (vehicle != null)
            {
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
            }
            var allDrivers = await _userService.GetUsersByRoleAsync(BusinessObjects.Enums.UserRole.Driver);
            Drivers = allDrivers
                .OrderBy(u => u.FullName)
                .Select(u => new DriverOptionViewModel
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email
                })
                .ToList();
            return Page();
        }

        try
        {
            if (await _vehicleService.IsVinExistsAsync(Input.Vin, id))
            {
                ModelState.AddModelError("Input.Vin", "VIN này đã tồn tại trong hệ thống");
                var vehicle = await _vehicleService.GetVehicleByIdAsync(id, 0);
                if (vehicle != null)
                {
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
                }
                var allDrivers = await _userService.GetUsersByRoleAsync(BusinessObjects.Enums.UserRole.Driver);
                Drivers = allDrivers
                    .OrderBy(u => u.FullName)
                    .Select(u => new DriverOptionViewModel
                    {
                        UserId = u.UserId,
                        FullName = u.FullName,
                        Email = u.Email
                    })
                    .ToList();
                return Page();
            }

            // Lấy xe hiện tại để kiểm tra
            var currentVehicle = await _vehicleService.GetVehicleByIdAsync(id, 0);
            if (currentVehicle == null)
            {
                TempData["ErrorMessage"] = "Xe không tồn tại.";
                return RedirectToPage("Index");
            }

            // Nếu đổi chủ xe, cần xóa xe cũ và tạo mới (hoặc update trực tiếp nếu cho phép)
            if (currentVehicle.UserId != Input.UserId)
            {
                // Xóa xe cũ
                await _vehicleService.DeleteVehicleAsync(id, currentVehicle.UserId);
                // Tạo xe mới cho chủ xe mới
                var newVehicle = await _vehicleService.CreateVehicleAsync(
                    Input.UserId,
                    Input.Vin,
                    Input.BatteryModel,
                    Input.BatteryType);

                var newUser = await _userService.GetUserByIdAsync(Input.UserId);
                var oldUser = await _userService.GetUserByIdAsync(currentVehicle.UserId);
                await LogActivityAsync("UPDATE_VEHICLE",
                    $"Đã chuyển xe: VIN {Input.Vin} từ user {currentVehicle.UserId} sang {newUser?.FullName ?? "N/A"}");

                try
                {
                    // Gửi notification cho chủ xe cũ
                    await _hubContext.Clients.User(currentVehicle.UserId.ToString()).SendAsync("ReceiveNotification", new
                    {
                        message = $"Xe với VIN {Input.Vin} đã được chuyển khỏi tài khoản của bạn",
                        type = "warning",
                        timestamp = DateTime.UtcNow
                    });

                    // Gửi notification cho chủ xe mới
                    await _hubContext.Clients.User(Input.UserId.ToString()).SendAsync("ReceiveNotification", new
                    {
                        message = $"Xe với VIN {Input.Vin} đã được thêm vào tài khoản của bạn",
                        type = "success",
                        timestamp = DateTime.UtcNow
                    });

                    // Gửi notification cho Admin
                    await _hubContext.Clients.Group("Admin").SendAsync("ReceiveNotification", new
                    {
                        message = $"Đã chuyển xe: VIN {Input.Vin} từ {oldUser?.FullName ?? "N/A"} sang {newUser?.FullName ?? "N/A"}",
                        type = "info",
                        timestamp = DateTime.UtcNow
                    });

                    _logger.LogInformation("SignalR notification sent for transferred vehicle {VehicleId}", newVehicle.VehicleId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send SignalR notification for transferred vehicle {VehicleId}", newVehicle.VehicleId);
                }
            }
            else
            {
                // Chỉ cập nhật thông tin
                var vehicle = await _vehicleService.UpdateVehicleAsync(
                    id,
                    currentVehicle.UserId,
                    Input.Vin,
                    Input.BatteryModel,
                    Input.BatteryType);

                await LogActivityAsync("UPDATE_VEHICLE",
                    $"Đã cập nhật xe: VIN {vehicle.Vin}");

                try
                {
                    // Gửi notification cho chủ xe
                    await _hubContext.Clients.User(Input.UserId.ToString()).SendAsync("ReceiveNotification", new
                    {
                        message = $"Thông tin xe với VIN {vehicle.Vin} đã được cập nhật",
                        type = "info",
                        timestamp = DateTime.UtcNow
                    });

                    // Gửi notification cho Admin
                    await _hubContext.Clients.Group("Admin").SendAsync("ReceiveNotification", new
                    {
                        message = $"Đã cập nhật xe: VIN {vehicle.Vin}",
                        type = "info",
                        timestamp = DateTime.UtcNow
                    });

                    _logger.LogInformation("SignalR notification sent for updated vehicle {VehicleId}", vehicle.VehicleId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send SignalR notification for updated vehicle {VehicleId}", vehicle.VehicleId);
                }
            }

            TempData["SuccessMessage"] = "Cập nhật xe thành công!";
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var vehicle = await _vehicleService.GetVehicleByIdAsync(id, 0);
            if (vehicle != null)
            {
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
            }
            var allDrivers = await _userService.GetUsersByRoleAsync(BusinessObjects.Enums.UserRole.Driver);
            Drivers = allDrivers
                .OrderBy(u => u.FullName)
                .Select(u => new DriverOptionViewModel
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email
                })
                .ToList();
            return Page();
        }
    }
}
