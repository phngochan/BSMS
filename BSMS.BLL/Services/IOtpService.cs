namespace BSMS.BLL.Services;

/// <summary>
/// Service for OTP generation and validation (stateless - no session management)
/// Session management should be handled by PageModels
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Generate a 6-digit OTP code
    /// </summary>
    string GenerateOtp();

    /// <summary>
    /// Hash OTP code for secure storage
    /// </summary>
    string HashOtp(string code);

    /// <summary>
    /// Validate OTP code against stored hash
    /// </summary>
    bool ValidateOtp(string code, string storedHash);
}
