using System.Security.Cryptography;
using System.Text;

namespace BSMS.BLL.Services.Implementations;
public class OtpService : IOtpService
{
    public string GenerateOtp()
    {
        return Random.Shared.Next(100000, 999999).ToString();
    }

    public string HashOtp(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return Convert.ToBase64String(bytes);
    }

    public bool ValidateOtp(string code, string storedHash)
    {
        return HashOtp(code) == storedHash;
    }
}
