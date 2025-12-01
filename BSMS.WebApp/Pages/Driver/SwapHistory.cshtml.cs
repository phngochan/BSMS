using BSMS.BLL.Services;
using BSMS.WebApp.ViewModels.Driver;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace BSMS.WebApp.Pages.Driver;

[Authorize(Roles = "Driver")]
public class SwapHistoryModel : PageModel
{
    private readonly ISwapTransactionService _swapTransactionService;
    private readonly ILogger<SwapHistoryModel> _logger;

    public SwapHistoryModel(
        ISwapTransactionService swapTransactionService,
        ILogger<SwapHistoryModel> logger)
    {
        _swapTransactionService = swapTransactionService;
        _logger = logger;
    }

    public List<SwapTransactionViewModel> SwapTransactions { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    [BindProperty(SupportsGet = true)]
    public string? FilterStatus { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? FilterStation { get; set; }

    public async Task OnGetAsync(int pageNumber = 1, string? filterStatus = null, string? filterStation = null)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        PageNumber = pageNumber;
        FilterStatus = filterStatus;
        FilterStation = filterStation;

        var allTransactions = await _swapTransactionService.GetUserTransactionsAsync(userId);

        // Apply filters
        var filteredTransactions = allTransactions.AsQueryable();

        if (!string.IsNullOrEmpty(FilterStatus))
        {
            filteredTransactions = filteredTransactions.Where(st => st.Status.ToString() == FilterStatus);
        }

        if (!string.IsNullOrEmpty(FilterStation))
        {
            filteredTransactions = filteredTransactions.Where(st =>
                st.Station != null &&
                (st.Station.Name.Contains(FilterStation, StringComparison.OrdinalIgnoreCase) ||
                 st.Station.Address.Contains(FilterStation, StringComparison.OrdinalIgnoreCase)));
        }

        TotalCount = filteredTransactions.Count();

        var pagedTransactions = filteredTransactions
            .OrderByDescending(st => st.SwapTime)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        SwapTransactions = pagedTransactions.Select(st => new SwapTransactionViewModel
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
        }).ToList();
    }
}

