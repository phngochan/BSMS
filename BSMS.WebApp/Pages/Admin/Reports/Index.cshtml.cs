using BSMS.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BSMS.WebApp.Pages.Admin.Reports
{
    public class IndexModel : PageModel
    {
        private readonly IReportService _reportService;
        private readonly IPaymentService _paymentService;

        [BindProperty] public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-30);
        [BindProperty] public DateTime EndDate { get; set; } = DateTime.Today;

        public decimal TotalRevenue { get; set; }
        public List<(DateTime Date, decimal Revenue)> DailyData { get; set; } = new();

        public IndexModel(IReportService reportService, IPaymentService paymentService)
        {
            _reportService = reportService;
            _paymentService = paymentService;
        }

        public async Task OnGetAsync()
        {
            TotalRevenue = await _paymentService.GetRevenueAsync(StartDate, EndDate);
            DailyData = await _reportService.GetDailyRevenueAsync(StartDate, EndDate);
        }

        public async Task<IActionResult> OnPostExportAsync()
        {
            var file = await _reportService.ExportRevenueToExcelAsync(StartDate, EndDate);
            return File(file, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                       $"DoanhThu_{StartDate:yyyyMMdd}_{EndDate:yyyyMMdd}.xlsx");
        }
    }
}
