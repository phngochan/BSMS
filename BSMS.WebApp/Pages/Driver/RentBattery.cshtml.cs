using BSMS.BLL.Services;
using BSMS.BusinessObjects.Enums;
using BSMS.BusinessObjects.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using BSMS.WebApp.Hubs;


namespace BSMS.WebApp.Pages.Driver
{
    public class RentBatteryModel : PageModel
    {
        private readonly IUserPackageService _pkgService;
        private readonly IPaymentService _paymentService;
        private readonly IHubContext<PaymentHub> _hubContext;

        public List<BatteryServicePackage> Packages { get; set; } = new();
        public List<UserPackage> PurchasedPackages { get; set; } = new();

        public RentBatteryModel(IUserPackageService pkgService, IPaymentService paymentService, IHubContext<PaymentHub> hubContext)
        {
            _pkgService = pkgService;
            _paymentService = paymentService;
            _hubContext = hubContext;
        }

        public async Task OnGetAsync()
        {
            var userId = GetUserId() ?? 2;
            Packages = await _pkgService.GetAvailablePackagesAsync();
            PurchasedPackages = await _pkgService.GetUserPackagesAsync(userId);
        }

        public async Task<IActionResult> OnPostBuyAsync(int packageId)
        {
            var userId = GetUserId() ?? 2;
            try
            {
                var payment = await _paymentService.CreatePaymentAsync(userId, packageId, PaymentMethod.EWallet);
                await _paymentService.UpdateStatusAsync(payment.PaymentId, PaymentStatus.Paid);
                await _pkgService.CreateUserPackageAsync(userId, packageId, payment.PaymentId);

                // Gửi SignalR cho Admin
                await _hubContext.Clients.All.SendAsync("PaymentUpdated", new
                {
                    paymentId = payment.PaymentId,
                    userId = userId,
                    amount = payment.Amount,
                    time = payment.PaymentTime.ToString("yyyy-MM-dd HH:mm:ss")
                });

                TempData["Success"] = "Mua gói thành công!";
            }
            catch
            {
                TempData["Success"] = "Có lỗi xảy ra khi mua gói!";
            }
            return RedirectToPage();
        }


        public async Task<IActionResult> OnPostRenewAsync(int packageId)
            => await OnPostBuyAsync(packageId);

        private int? GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub");
            return int.TryParse(claim?.Value, out int id) ? id : null;
        }
    }
}