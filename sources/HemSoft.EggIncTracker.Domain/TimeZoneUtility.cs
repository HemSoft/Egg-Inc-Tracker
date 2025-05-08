namespace HemSoft.EggIncTracker.Domain;

using System;

/// <summary>
/// Utility class for handling time zone conversions
/// </summary>
public static class TimeZoneUtility
{
    /// <summary>
    /// Converts a UTC DateTime to the local time zone of the system
    /// </summary>
    /// <param name="utcTime">The UTC time to convert</param>
    /// <returns>The time in the local time zone</returns>
    public static DateTime ToLocalTime(DateTime utcTime)
    {
        if (utcTime.Kind != DateTimeKind.Utc)
        {
            // Ensure the input is treated as UTC
            utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
        }
        
        return TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.Local);
    }

    /// <summary>
    /// Converts a Unix timestamp to the local time zone of the system
    /// </summary>
    /// <param name="unixTimestamp">The Unix timestamp in seconds</param>
    /// <returns>The time in the local time zone</returns>
    public static DateTime UnixTimestampToLocalTime(long unixTimestamp)
    {
        // Unix timestamp is seconds past epoch (UTC)
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime utcDateTime = epoch.AddSeconds(unixTimestamp);
        
        return ToLocalTime(utcDateTime);
    }

    /// <summary>
    /// Converts a Unix timestamp (as double) to the local time zone of the system
    /// </summary>
    /// <param name="unixTimestamp">The Unix timestamp in seconds (with optional decimal precision)</param>
    /// <returns>The time in the local time zone</returns>
    public static DateTime UnixTimestampToLocalTime(double unixTimestamp)
    {
        return UnixTimestampToLocalTime((long)unixTimestamp);
    }

    /// <summary>
    /// Formats a UTC time for display, showing "Today", "Yesterday", or the date
    /// </summary>
    /// <param name="utcTime">The UTC time to format</param>
    /// <returns>A formatted string representation of the time</returns>
    public static string FormatTimeForDisplay(DateTime? utcTime)
    {
        if (!utcTime.HasValue || utcTime.Value == DateTime.MinValue)
        {
            return string.Empty;
        }

        var localTime = ToLocalTime(utcTime.Value);
        var today = DateTime.Today;

        return localTime.Date == today
            ? $"Today {localTime:HH:mm}"
            : localTime.Date == today.AddDays(-1)
                ? $"Yesterday {localTime:HH:mm}"
                : localTime.ToString("MM/dd HH:mm");
    }
}
