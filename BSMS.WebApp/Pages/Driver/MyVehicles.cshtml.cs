using BSMS.BLL.Services;
using BSMS.WebApp.ViewModels.Driver;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace BSMS.WebApp.Pages.Driver;

[Authorize(Roles = "Driver")]
public class MyVehiclesModel : PageModel
{
    private readonly IUserService _userService;
    private readonly ILogger<MyVehiclesModel> _logger;

    public MyVehiclesModel(
        IUserService userService,
        ILogger<MyVehiclesModel> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public List<VehicleInfoViewModel> Vehicles { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        var user = await _userService.GetUserWithVehiclesAsync(userId);
        if (user?.Vehicles != null)
        {
            Vehicles = user.Vehicles
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => new VehicleInfoViewModel
                {
                    VehicleId = v.VehicleId,
                    Vin = v.Vin,
                    BatteryModel = v.BatteryModel,
                    BatteryType = v.BatteryType,
                    CreatedAt = v.CreatedAt
                })
                .ToList();
        }
    }
}

