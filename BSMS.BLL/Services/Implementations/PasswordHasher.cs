using System.Security.Cryptography;
using System.Text;

namespace BSMS.BLL.Services.Implementations;
public class PasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        var hashOfInput = HashPassword(password);
        return hashOfInput == hashedPassword;
    }
}
