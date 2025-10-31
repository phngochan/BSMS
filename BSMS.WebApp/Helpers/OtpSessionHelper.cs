using System.Text.Json;

namespace BSMS.WebApp.Helpers;

/// <summary>
/// Helper for managing OTP-related session data in PageModels
/// </summary>
public static class OtpSessionHelper
{
    private const string OtpKeyPrefix = "OTP:";
    private const string RegisterDataKey = "OTP:register:data";
    private const string ResetEmailKey = "OTP:reset:email";
    private const string ResetVerifiedKey = "OTP:reset:verified";

    public class OtpData
    {
        public string Email { get; set; } = string.Empty;
        public string CodeHash { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    // ========== OTP Storage ==========
    public static void StoreOtp(this ISession session, string purpose, string email, string codeHash, TimeSpan ttl)
    {
        var data = new OtpData
        {
            Email = email,
            CodeHash = codeHash,
            ExpiresAt = DateTime.UtcNow.Add(ttl)
        };
        session.SetString($"{OtpKeyPrefix}{purpose}", JsonSerializer.Serialize(data));
    }

    public static OtpData? GetOtp(this ISession session, string purpose)
    {
        var json = session.GetString($"{OtpKeyPrefix}{purpose}");
        return string.IsNullOrEmpty(json) ? null : JsonSerializer.Deserialize<OtpData>(json);
    }

    public static void RemoveOtp(this ISession session, string purpose)
    {
        session.Remove($"{OtpKeyPrefix}{purpose}");
    }

    // ========== Registration Data ==========
    public static void SetPendingRegistration<T>(this ISession session, T data)
    {
        session.SetString(RegisterDataKey, JsonSerializer.Serialize(data));
    }

    public static T? GetPendingRegistration<T>(this ISession session)
    {
        var json = session.GetString(RegisterDataKey);
        return string.IsNullOrEmpty(json) ? default : JsonSerializer.Deserialize<T>(json);
    }

    public static void ClearPendingRegistration(this ISession session)
    {
        session.Remove(RegisterDataKey);
    }

    // ========== Password Reset Flow ==========
    public static void SetResetEmail(this ISession session, string email)
    {
        session.SetString(ResetEmailKey, email);
    }

    public static string? GetResetEmail(this ISession session)
    {
        return session.GetString(ResetEmailKey);
    }

    public static void ClearResetEmail(this ISession session)
    {
        session.Remove(ResetEmailKey);
    }

    public static void MarkResetVerified(this ISession session)
    {
        session.SetString(ResetVerifiedKey, "1");
    }

    public static bool IsResetVerified(this ISession session)
    {
        return session.GetString(ResetVerifiedKey) == "1";
    }

    public static void ClearResetVerified(this ISession session)
    {
        session.Remove(ResetVerifiedKey);
    }
}
