using BSMS.BLL.Services;
using BSMS.BusinessObjects.Enums;
using BSMS.WebApp.ViewModels.Driver;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace BSMS.WebApp.Pages;

[Authorize]
public class ProfileModel : PageModel
{
    private readonly IUserService _userService;
    private readonly ISwapTransactionService _swapTransactionService;
    private readonly ILogger<ProfileModel> _logger;

    public ProfileModel(
        IUserService userService,
        ISwapTransactionService swapTransactionService,
        ILogger<ProfileModel> logger)
    {
        _userService = userService;
        _swapTransactionService = swapTransactionService;
        _logger = logger;
    }

    public ProfileViewModel? Profile { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var vehicles = await _userService.GetUserWithVehiclesAsync(userId);
        var swapTransactions = await _swapTransactionService.GetUserTransactionsAsync(userId);

        Profile = new ProfileViewModel
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Phone = user.Phone,
            Role = user.Role.ToString(),
            CreatedAt = user.CreatedAt,
            Vehicles = vehicles?.Vehicles?.Select(v => new VehicleInfoViewModel
            {
                VehicleId = v.VehicleId,
                Vin = v.Vin,
                BatteryModel = v.BatteryModel,
                BatteryType = v.BatteryType,
                CreatedAt = v.CreatedAt
            }).ToList() ?? new List<VehicleInfoViewModel>(),
            SwapTransactions = swapTransactions.Select(st => new SwapTransactionViewModel
            {
                TransactionId = st.TransactionId,
                SwapTime = st.SwapTime,
                StationName = st.Station?.Name ?? "Chưa xác định",
                StationAddress = st.Station?.Address ?? "",
                VehicleVin = st.Vehicle?.Vin ?? "",
                BatteryTakenModel = st.BatteryTaken?.Model ?? "",
                BatteryTakenSerialNumber = st.BatteryTaken?.BatteryId.ToString() ?? "",
                BatteryReturnedModel = st.BatteryReturned?.Model,
                BatteryReturnedSerialNumber = st.BatteryReturned?.BatteryId.ToString(),
                TotalCost = st.TotalCost,
                Status = st.Status.ToString()
            })
            .OrderByDescending(st => st.SwapTime)
            .ToList(),
            TotalSwaps = swapTransactions.Count(),
            CompletedSwaps = swapTransactions.Count(st => st.Status == SwapStatus.Completed),
            TotalSpent = swapTransactions
                .Where(st => st.Status == SwapStatus.Completed)
                .Sum(st => st.TotalCost)
        };

        return Page();
    }
}

