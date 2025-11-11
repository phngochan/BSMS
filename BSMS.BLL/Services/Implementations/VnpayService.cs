using BSMS.BLL.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Security.Cryptography;
using System.Text;


public class VnpayService : IVnpayService
{
    private readonly IConfiguration _config;

    public VnpayService(IConfiguration config)
    {
        _config = config;
    }

    private string Sha512(string input)
    {
        using var sha = SHA512.Create();
        byte[] bytes = Encoding.UTF8.GetBytes(input);
        byte[] hash = sha.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }

    // =========================== CREATE PAYMENT URL ===========================
    public string CreatePaymentUrl(int paymentId, decimal amount, HttpContext context)
    {
        var vnp = _config.GetSection("VNPAY");
        string tmn = vnp["TmnCode"];
        string secret = vnp["HashSecret"];
        string returnUrl = vnp["ReturnUrl"];
        string baseUrl = vnp["PaymentUrl"];

        var data = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            { "vnp_Version", "2.1.0" },
            { "vnp_Command", "pay" },
            { "vnp_TmnCode", tmn },
            { "vnp_Amount", ((long)(amount * 100)).ToString() },
            { "vnp_CurrCode", "VND" },
            { "vnp_TxnRef", paymentId.ToString() },
            { "vnp_OrderInfo", "Thanh toan goi thue pin" },
            { "vnp_OrderType", "other" },
            { "vnp_Locale", "vn" },
            { "vnp_ReturnUrl", returnUrl },
            { "vnp_IpAddr", context.Connection.RemoteIpAddress?.ToString()},
            { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") }
        };

        string raw = string.Join("&", data.Select(d => $"{d.Key}={d.Value}"));
        string secureHash = Sha512(secret + raw);

        string query = string.Join("&",
            data.Select(d => $"{d.Key}={WebUtility.UrlEncode(d.Value)}")) +
            $"&vnp_SecureHashType=SHA512&vnp_SecureHash={secureHash}";

        return $"{baseUrl}?{query}";
    }

    // =========================== VERIFY SIGNATURE ===========================
    public bool VerifySignature(IQueryCollection query, string secretKey)
    {
        var sorted = query
            .Where(x => x.Key != "vnp_SecureHash" && x.Key != "vnp_SecureHashType")
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.Value.ToString());

        string raw = string.Join("&", sorted.Select(d => $"{d.Key}={d.Value}"));
        string myHash = Sha512(secretKey + raw);
        string vnpHash = query["vnp_SecureHash"];

        return myHash.Equals(vnpHash, StringComparison.OrdinalIgnoreCase);
    }
}