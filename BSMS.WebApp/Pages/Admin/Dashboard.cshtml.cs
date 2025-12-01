using BSMS.BLL.Services;
using BSMS.WebApp.Pages;

namespace BSMS.WebApp.Pages.Admin;

public class DashboardModel : BasePageModel
{
    private readonly IUserService _userService;
    private readonly IChangingStationService _stationService;
    private readonly ISwapTransactionService _swapTransactionService;

    public DashboardModel(
        IUserService userService,
        IChangingStationService stationService,
        ISwapTransactionService swapTransactionService,
        IUserActivityLogService activityLogService) : base(activityLogService)
    {
        _userService = userService;
        _stationService = stationService;
        _swapTransactionService = swapTransactionService;
    }

    public int TotalUsers { get; set; }
    public int TotalStations { get; set; }
    public int DailyTransactions { get; set; }
    public decimal MonthlyRevenue { get; set; }

    public async Task OnGetAsync()
    {
        TotalUsers = await _userService.CountUsersAsync();
        TotalStations = await _stationService.GetStationCountAsync();
        DailyTransactions = await _swapTransactionService.GetDailyTransactionCountAsync(DateTime.UtcNow);
        MonthlyRevenue = await _swapTransactionService.GetCurrentMonthRevenueAsync();
    }
}
