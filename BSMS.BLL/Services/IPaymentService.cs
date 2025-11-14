using BSMS.BusinessObjects.DTOs.Admin;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSMS.BLL.Services
{
    // BLL/Services/IPaymentService.cs
    public interface IPaymentService
    {
        Task<List<Payment>> GetFilteredAsync(int? userId, DateTime? start, DateTime? end, PaymentStatus? status);
        Task<decimal> GetRevenueAsync(DateTime start, DateTime end);
        Task<int> CountActivePackagesAsync();
        Task<(List<Payment>, int)> GetPagedAsync(int? userId, DateTime? start, DateTime? end,
            PaymentStatus? status, string? searchName, int pageIndex, int pageSize);
        Task<List<RevenueDto>> GetRevenueLast30DaysAsync();

        Task<Payment> CreatePaymentAsync(int userId, int packageId, PaymentMethod method);
        Task<Payment?> GetByIdAsync(int id);
        Task UpdateStatusAsync(int paymentId, PaymentStatus status);
        Task UpdatePaymentAsync(Payment payment);
        Task<Payment> CreateCustomPaymentAsync(int userId, decimal amount, PaymentMethod method, PaymentStatus status, string invoiceReference);
    }
}
