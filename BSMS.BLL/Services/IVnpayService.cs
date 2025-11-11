using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BSMS.BLL.Services
{
    public interface IVnpayService
    {
        string CreatePaymentUrl(int paymentId, decimal amount, HttpContext context);
        bool VerifySignature(IQueryCollection query, string hashSecret);
    }
}
