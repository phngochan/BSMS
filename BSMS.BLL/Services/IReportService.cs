using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSMS.BLL.Services
{
    public interface IReportService
    {
        Task<byte[]> ExportRevenueToExcelAsync(DateTime start, DateTime end);
        Task<List<(DateTime Date, decimal Revenue)>> GetDailyRevenueAsync(DateTime start, DateTime end);
    }
}
