using BSMS.BusinessObjects.DTOs.Admin;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSMS.DAL.Repositories
{
    public interface IPaymentRepository
    {
        Task<List<Payment>> GetFilteredAsync(int? userId, DateTime? start, DateTime? end, PaymentStatus? status);
        Task<(List<Payment>, int)> GetPagedAsync(int? userId, DateTime? start, DateTime? end,
            PaymentStatus? status, string? searchName, int pageIndex, int pageSize);
        Task<List<RevenueDto>> GetRevenueLast30DaysAsync();
        Task<decimal> GetTotalRevenueAsync(DateTime from, DateTime to);
        Task<int> CountActivePackagesAsync();
        Task<List<Payment>> FilterAdvancedAsync(int? userId, DateTime? start, DateTime? end, PaymentStatus? status, PaymentMethod? method, decimal? minAmount, decimal? maxAmount);
    }
}
