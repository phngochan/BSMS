using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSMS.WebApp.Pages.Admin;

public class DashboardModel : PageModel
{
    public int TotalUsers { get; set; }
    public int TotalStations { get; set; }
    public int DailyTransactions { get; set; }
    public decimal MonthlyRevenue { get; set; }

    public void OnGet()
    {
        // Mock values; replace with real service layer calls later
        TotalUsers = 1245;
        TotalStations = 18;
        DailyTransactions = 162;
        MonthlyRevenue = 12850m;
    }
}
