namespace HemSoft.EggIncTracker.Data;

using System.Globalization;
using System.Numerics;
using System.Text;

public static class Utils
{
    public static DateTime ConvertUnixTimestampToCST(long unixTimestamp)
    {
        // Unix timestamp is seconds past epoch (UTC)
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime utcDateTime = epoch.AddSeconds(unixTimestamp);

        // Convert to CST (Central Standard Time)
        TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
        DateTime cstDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, cstZone);

        return cstDateTime;
    }

    public static TimeSpan GetTimeRemaining(double targetUnixTimestamp)
    {
        // Get current Unix timestamp with millisecond precision
        DateTimeOffset now = DateTimeOffset.UtcNow;
        double currentTimestamp = now.ToUnixTimeSeconds() + (now.Millisecond / 1000.0);

        // Calculate seconds remaining
        double secondsRemaining = targetUnixTimestamp - currentTimestamp;

        return TimeSpan.FromSeconds(secondsRemaining);
    }

    /// <summary>
    /// Gets the target date in Central Standard Time for a given Unix timestamp.
    /// </summary>
    /// <param name="unixTimestamp">The Unix timestamp in seconds (with optional decimal precision)</param>
    /// <returns>A DateTimeOffset representing the target time in CST</returns>
    public static DateTimeOffset GetTargetDateCST(double unixTimestamp)
    {
        return DateTimeOffset.FromUnixTimeSeconds((long)unixTimestamp)
            .ToOffset(TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time")
                .GetUtcOffset(DateTime.UtcNow));
    }

    public static string DoubleToReadableTime(double secondsDouble)
    {
        var seconds = (int) secondsDouble;

        if (seconds < 0)
            seconds = 0;

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

    public static string FormatBigInteger(string bigInteger, bool truncateFractions = false, bool truncateZeroFractions = false)
    {
        BigInteger bi;

        // First try direct BigInteger parsing
        if (BigInteger.TryParse(bigInteger, NumberStyles.Any, CultureInfo.InvariantCulture, out bi))
        {
            // Proceed with existing BigInteger
        }
        // If that fails, try decimal parsing for scientific notation
        else if (decimal.TryParse(bigInteger, NumberStyles.Float, CultureInfo.InvariantCulture, out decimal decimalValue))
        {
            // Convert decimal to BigInteger, handling any decimal places
            bi = (BigInteger)(decimalValue * 1000000M); // Multiply by 10^6 to preserve some decimal places
            bi /= 1000000; // Divide back to get the integer part
        }
        else
        {
            throw new ArgumentException("Invalid number format", nameof(bigInteger));
        }

        string[] suffixes = { "", "K", "M", "B", "T", "q", "Q", "s", "S", "o", "N", "d", "U" };
        BigInteger divisor = 1;
        int suffixIndex = 0;

        // Find appropriate suffix
        while (bi / divisor >= 1000 && suffixIndex < suffixes.Length - 1)
        {
            divisor *= 1000;
            suffixIndex++;
        }

        decimal formattedValue;
        try
        {
            // Set the scaling factor to retain 3 decimal places
            BigInteger scaleFactor = new BigInteger(1000); // 10^3 for 3 decimal places
            if (divisor != 0)
            {
                BigInteger scaledBi = bi * scaleFactor;
                BigInteger bigResult = scaledBi / divisor;
                formattedValue = (decimal)bigResult / (decimal)scaleFactor;

                // Handle decimal overflow
                if (formattedValue > decimal.MaxValue)
                    formattedValue = decimal.MaxValue;
                else if (formattedValue < decimal.MinValue)
                    formattedValue = decimal.MinValue;

                // If truncating fractions, round down to the nearest integer
                if (truncateFractions)
                {
                    formattedValue = Math.Floor(formattedValue);
                }
            }
            else
            {
                throw new DivideByZeroException("Divisor cannot be zero.");
            }
        }
        catch (OverflowException)
        {
            formattedValue = decimal.MaxValue;
        }
        catch (DivideByZeroException ex)
        {
            Console.WriteLine(ex.Message);
            throw; // Re-throw as this is a critical error
        }

        // Format the number string
        string formattedString;
        if (truncateZeroFractions)
        {
            // Format with 3 decimal places initially
            formattedString = formattedValue.ToString("F3");

            // Remove trailing zeros after decimal point
            if (formattedString.Contains('.'))
            {
                formattedString = formattedString.TrimEnd('0');
                // Remove decimal point if no decimals remain
                if (formattedString.EndsWith('.'))
                    formattedString = formattedString.TrimEnd('.');
            }
        }
        else
        {
            // Use original formatting
            var formatString = truncateFractions ? "F0" : "F3";
            formattedString = formattedValue.ToString(formatString);
        }

        return $"{formattedString}{suffixes[suffixIndex]}";
    }

    // Removed FormatDoubleWithSuffix as it wasn't effective for the chart
}
