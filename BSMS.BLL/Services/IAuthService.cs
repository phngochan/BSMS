using BSMS.BusinessObjects.Models;

namespace BSMS.BLL.Services;
public interface IAuthService
{
    Task<AuthResult> LoginAsync(string username, string password);
    Task<AuthResult> RegisterAsync(string username, string email, string fullName, string phone, string password);
    Task<AuthResult> ValidateTokenAsync(string token);
}
