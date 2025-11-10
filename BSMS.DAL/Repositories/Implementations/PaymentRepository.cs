using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using BSMS.DAL.Base;
using BSMS.DAL.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BSMS.DAL.Repositories.Implementations
{
    public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
    {

        public PaymentRepository(BSMSDbContext context) : base(context)
        {
        }

        public async Task<List<Payment>> GetFilteredAsync(int? userId, DateTime? start, DateTime? end, PaymentStatus? status)
        {
            var query = _context.Payments.Include(p => p.User).AsQueryable();

            if (userId.HasValue) query = query.Where(p => p.UserId == userId.Value);
            if (start.HasValue) query = query.Where(p => p.PaymentTime >= start.Value);
            if (end.HasValue) query = query.Where(p => p.PaymentTime <= end.Value);
            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }
            return await query.OrderByDescending(p => p.PaymentTime).ToListAsync();
        }
    }
}
