using BSMS.BusinessObjects.Models;

namespace BSMS.BLL.Services;

public class AuthResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public User? User { get; set; }
}

