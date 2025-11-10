using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
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
    }
}
