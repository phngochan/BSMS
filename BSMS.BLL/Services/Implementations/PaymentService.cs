using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSMS.BLL.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _repo;
        public PaymentService(IPaymentRepository repo) => _repo = repo;

        public async Task<List<Payment>> GetFilteredAsync(int? userId, DateTime? start, DateTime? end, string? status) =>
            await _repo.GetFilteredAsync(userId, start, end, PaymentStatus.Pending);

        public async Task<decimal> GetRevenueAsync(DateTime start, DateTime end)
        {
            var payments = await _repo.GetFilteredAsync(null, start, end, PaymentStatus.Paid);
            return payments.Sum(p => p.Amount);
        }
    }
}
