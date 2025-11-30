// Pages/Driver/RentBattery.cshtml.cs
using BSMS.BLL.Services;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BSMS.WebApp.Pages.Driver
{
    public class RentBatteryModel : PageModel
    {
        private readonly IUserPackageService _pkgService;
        private readonly IPaymentService _paymentService;

        public List<BatteryServicePackage> Packages { get; set; } = new();
        public UserPackage? ActivePackage { get; set; }
        public int UsedSwaps { get; set; } = 0;
        public List<SwapTransaction> RecentSwaps { get; set; } = new();

        public RentBatteryModel(
            IUserPackageService pkgService,
            IPaymentService paymentService)
        {
            _pkgService = pkgService;
            _paymentService = paymentService;
        }

   // Pages/Driver/RentBattery.cshtml.cs
            public async Task OnGetAsync()
            {
                var userId = 1;
                ActivePackage = await _pkgService.GetCurrentPackageAsync(userId); // ← ĐÃ SỬA
                Packages = await _pkgService.GetAvailablePackagesAsync();
            }

        // BỎ VNPAY – THANH TOÁN NGAY
        // Pages/Driver/RentBattery.cshtml.cs
        public async Task<IActionResult> OnPostBuyAsync(int packageId)
        {
            var userId = 1;
            try
            {
                var payment = await _paymentService.CreatePaymentAsync(userId, packageId, PaymentMethod.EWallet);
                await _paymentService.UpdateStatusAsync(payment.PaymentId, PaymentStatus.Paid);
                await _pkgService.CreateUserPackageAsync(userId, packageId, payment.PaymentId);

                TempData["Success"] = "Mua gói thành công!";

                // GỌI LẠI OnGetAsync ĐỂ CẬP NHẬT DỮ LIỆU
                await OnGetAsync();
                return Page();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Lỗi: " + ex.Message;
                await OnGetAsync();
                return Page();
            }
        }

        public async Task<IActionResult> OnPostRenewAsync(int packageId)
        {
            return await OnPostBuyAsync(packageId); // GỌI CHUNG
        }

    }
}