// BLL/Services/Implementations/PaymentService.cs
using BSMS.BusinessObjects.DTOs.Admin;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;
using BSMS.DAL.Context;
using BSMS.DAL.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BSMS.BLL.Services.Implementations
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepo;
        private readonly IGenericRepository<BatteryServicePackage> _packageRepo;
        private readonly BSMSDbContext _context;

        public PaymentService(
            IPaymentRepository paymentRepo,
            IGenericRepository<BatteryServicePackage> packageRepo,
            BSMSDbContext context)
        {
            _paymentRepo = paymentRepo;
            _packageRepo = packageRepo;
            _context = context;
        }

        // === CÁC METHOD CŨ (TỪ REPO) ===
        public async Task<List<Payment>> GetFilteredAsync(int? userId, DateTime? start, DateTime? end, PaymentStatus? status)
            => await _paymentRepo.GetFilteredAsync(userId, start, end, status);

        public async Task<decimal> GetRevenueAsync(DateTime start, DateTime end)
            => await _paymentRepo.GetTotalRevenueAsync(start, end);

        public async Task<int> CountActivePackagesAsync()
            => await _paymentRepo.CountActivePackagesAsync();

        public async Task<(List<Payment>, int)> GetPagedAsync(int? userId, DateTime? start, DateTime? end,
            PaymentStatus? status, string? searchName, int pageIndex, int pageSize)
            => await _paymentRepo.GetPagedAsync(userId, start, end, status, searchName, pageIndex, pageSize);

        public async Task<List<RevenueDto>> GetRevenueLast30DaysAsync()
            => await _paymentRepo.GetRevenueLast30DaysAsync();

        // === CREATE PAYMENT (SỬA ĐÚNG) ===
        //public async Task<Payment> CreatePaymentAsync(int userId, int packageId, PaymentMethod method)
        //{
        //    var pkg = await _context.BatteryServicePackages.FindAsync(packageId);
        //    if (pkg == null) throw new ArgumentException("Gói không tồn tại");

        //    var payment = new Payment
        //    {
        //        UserId = userId,
        //        Amount = pkg.Price,
        //        Method = PaymentMethod.EWallet,
        //        Status = PaymentStatus.Pending,
        //        PaymentTime = DateTime.Now,

        //        InvoiceUrl = string.Empty
        //    };

        //    _context.Payments.Add(payment);
        //    await _context.SaveChangesAsync();
        //    return payment;
        //}

        //// === THÊM METHOD CẬP NHẬT TRẠNG THÁI ===
        //public async Task UpdateStatusAsync(int paymentId, PaymentStatus status)
        //{
        //    var payment = await _context.Payments.FindAsync(paymentId);
        //    if (payment != null)
        //    {
        //        payment.Status = status;
        //        await _context.SaveChangesAsync();
        //    }
        //}

        // BLL/Services/Implementations/PaymentService.cs
        public async Task<Payment> CreatePaymentAsync(int userId, int packageId, PaymentMethod method)
        {
            var pkg = await _context.BatteryServicePackages.FindAsync(packageId);
            if (pkg == null) throw new ArgumentException("Gói không tồn tại");

            var payment = new Payment
            {
                UserId = userId,
                Amount = pkg.Price,
                Method = method,
                Status = PaymentStatus.Pending,
                PaymentTime = DateTime.Now,
                InvoiceUrl = string.Empty
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();
            return payment;
        }

        public async Task UpdateStatusAsync(int paymentId, PaymentStatus status)
        {
            var payment = await _context.Payments.FindAsync(paymentId);
            if (payment != null)
            {
                payment.Status = status;
                payment.PaymentTime = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Payment?> GetByIdAsync(int id)
            => await _context.Payments.FindAsync(id);
    }
}