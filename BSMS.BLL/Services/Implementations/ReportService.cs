using BSMS.BusinessObjects.Enums; // <- Bạn đã có dòng này, tốt!
using BSMS.DAL.Repositories;
using OfficeOpenXml; // <- THÊM DÒNG NÀY (Sau khi cài đặt NuGet)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSMS.BLL.Services.Implementations
{
    public class ReportService : IReportService
    {
        private readonly IPaymentRepository _paymentRepo;
        public ReportService(IPaymentRepository paymentRepo) => _paymentRepo = paymentRepo;

        public async Task<List<(DateTime Date, decimal Revenue)>> GetDailyRevenueAsync(DateTime start, DateTime end)
        {
            var payments = await _paymentRepo.GetFilteredAsync(null, start, end, PaymentStatus.Paid);

            return payments
                .GroupBy(p => p.PaymentTime.Date)
                .Select(g => (g.Key, g.Sum(p => p.Amount)))
                .OrderBy(x => x.Item1)
                .ToList();
        }

        public async Task<byte[]> ExportRevenueToExcelAsync(DateTime start, DateTime end)
        {
            var payments = await _paymentRepo.GetFilteredAsync(null, start, end, PaymentStatus.Paid);


            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("DoanhThu");

            ws.Cells[1, 1].Value = "Ngày";
            ws.Cells[1, 2].Value = "Số tiền";
            ws.Cells[1, 3].Value = "Phương thức";
            ws.Cells[1, 4].Value = "User";

            int row = 2;
            foreach (var p in payments)
            {
                ws.Cells[row, 1].Value = p.PaymentTime.ToString("dd/MM/yyyy");
                ws.Cells[row, 2].Value = (double)p.Amount;
                ws.Cells[row, 3].Value = p.Method;
                ws.Cells[row, 4].Value = p.User?.FullName ?? "N/A";
                row++;
            }

            ws.Cells.AutoFitColumns();
            return await package.GetAsByteArrayAsync();
        }
    }
}