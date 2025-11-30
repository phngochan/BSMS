// Pages/Payment/VnpayReturn.cshtml.cs
using BSMS.BLL.Services;
using BSMS.BusinessObjects.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace BSMS.WebApp.Pages.Driver
{
    public class VnpayReturnModel : PageModel
    {
        private readonly IPaymentService _paymentService;
        private readonly IVnpayService _vnpay;
        private readonly IConfiguration _config;
        private readonly ILogger<VnpayReturnModel> _logger;

        public string Message { get; set; } = "";
        public bool IsSuccess { get; set; } = false;

        public VnpayReturnModel(
            IPaymentService paymentService,
            IVnpayService vnpay,
            IConfiguration config,
            ILogger<VnpayReturnModel> logger)
        {
            _paymentService = paymentService;
            _vnpay = vnpay;
            _config = config;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                _logger.LogInformation("========== VNPAY RETURN START ==========");

                // Lấy toàn bộ query string
                var queryCollection = HttpContext.Request.Query;

                // Log tất cả params
                _logger.LogInformation("Query Parameters Received:");
                foreach (var param in queryCollection)
                {
                    _logger.LogInformation($"  {param.Key} = {param.Value}");
                }

                // Verify signature
                var vnp = _config.GetSection("VNPAY");
                var hashSecret = vnp["HashSecret"];

                _logger.LogInformation($"HashSecret from config: {hashSecret}");

                if (!_vnpay.VerifySignature(queryCollection, hashSecret))
                {
                    _logger.LogError("Signature verification FAILED!");
                    Message = "Chữ ký không hợp lệ! Giao dịch có thể bị giả mạo.";
                    IsSuccess = false;
                    return Page();
                }

                _logger.LogInformation("Signature verification SUCCESS!");

                // Get payment info
                var responseCode = queryCollection["vnp_ResponseCode"].ToString();
                var txnRef = queryCollection["vnp_TxnRef"].ToString();
                var amount = queryCollection["vnp_Amount"].ToString();
                var transactionNo = queryCollection["vnp_TransactionNo"].ToString();

                _logger.LogInformation($"Response Code: {responseCode}");
                _logger.LogInformation($"TxnRef: {txnRef}");
                _logger.LogInformation($"Amount: {amount}");
                _logger.LogInformation($"TransactionNo: {transactionNo}");

                if (responseCode == "00")
                {
                    if (int.TryParse(txnRef, out int paymentId))
                    {
                        _logger.LogInformation($"Updating payment {paymentId} to Paid status");
                        await _paymentService.UpdateStatusAsync(paymentId, PaymentStatus.Paid);

                        IsSuccess = true;
                        Message = $"✅ Thanh toán thành công! Mã giao dịch: {transactionNo}. Gói đã được kích hoạt.";
                        _logger.LogInformation("Payment updated successfully!");
                    }
                    else
                    {
                        _logger.LogError($"Invalid TxnRef: {txnRef}");
                        Message = "❌ Lỗi: Không thể xử lý mã thanh toán.";
                        IsSuccess = false;
                    }
                }
                else
                {
                    _logger.LogWarning($"Payment failed with response code: {responseCode}");
                    IsSuccess = false;
                    Message = GetErrorMessage(responseCode);
                }

                _logger.LogInformation("========== VNPAY RETURN END ==========");
                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception in VnpayReturn: {ex.Message}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");
                Message = "❌ Có lỗi xảy ra khi xử lý thanh toán.";
                IsSuccess = false;
                return Page();
            }
        }

        private string GetErrorMessage(string responseCode)
        {
            return responseCode switch
            {
                "07" => "❌ Trừ tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường).",
                "09" => "❌ Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng chưa đăng ký dịch vụ InternetBanking tại ngân hàng.",
                "10" => "❌ Giao dịch không thành công do: Khách hàng xác thực thông tin thẻ/tài khoản không đúng quá 3 lần.",
                "11" => "❌ Giao dịch không thành công do: Đã hết hạn chờ thanh toán. Xin quý khách vui lòng thực hiện lại giao dịch.",
                "12" => "❌ Giao dịch không thành công do: Thẻ/Tài khoản của khách hàng bị khóa.",
                "13" => "❌ Giao dịch không thành công do Quý khách nhập sai mật khẩu xác thực giao dịch (OTP).",
                "24" => "❌ Giao dịch không thành công do: Khách hàng hủy giao dịch.",
                "51" => "❌ Giao dịch không thành công do: Tài khoản của quý khách không đủ số dư để thực hiện giao dịch.",
                "65" => "❌ Giao dịch không thành công do: Tài khoản của Quý khách đã vượt quá hạn mức giao dịch trong ngày.",
                "75" => "❌ Ngân hàng thanh toán đang bảo trì.",
                "79" => "❌ Giao dịch không thành công do: KH nhập sai mật khẩu thanh toán quá số lần quy định.",
                _ => $"❌ Thanh toán thất bại. Mã lỗi: {responseCode}"
            };
        }
    }
}