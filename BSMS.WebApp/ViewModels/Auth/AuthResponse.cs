using BSMS.BusinessObjects.DTOs.Auth;

namespace BSMS.WebApp.ViewModels.Auth;

public class AuthResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public UserDto? User { get; set; }
    public string? Token { get; set; }
}

