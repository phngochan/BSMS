using BSMS.BLL.Services;
using BSMS.WebApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BSMS.WebApp.Pages.Admin.Vehicles;

[Authorize(Roles = "Admin")]
public class IndexModel : BasePageModel
{
    private readonly IVehicleService _vehicleService;
    private readonly IUserService _userService;

    public IndexModel(
        IVehicleService vehicleService,
        IUserService userService,
        IUserActivityLogService activityLogService) : base(activityLogService)
    {
        _vehicleService = vehicleService;
        _userService = userService;
    }

    public List<VehicleViewModel> Vehicles { get; set; } = new();
    public List<DriverOptionViewModel> Drivers { get; set; } = new();
    public List<string> AllVins { get; set; } = new();
    public List<string> AllBatteryModels { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? FilterUserId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterVin { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterBatteryModel { get; set; }

    [TempData]
    public string? SuccessMessage { get; set; }
    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        // Lấy danh sách tất cả Driver
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

        // Lấy tất cả xe
        var allUsers = await _userService.GetAllUsersAsync();
        var allVehicles = new List<BusinessObjects.Models.Vehicle>();

        foreach (var user in allUsers)
        {
            var userVehicles = await _vehicleService.GetUserVehiclesAsync(user.UserId);
            allVehicles.AddRange(userVehicles);
        }

        // Apply filters
        var filteredVehicles = allVehicles.AsQueryable();

        if (FilterUserId.HasValue && FilterUserId.Value > 0)
        {
            filteredVehicles = filteredVehicles.Where(v => v.UserId == FilterUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(FilterVin))
        {
            filteredVehicles = filteredVehicles.Where(v => v.Vin.Contains(FilterVin, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(FilterBatteryModel))
        {
            filteredVehicles = filteredVehicles.Where(v => v.BatteryModel.Contains(FilterBatteryModel, StringComparison.OrdinalIgnoreCase));
        }

        var userNamesDict = allUsers.ToDictionary(u => u.UserId, u => u.FullName);

        // Map to ViewModels
        Vehicles = filteredVehicles
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new VehicleViewModel
            {
                VehicleId = v.VehicleId,
                UserId = v.UserId,
                UserName = userNamesDict.ContainsKey(v.UserId) ? userNamesDict[v.UserId] : "N/A",
                Vin = v.Vin,
                BatteryModel = v.BatteryModel,
                BatteryType = v.BatteryType,
                CreatedAt = v.CreatedAt
            })
            .ToList();

        // Lấy danh sách VIN và BatteryModel để autocomplete
        AllVins = allVehicles.Select(v => v.Vin).Distinct().OrderBy(v => v).ToList();
        AllBatteryModels = allVehicles.Select(v => v.BatteryModel).Distinct().OrderBy(v => v).ToList();
    }
}
