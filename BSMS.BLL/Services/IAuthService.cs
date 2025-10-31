using BSMS.BusinessObjects.DTOs.Auth;

namespace BSMS.BLL.Services;
public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> ValidateTokenAsync(string token);
}
