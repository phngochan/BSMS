// Pages/Admin/Payments/Index.cshtml.cs
using BSMS.BLL.Services;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.WebApp.Extensions; // THÊM
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ClosedXML.Excel;
using System.IO;

using PaymentModel = BSMS.BusinessObjects.Models.Payment; // ALIAS ĐỂ TRÁNH XUNG ĐỘT

namespace BSMS.WebApp.Pages.Admin.Payments
{
    public class IndexModel : PageModel
    {
        private readonly IPaymentService _paymentService;
        private readonly IWebHostEnvironment _env;

        [BindProperty(SupportsGet = true)] public int? UserId { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Start { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? End { get; set; }
        [BindProperty(SupportsGet = true)] public PaymentStatus? Status { get; set; }
        [BindProperty(SupportsGet = true)] public string? SearchName { get; set; }
        [BindProperty(SupportsGet = true)] public int PageIndex { get; set; } = 1;
        public int PageSize => 10;

        public List<PaymentModel> Payments { get; set; } = new(); // DÙNG ALIAS
        public int TotalCount { get; set; }
        public List<string> RevenueLabels { get; set; } = new();
        public List<decimal> RevenueData { get; set; } = new();

        public IndexModel(IPaymentService paymentService, IWebHostEnvironment env)
        {
            _paymentService = paymentService;
            _env = env;
        }

        public async Task<IActionResult> OnGetAsync(string? export)
        {
            if (export == "excel")
            {
                var allPayments = await _paymentService.GetFilteredAsync(UserId, Start, End, Status);
                if (!string.IsNullOrEmpty(SearchName))
                {
                    allPayments = allPayments
                        .Where(p => p.User?.FullName.Contains(SearchName, StringComparison.OrdinalIgnoreCase) == true ||
                                    p.UserId.ToString() == SearchName)
                        .ToList();
                }
                var excelBytes = GenerateExcel(allPayments);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                           $"GiaoDich_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            }

            var revenue = await _paymentService.GetRevenueLast30DaysAsync();
            RevenueLabels = revenue.Select(x => x.Date.ToString("dd/MM")).ToList();
            RevenueData = revenue.Select(x => x.Total).ToList();

            var (payments, total) = await _paymentService.GetPagedAsync(
                UserId, Start, End, Status, SearchName, PageIndex, PageSize);

            Payments = payments; // OK
            TotalCount = total;

            return Page();
        }

        private byte[] GenerateExcel(List<PaymentModel> payments)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("GiaoDich");

            var headers = new[] { "ID", "User", "Số tiền", "Phương thức", "Thời gian", "Trạng thái", "Hóa đơn" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(1, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            for (int i = 0; i < payments.Count; i++)
            {
                var p = payments[i];
                ws.Cell(i + 2, 1).Value = p.PaymentId;
                ws.Cell(i + 2, 2).Value = p.User?.FullName ?? "User " + p.UserId;
                ws.Cell(i + 2, 3).Value = p.Amount;
                ws.Cell(i + 2, 3).Style.NumberFormat.Format = "#,##0";
                ws.Cell(i + 2, 4).Value = p.Method.GetDisplayName(); // Done
                ws.Cell(i + 2, 5).Value = p.PaymentTime.ToString("dd/MM/yyyy HH:mm");
                ws.Cell(i + 2, 6).Value = p.Status.GetDisplayName();  // Done
                ws.Cell(i + 2, 7).Value = p.InvoiceUrl ?? "—";
            }

            ws.Columns().AdjustToContents();
            ws.Column(3).Width = 15;
            ws.Column(5).Width = 18;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}