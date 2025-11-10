namespace BSMS.WebApp.Helpers;

public static class DateTimeExtensions
{
    private const int VietnamTimeZoneOffset = 7;

    public static DateTime ToLocalTime(this DateTime utcDateTime)
    {
        if (utcDateTime.Kind == DateTimeKind.Local)
        {
            return utcDateTime;
        }

        if (utcDateTime.Kind == DateTimeKind.Unspecified)
        {
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }

        return utcDateTime.AddHours(VietnamTimeZoneOffset);
    }

    public static DateTime ToUtcTime(this DateTime localDateTime)
    {
        if (localDateTime.Kind == DateTimeKind.Utc)
        {
            return localDateTime;
        }

        if (localDateTime.Kind == DateTimeKind.Unspecified)
        {
            var utcTime = localDateTime.AddHours(-VietnamTimeZoneOffset);
            return DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
        }

        return localDateTime.ToUniversalTime();
    }

    public static string ToLocalString(this DateTime utcDateTime, string format = "HH:mm dd/MM/yyyy")
    {
        return utcDateTime.ToLocalTime().ToString(format);
    }

    public static string ToLocalDateString(this DateTime utcDateTime)
    {
        return utcDateTime.ToLocalTime().ToString("dd/MM/yyyy");
    }

    public static string ToLocalTimeString(this DateTime utcDateTime)
    {
        return utcDateTime.ToLocalTime().ToString("HH:mm");
    }

    public static string ToLocalDateTimeString(this DateTime utcDateTime)
    {
        return utcDateTime.ToLocalTime().ToString("HH:mm dd/MM/yyyy");
    }
}

