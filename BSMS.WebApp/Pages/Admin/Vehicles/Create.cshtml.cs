using BSMS.BLL.Services;
using BSMS.WebApp.Hubs;
using BSMS.WebApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace BSMS.WebApp.Pages.Admin.Vehicles;

[Authorize(Roles = "Admin")]
public class CreateModel : BasePageModel
{
    private readonly IVehicleService _vehicleService;
    private readonly IUserService _userService;
    private readonly ILogger<CreateModel> _logger;
    private readonly IHubContext<NotificationHub> _hubContext;

    public CreateModel(
        IVehicleService vehicleService,
        IUserService userService,
        IUserActivityLogService activityLogService,
        IHubContext<NotificationHub> hubContext,
        ILogger<CreateModel> logger) : base(activityLogService)
    {
        _vehicleService = vehicleService;
        _userService = userService;
        _logger = logger;
        _hubContext = hubContext;
    }

    [BindProperty]
    public VehicleCreateViewModel Input { get; set; } = new();

    public List<DriverOptionViewModel> Drivers { get; set; } = new();

    [TempData]
    public string? SuccessMessage { get; set; }
    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
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
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
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
            if (await _vehicleService.IsVinExistsAsync(Input.Vin))
            {
                ModelState.AddModelError("Input.Vin", "VIN này đã tồn tại trong hệ thống");
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

            var vehicle = await _vehicleService.CreateVehicleAsync(
                Input.UserId,
                Input.Vin,
                Input.BatteryModel,
                Input.BatteryType);

            var user = await _userService.GetUserByIdAsync(Input.UserId);
            await LogActivityAsync("CREATE_VEHICLE",
                $"Đã thêm xe mới: VIN {vehicle.Vin} cho {user?.FullName ?? "N/A"}");

            try
            {
                // Gửi notification cho chủ xe (driver)
                await _hubContext.Clients.User(Input.UserId.ToString()).SendAsync("ReceiveNotification", new
                {
                    message = $"Xe mới với VIN {vehicle.Vin} đã được thêm vào tài khoản của bạn",
                    type = "success",
                    timestamp = DateTime.UtcNow
                });

                // Gửi notification cho Admin
                await _hubContext.Clients.Group("Admin").SendAsync("ReceiveNotification", new
                {
                    message = $"Đã thêm xe mới: VIN {vehicle.Vin} cho {user?.FullName ?? "N/A"}",
                    type = "info",
                    timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("SignalR notification sent for created vehicle {VehicleId} to user {UserId}", vehicle.VehicleId, Input.UserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send SignalR notification for created vehicle {VehicleId}", vehicle.VehicleId);
            }

            TempData["SuccessMessage"] = "Thêm xe thành công!";
            return RedirectToPage("Index");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
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
