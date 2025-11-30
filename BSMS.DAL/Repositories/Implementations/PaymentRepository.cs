// DAL/Repositories/Implementations/PaymentRepository.cs
using BSMS.BusinessObjects.DTOs.Admin;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;
using BSMS.DAL.Context;
using BSMS.DAL.Repositories;
using Microsoft.EntityFrameworkCore;

public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
{
    private readonly BSMSDbContext _context;
    public PaymentRepository(BSMSDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<List<Payment>> GetFilteredAsync(int? userId, DateTime? start, DateTime? end, PaymentStatus? status)
    {
        var query = _context.Payments.Include(p => p.User).AsQueryable();
        if (userId.HasValue) query = query.Where(p => p.UserId == userId.Value);
        if (start.HasValue) query = query.Where(p => p.PaymentTime >= start.Value);
        if (end.HasValue) query = query.Where(p => p.PaymentTime <= end.Value.AddDays(1));
        if (status.HasValue) query = query.Where(p => p.Status == status.Value);
        return await query.OrderByDescending(p => p.PaymentTime).ToListAsync();
    }

    public async Task<(List<Payment>, int)> GetPagedAsync(int? userId, DateTime? start, DateTime? end,
        PaymentStatus? status, string? searchName, int pageIndex, int pageSize)
    {
        var query = _context.Payments.Include(p => p.User).AsQueryable();

        if (userId.HasValue) query = query.Where(p => p.UserId == userId.Value);
        if (start.HasValue) query = query.Where(p => p.PaymentTime >= start.Value);
        if (end.HasValue) query = query.Where(p => p.PaymentTime <= end.Value.AddDays(1));
        if (status.HasValue) query = query.Where(p => p.Status == status.Value);
        if (!string.IsNullOrEmpty(searchName))
        {
            query = query.Where(p => p.User.FullName.Contains(searchName) ||
                                    p.UserId.ToString() == searchName);
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.PaymentId)
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<List<RevenueDto>> GetRevenueLast30DaysAsync()
    {
        var start = DateTime.Today.AddDays(-29);
        var end = DateTime.Today;

        return await _context.Payments
            .Where(p => p.PaymentTime >= start && p.PaymentTime <= end && p.Status == PaymentStatus.Paid)
            .GroupBy(p => p.PaymentTime.Date)
            .Select(g => new RevenueDto
            {
                Date = g.Key,
                Total = g.Sum(p => p.Amount)
            })
            .OrderBy(x => x.Date)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalRevenueAsync(DateTime from, DateTime to)
    {
        return await _context.Payments
            .Where(p => p.Status == PaymentStatus.Paid && p.PaymentTime >= from && p.PaymentTime <= to)
            .SumAsync(p => p.Amount);
    }

    public async Task<int> CountActivePackagesAsync()
    {
        return await _context.UserPackages
            .CountAsync(up => up.Status == PackageStatus.Active);
    }

    public async Task<List<Payment>> FilterAdvancedAsync(
    int? userId, DateTime? start, DateTime? end,
    PaymentStatus? status, PaymentMethod? method,
    decimal? minAmount, decimal? maxAmount)
    {
        var q = _context.Payments.Include(p => p.User).AsQueryable();

        if (userId != null)
            q = q.Where(p => p.UserId == userId);

        if (start != null)
            q = q.Where(p => p.PaymentTime >= start);

        if (end != null)
            q = q.Where(p => p.PaymentTime <= end);

        if (status != null)
            q = q.Where(p => p.Status == status);

        if (method != null)
            q = q.Where(p => p.Method == method);

        if (minAmount != null)
            q = q.Where(p => p.Amount >= minAmount);

        if (maxAmount != null)
            q = q.Where(p => p.Amount <= maxAmount);

        return await q.OrderByDescending(p => p.PaymentTime).ToListAsync();
    }

}