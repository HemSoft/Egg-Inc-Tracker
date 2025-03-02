namespace HemSoft.EggIncTracker.Domain;

using System;
using System.Text;

public static class Extensions
{
    public static string ToReadableTime(this int seconds)
    {
        if (seconds < 0)
            throw new ArgumentOutOfRangeException(nameof(seconds), "Seconds cannot be negative");

        int days = seconds / (24 * 3600);
        seconds %= 24 * 3600;
        int hours = seconds / 3600;
        seconds %= 3600;
        int minutes = seconds / 60;
        seconds %= 60;

        var parts = new StringBuilder();

        if (days > 0)
            parts.Append($"{days} day{(days > 1 ? "s" : "")} ");

        if (hours > 0)
            parts.Append($"{hours} hour{(hours > 1 ? "s" : "")} ");

        if (minutes > 0)
            parts.Append($"{minutes} minute{(minutes > 1 ? "s" : "")} ");

        if (seconds > 0 || parts.Length == 0) // Always show seconds if no other part
            parts.Append($"{seconds} second{(seconds > 1 ? "s" : "")}");

        return parts.ToString().Trim();
    }

    public static DateTime ToCst(this long unixTimestamp)
    {
        // Unix timestamp is seconds past epoch (UTC)
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime utcDateTime = epoch.AddSeconds(unixTimestamp);

        // Convert to CST (Central Standard Time)
        TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
        DateTime cstDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, cstZone);

        return cstDateTime;
    }
}