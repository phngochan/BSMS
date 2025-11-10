using BSMS.BusinessObjects.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSMS.BLL.Services
{
    public interface IPaymentService
    {
        Task<List<Payment>> GetFilteredAsync(int? userId, DateTime? start, DateTime? end, string? status);
        Task<decimal> GetRevenueAsync(DateTime start, DateTime end);
    }
}
