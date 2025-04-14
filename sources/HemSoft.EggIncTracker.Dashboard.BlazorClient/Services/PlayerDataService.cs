namespace HemSoft.EggIncTracker.Dashboard.BlazorClient.Services;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics; // Added using statement for BigInteger
using System.Linq;
using System.Threading.Tasks;
using HemSoft.EggIncTracker.Data; // Added using statement for Utils
using HemSoft.EggIncTracker.Data.Dtos;
using HemSoft.EggIncTracker.Domain;
using Microsoft.Extensions.Logging;

public class PlayerDataService
{
    private readonly ApiService _apiService;
    private readonly ILogger<PlayerDataService> _logger;

    // Lazy initialization for the suffixes dictionary
    private static readonly Lazy<Dictionary<string, double>> SuffixesLazy = new Lazy<Dictionary<string, double>>(() =>
    {
        var suffixes = new Dictionary<string, double>(); // Removed StringComparer.OrdinalIgnoreCase
        suffixes.Add("K", 1e3);
        suffixes.Add("M", 1e6);
        suffixes.Add("B", 1e9);
        suffixes.Add("T", 1e12);
        suffixes.Add("q", 1e15);
        suffixes.Add("Q", 1e18);
        suffixes.Add("s", 1e21);
        suffixes.Add("S", 1e24);
        suffixes.Add("O", 1e27);
        suffixes.Add("N", 1e30);
        suffixes.Add("d", 1e33);
        suffixes.Add("U", 1e36);
        suffixes.Add("D", 1e39);
        suffixes.Add("Td", 1e42);
        suffixes.Add("Qd", 1e45);
        suffixes.Add("sd", 1e48);
        suffixes.Add("Sd", 1e51);
        suffixes.Add("Od", 1e54);
        suffixes.Add("Nd", 1e57);
        suffixes.Add("V", 1e60);
        suffixes.Add("uV", 1e63);
        suffixes.Add("dV", 1e66);
        suffixes.Add("tV", 1e69);
        suffixes.Add("qV", 1e72);
        suffixes.Add("QV", 1e75);
        suffixes.Add("sV", 1e78);
        suffixes.Add("SV", 1e81);
        suffixes.Add("OV", 1e84);
        suffixes.Add("NV", 1e87);
        suffixes.Add("tT", 1e90);
        return suffixes;
    });

    // Access the dictionary via the Value property
    private static Dictionary<string, double> Suffixes => SuffixesLazy.Value;


    public PlayerDataService(ApiService apiService, ILogger<PlayerDataService> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    // --- Common Calculation Methods ---

    public double CalculateProgressPercentage(string? current, string? target, string? previous)
    {
        if (string.IsNullOrEmpty(current) || string.IsNullOrEmpty(target) || string.IsNullOrEmpty(previous))
        {
            _logger.LogWarning($"CalculateProgressPercentage received null or empty values: Current={current}, Target={target}, Previous={previous}");
            return 0;
        }

        try
        {
            // Simplify to use only double for calculations
            double currentValue = ParseBigNumber(current);
            double targetValue = ParseBigNumber(target);
            double previousValue = ParseBigNumber(previous);

            _logger.LogDebug("Parsed values for percentage calc: Current={CurrentVal}, Target={TargetVal}, Previous={PreviousVal} (Input: C={CurrentIn}, T={TargetIn}, P={PreviousIn})",
                currentValue, targetValue, previousValue, current, target, previous);

            // --- Double Calculation ---
            if (targetValue <= previousValue)
            {
                // Handle cases where target is not strictly greater than previous (e.g., rank mismatch, player is target/previous)
                // If current is at or beyond the (lower) target, it's 100% relative to this segment.
                // Otherwise, it's 0% relative to this segment.
                return currentValue >= targetValue ? 100.0 : 0.0;
            }

            double range = targetValue - previousValue;
            // Check for non-positive range AFTER checking target <= previous
            if (range <= 0)
            {
                // This case should ideally be caught by the first check, but as a safeguard:
                // If range is zero or negative, and target > previous (which shouldn't happen here),
                // treat progress as 100% if current >= target, otherwise 0%.
                return currentValue >= targetValue ? 100.0 : 0.0;
            }

            double progress = currentValue - previousValue;

            // Clamp progress: Ensure progress is not negative and not greater than the total range.
            progress = Math.Max(0.0, Math.Min(progress, range));

            double percentage = (progress / range) * 100.0;

            // Final clamp: Ensure percentage is strictly between 0 and 100.
            var finalPercentage = Math.Min(Math.Max(percentage, 0.0), 100.0);
            return finalPercentage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating progress percentage for Current={Current}, Target={Target}, Previous={Previous}", current, target, previous);

            // Try to provide a fallback calculation if possible
            try
            {
                // If we can parse the current and target values, we can still provide a simple percentage
                double currentValue = ParseBigNumber(current);
                double targetValue = ParseBigNumber(target);

                if (targetValue > 0 && currentValue > 0)
                {
                    // Simple percentage calculation as fallback
                    double simplePercentage = Math.Min((currentValue / targetValue) * 100.0, 100.0);
                    _logger.LogWarning("Using fallback percentage calculation: {Percentage}%", simplePercentage);
                    return simplePercentage;
                }
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx, "Fallback calculation also failed");
            }

            return 0; // Return 0 on error
        }
    }


    // Make ParseBigNumber public so it can be called from components
    public double ParseBigNumber(string? value) // Changed to public and nullable string
    {
        _logger.LogDebug("ParseBigNumber received value: {Value}", value); // Log input

        if (string.IsNullOrEmpty(value))
        {
            _logger.LogWarning("ParseBigNumber received null or empty value");
            return 0;
        }

        // Remove any non-numeric characters except for decimal points and scientific notation
        // Also remove trailing '%' specifically for EB values
        var cleanValue = value.Trim().TrimEnd('%');
        _logger.LogDebug("ParseBigNumber cleanValue: {CleanValue}", cleanValue); // Log cleaned value

        // Handle scientific notation (e.g., 1.23e45)
        if (cleanValue.Contains('e', StringComparison.OrdinalIgnoreCase))
        {
            if (double.TryParse(cleanValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double sciResult))
            {
                return sciResult;
            }
            _logger.LogWarning("Failed to parse scientific notation: {Value}", value);
            return 0; // Failed to parse scientific notation
        }

        // Use the lazily initialized static Suffixes dictionary
        // Check for longest matching suffix first (e.g., "Qd" before "d")
        // No need to order by length if dictionary uses OrdinalIgnoreCase
        foreach (var suffixPair in Suffixes)
        {
            if (cleanValue.EndsWith(suffixPair.Key, StringComparison.OrdinalIgnoreCase)) // Keep OrdinalIgnoreCase check
            {
                var numericPart = cleanValue.Substring(0, cleanValue.Length - suffixPair.Key.Length);
                if (double.TryParse(numericPart, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                {
                    return number * suffixPair.Value;
                }
                else
                {
                    _logger.LogWarning("Failed to parse numeric part '{NumericPart}' with suffix '{Suffix}' from value '{Value}'", numericPart, suffixPair.Key, value);
                    return 0; // Failed to parse numeric part
                }
            }
        }

        // If no suffix is found, try to parse as a regular number
        double result = 0;
        try
        {
            if (double.TryParse(cleanValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result)) // Added AllowThousands
            {
                _logger.LogDebug("ParseBigNumber parsed as plain number: {Result}", result); // Log result
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing plain number: {CleanValue}", cleanValue);
            result = 0; // Ensure result is 0 on error
        }


        _logger.LogWarning("Failed to parse big number: {Value}. Returning 0.", value);
        // If all else fails, return 0
        return result; // Return result (which will be 0 if parsing failed)
    }


    public string GetProgressColorStyle(int? actual, int goal)
    {
        if (actual == null || goal <= 0)
            return "color: white"; // Default color if no goal or data

        double percentage = (double)actual.Value / goal * 100;

        if (percentage >= 100)
            return "color: #33CC33"; // Green
        else if (percentage >= 75)
            return "color: #3399FF"; // Blue
        else if (percentage >= 50)
            return "color: #FFCC00"; // Yellow
        else if (percentage >= 25)
            return "color: #FF6666"; // Red
        else
            return "color: #FF0000"; // Dark red
    }

    /// <summary>
    /// Gets the color style for SE This Week based on progress toward the daily portion of the weekly goal
    /// </summary>
    /// <param name="seThisWeekValue">The actual SE gained this week as a BigInteger.</param>
    /// <param name="weeklySEGainGoalStr">The weekly SE gain goal as a string (e.g., "3.000s").</param>
    public string GetSEProgressColorStyle(BigInteger? seThisWeekValue, string? weeklySEGainGoalStr)
    {
        // Use BigInteger directly, parse the goal string
        if (!seThisWeekValue.HasValue || string.IsNullOrEmpty(weeklySEGainGoalStr))
            return "color: white"; // Default color if no goal or data

        // Get the current day of the week (1 = Monday, 7 = Sunday)
        var localNow = DateTime.Now;
        var dayOfWeek = (int)localNow.DayOfWeek;
        // Convert to 1-based with Monday as 1
        int daysSinceMonday = dayOfWeek == 0 ? 7 : dayOfWeek;

        BigInteger currentSEGain = seThisWeekValue.Value;
        BigInteger weeklySEGoal;
        BigInteger expectedSEGain;
        double percentage = 0; // Use double for percentage calculation

        try
        {
            weeklySEGoal = BigNumberCalculator.ParseBigNumber(weeklySEGainGoalStr);

            if (weeklySEGoal <= BigInteger.Zero)
                return "color: white";

            // Calculate expected gain using BigInteger arithmetic
            expectedSEGain = (weeklySEGoal * daysSinceMonday) / 7;

            // Calculate percentage using BigInteger, convert to double at the end
            if (expectedSEGain > BigInteger.Zero)
            {
                // Use decimal for intermediate percentage calculation for better precision than double
                decimal currentDecimal = (decimal)currentSEGain;
                decimal expectedDecimal = (decimal)expectedSEGain;
                percentage = Math.Min((double)(currentDecimal / expectedDecimal * 100m), 100.0);
            }
            else
            {
                percentage = (currentSEGain > BigInteger.Zero) ? 100.0 : 0.0; // 100% if any gain, else 0%
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing or calculating SE gain goal for color style. Goal string: {GoalString}", weeklySEGainGoalStr);
            return "color: white"; // Default color on error
        }


        // Calculate RGB values for the gradient from red to green
        // Ensure percentage is clamped between 0 and 100 before color calculation
        percentage = Math.Max(0.0, Math.Min(percentage, 100.0));
        // Red component decreases from 255 to 0 as percentage increases
        int red = (int)(255 * (1 - percentage / 100.0));
        // Green component increases from 0 to 255 as percentage increases
        int green = (int)(255 * (percentage / 100.0));
        // Blue component remains 0
        int blue = 0;

        return $"color: rgb({red}, {green}, {blue})";
    }

    /// <summary>
    /// Determines if the daily prestige goal has been reached
    /// </summary>
    public bool HasReachedDailyPrestigeGoal(int? prestigesToday, int dailyPrestigeGoal)
    {
        if (prestigesToday == null || dailyPrestigeGoal <= 0)
            return false;

        return prestigesToday.Value >= dailyPrestigeGoal;
    }

    /// <summary>
    /// Calculates how many prestiges are missing to reach the daily goal
    /// </summary>
    /// <returns>A tuple with (bool IsGoalMet, int MissingAmount, int ExpectedAmount, int PercentComplete)</returns>
    public (bool IsGoalMet, int MissingAmount, int ExpectedAmount, int PercentComplete) CalculateMissingDailyPrestiges(int? prestigesToday, int dailyPrestigeGoal)
    {
        if (prestigesToday == null || dailyPrestigeGoal <= 0)
            return (false, 0, 0, 0);

        bool isGoalMet = prestigesToday.Value >= dailyPrestigeGoal;
        int missingAmount = Math.Max(0, dailyPrestigeGoal - prestigesToday.Value);

        // Calculate percentage complete (capped at 100%)
        int percentComplete = dailyPrestigeGoal > 0
            ? (int)Math.Min(Math.Round((double)prestigesToday.Value / dailyPrestigeGoal * 100), 100)
            : 0;

        return (isGoalMet, missingAmount, dailyPrestigeGoal, percentComplete);
    }

    /// <summary>
    /// Determines if the weekly prestige goal has been reached for the current day of the week
    /// </summary>
    public bool HasReachedWeeklyPrestigeGoalForCurrentDay(int? prestigesThisWeek, int dailyPrestigeGoal)
    {
        if (prestigesThisWeek == null || dailyPrestigeGoal <= 0)
            return false;

        // Get the current day of the week (1 = Monday, 7 = Sunday)
        var localNow = DateTime.Now;
        var dayOfWeek = (int)localNow.DayOfWeek;
        // Convert to 1-based with Monday as 1
        int daysSinceMonday = dayOfWeek == 0 ? 7 : dayOfWeek;

        // Calculate the expected prestiges for the current day
        int expectedPrestiges = dailyPrestigeGoal * daysSinceMonday;

        _logger.LogDebug("Weekly prestige goal check: Current={Current}, Expected={Expected}, DayOfWeek={DayOfWeek}, DailyGoal={DailyGoal}",
            prestigesThisWeek, expectedPrestiges, daysSinceMonday, dailyPrestigeGoal);

        return prestigesThisWeek.Value >= expectedPrestiges;
    }

    /// <summary>
    /// Calculates how many prestiges are missing to reach the daily portion of the weekly goal
    /// </summary>
    /// <returns>A tuple with (bool IsGoalMet, int MissingAmount, int ExpectedAmount, int PercentComplete)</returns>
    public (bool IsGoalMet, int MissingAmount, int ExpectedAmount, int PercentComplete) CalculateMissingPrestiges(int? prestigesThisWeek, int dailyPrestigeGoal)
    {
        if (prestigesThisWeek == null || dailyPrestigeGoal <= 0)
            return (false, 0, 0, 0);

        // Get the current day of the week (1 = Monday, 7 = Sunday)
        var localNow = DateTime.Now;
        var dayOfWeek = (int)localNow.DayOfWeek;
        // Convert to 1-based with Monday as 1
        int daysSinceMonday = dayOfWeek == 0 ? 7 : dayOfWeek;

        // Calculate the expected prestiges for the current day
        int expectedPrestiges = dailyPrestigeGoal * daysSinceMonday;

        bool isGoalMet = prestigesThisWeek.Value >= expectedPrestiges;
        int missingAmount = Math.Max(0, expectedPrestiges - prestigesThisWeek.Value);

        // Calculate percentage complete (capped at 100%)
        int percentComplete = expectedPrestiges > 0
            ? (int)Math.Min(Math.Round((double)prestigesThisWeek.Value / expectedPrestiges * 100), 100)
            : 0;

        return (isGoalMet, missingAmount, expectedPrestiges, percentComplete);
    }

    /// <summary>
    /// Determines if the weekly SE gain goal has been reached for the current day of the week
    /// </summary>
    public bool HasReachedWeeklySEGainGoalForCurrentDay(string? seThisWeek, string? weeklySEGainGoal)
    {
        if (string.IsNullOrEmpty(seThisWeek) || string.IsNullOrEmpty(weeklySEGainGoal))
            return false;

        // Get the current day of the week (1 = Monday, 7 = Sunday)
        var localNow = DateTime.Now;
        var dayOfWeek = (int)localNow.DayOfWeek;
        // Convert to 1-based with Monday as 1
        int daysSinceMonday = dayOfWeek == 0 ? 7 : dayOfWeek;

        // Parse the SE values
        double currentSEGain = ParseBigNumber(seThisWeek);
        double weeklySEGoal = ParseBigNumber(weeklySEGainGoal);

        if (weeklySEGoal <= 0)
            return false;

        // Calculate the expected SE gain for the current day (proportional to days passed)
        double expectedSEGain = (weeklySEGoal / 7.0) * daysSinceMonday;

        _logger.LogDebug("Weekly SE gain goal check: Current={Current}, Expected={Expected}, DayOfWeek={DayOfWeek}, WeeklyGoal={WeeklyGoal}",
            currentSEGain, expectedSEGain, daysSinceMonday, weeklySEGoal);

        return currentSEGain >= expectedSEGain;
    }

    /// <summary>
    /// Calculates how much SE gain is missing to reach the daily portion of the weekly goal
    /// </summary>
    /// <param name="seThisWeekValue">The actual SE gained this week as a BigInteger.</param>
    /// <param name="weeklySEGainGoalStr">The weekly SE gain goal as a string (e.g., "3.000s").</param>
    /// <returns>A tuple with (bool IsGoalMet, string MissingAmount, string ExpectedAmount, int PercentComplete)</returns>
    public (bool IsGoalMet, string MissingAmount, string ExpectedAmount, int PercentComplete) CalculateMissingSEGain(BigInteger? seThisWeekValue, string? weeklySEGainGoalStr)
    {
        // Use BigInteger directly, parse the goal string
        if (!seThisWeekValue.HasValue || string.IsNullOrEmpty(weeklySEGainGoalStr))
            return (false, "0", "0", 0);

        // Get the current day of the week (1 = Monday, 7 = Sunday)
        var localNow = DateTime.Now;
        var dayOfWeek = (int)localNow.DayOfWeek;
        // Convert to 1-based with Monday as 1
        int daysSinceMonday = dayOfWeek == 0 ? 7 : dayOfWeek;

        BigInteger currentSEGain = seThisWeekValue.Value;
        BigInteger weeklySEGoal;
        BigInteger expectedSEGain;
        BigInteger missingAmount;
        int percentComplete = 0;
        bool isGoalMet = false;

        try
        {
            weeklySEGoal = BigNumberCalculator.ParseBigNumber(weeklySEGainGoalStr);

            if (weeklySEGoal <= BigInteger.Zero)
                return (false, "0", "0", 0);

            // Calculate expected gain using BigInteger arithmetic to maintain precision
            // Multiply first to avoid potential truncation with integer division
            expectedSEGain = (weeklySEGoal * daysSinceMonday) / 7;

            isGoalMet = currentSEGain >= expectedSEGain;
            missingAmount = BigInteger.Max(BigInteger.Zero, expectedSEGain - currentSEGain);

            // Calculate percentage complete using BigInteger
            if (expectedSEGain > BigInteger.Zero)
            {
                // Multiply by 10000 for 2 decimal places in percentage before dividing
                BigInteger scaledCurrent = currentSEGain * 10000;
                BigInteger percentageScaled = scaledCurrent / expectedSEGain;
                // Clamp between 0 and 10000 (representing 0% to 100.00%)
                percentageScaled = BigInteger.Min(BigInteger.Max(percentageScaled, BigInteger.Zero), 10000);
                // Convert back to integer percentage (0-100)
                percentComplete = (int)(percentageScaled / 100);
            }
            else
            {
                percentComplete = (currentSEGain > BigInteger.Zero) ? 100 : 0; // If goal is 0, 100% if any gain, else 0%
            }

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing or calculating SE gain goal. Goal string: {GoalString}", weeklySEGainGoalStr);
            return (false, "Error", "Error", 0); // Indicate error in formatting
        }

        // Format the BigInteger results using Utils.FormatBigInteger
        string formattedMissingAmount = Utils.FormatBigInteger(missingAmount.ToString(CultureInfo.InvariantCulture), false, false);
        string formattedExpectedAmount = Utils.FormatBigInteger(expectedSEGain.ToString(CultureInfo.InvariantCulture), false, false);

        // Corrected LogDebug call again: Placeholders match parameters exactly now.
        _logger.LogDebug("Formatted SE Gain Tooltip Values (BigInt): Missing='{FormattedMissingAmount}', Expected='{FormattedExpectedAmount}' (Raw Missing={MissingAmount}, Raw Expected={ExpectedSEGain})", // Corrected placeholder names
            formattedMissingAmount, formattedExpectedAmount, missingAmount, expectedSEGain);

        return (isGoalMet, formattedMissingAmount, formattedExpectedAmount, percentComplete);
    }

    // --- More Complex Methods (To be added) ---    // Calculation for player prestige counts (today and this week)
    public async Task<(int? PrestigesToday, int? PrestigesThisWeek)> CalculatePrestigesAsync(PlayerDto player)
    {
        if (player == null || string.IsNullOrEmpty(player.PlayerName))
        {
            _logger.LogWarning("CalculatePrestigesAsync called with null or invalid player.");
            return (0, 0); // Return default values
        }

        int? prestigesToday = 0;
        int? prestigesThisWeek = 0;

        try
        {
            // Get the current time in local time instead of UTC to match the user's day
            var localNow = DateTime.Now;
            var localToday = new DateTime(localNow.Year, localNow.Month, localNow.Day, 0, 0, 0, DateTimeKind.Local);
            var todayUtcStart = localToday.ToUniversalTime(); // Convert to UTC for API calls

            // Calculate the start of the week (Monday) in local time, then convert to UTC
            int daysSinceMonday = ((int)localToday.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7; // Monday as start
            var startOfWeekLocal = localToday.AddDays(-daysSinceMonday);
            var startOfWeekUtc = startOfWeekLocal.ToUniversalTime();

            // Debug current time information
            _logger.LogDebug("Time info for {PlayerName}: LocalNow={LocalNow}, LocalToday={LocalToday}, TodayUtcStart={TodayUtcStart}",
                player.PlayerName, localNow, localToday, todayUtcStart);

            // Get player history for today and this week (use accurate time ranges)
            var playerHistoryTodayTask = _apiService.GetPlayerHistoryAsync(
                player.PlayerName,
                todayUtcStart,
                DateTime.UtcNow); // Ensure we get the very latest data

            var playerHistoryWeekTask = _apiService.GetPlayerHistoryAsync(
                player.PlayerName,
                startOfWeekUtc,
                DateTime.UtcNow);

            // Wait for both tasks to complete
            await Task.WhenAll(playerHistoryTodayTask, playerHistoryWeekTask);

            var playerHistoryToday = await playerHistoryTodayTask;
            var playerHistoryWeek = await playerHistoryWeekTask;

            _logger.LogDebug("Player history records retrieved for {PlayerName}: Today={TodayCount}, ThisWeek={WeekCount}",
                player.PlayerName,
                playerHistoryToday?.Count ?? 0,
                playerHistoryWeek?.Count ?? 0);

            // Debug: Log today's prestige records to diagnose issues
            if (playerHistoryToday != null && playerHistoryToday.Any())
            {
                var earliest = playerHistoryToday.OrderBy(x => x.Updated).FirstOrDefault();
                var latest = playerHistoryToday.OrderByDescending(x => x.Updated).FirstOrDefault();

                if (earliest != null && latest != null)
                {
                    _logger.LogDebug("Today's prestige range for {PlayerName}: Earliest={Earliest} at {EarliestTime}, Latest={Latest} at {LatestTime}",
                        player.PlayerName,
                        earliest.Prestiges,
                        earliest.Updated,
                        latest.Prestiges,
                        latest.Updated);
                }

                // Log first 5 records to help diagnose the issue
                int i = 0;
                foreach (var record in playerHistoryToday.OrderBy(x => x.Updated).Take(5))
                {
                    _logger.LogDebug("Today's record #{Idx} for {PlayerName}: Updated={Updated}, Prestiges={Prestiges}",
                        ++i, player.PlayerName, record.Updated, record.Prestiges);
                }
            }

            // Calculate prestige counts
            if (player.Prestiges.HasValue)
            {
                // --- DAILY PRESTIGE CALCULATION ---
                if (playerHistoryToday == null || playerHistoryToday.Count == 0)
                {
                    // No history for today - this is the first record of the day
                    // We need to find yesterday's last record to use as baseline
                    _logger.LogDebug("No history found for {PlayerName} today, checking yesterday's records", player.PlayerName);

                    var yesterdayEnd = localToday.AddDays(-1).ToUniversalTime();
                    var yesterdayStart = new DateTime(yesterdayEnd.Year, yesterdayEnd.Month, yesterdayEnd.Day, 0, 0, 0, DateTimeKind.Utc);

                    var playerHistoryYesterday = await _apiService.GetPlayerHistoryAsync(
                        player.PlayerName,
                        yesterdayStart,
                        yesterdayEnd);

                    var lastYesterday = playerHistoryYesterday?.OrderByDescending(x => x.Updated).FirstOrDefault();

                    // If we have yesterday's record, use it as baseline
                    if (lastYesterday != null && lastYesterday.Prestiges.HasValue)
                    {
                        int baselineToday = lastYesterday.Prestiges.Value;

                        // Check if the player has prestiged today by looking at the last update time
                        var lastUpdate = player.Updated;
                        var todayStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0, DateTimeKind.Local);
                        bool updatedToday = lastUpdate >= todayStart;

                        if (!updatedToday)
                        {
                            // If the player hasn't been updated today, they haven't prestiged today
                            prestigesToday = 0;
                            _logger.LogDebug("Player {PlayerName} hasn't been updated today, setting prestigesToday to 0", player.PlayerName);
                        }
                        else
                        {
                            // Only add the +1 adjustment if the player has actually prestiged today
                            int diff = player.Prestiges.Value - baselineToday;
                            // Corrected logic: Only add 1 if diff > 0
                            prestigesToday = Math.Max(0, diff); // Calculate difference, ensure non-negative

                            _logger.LogDebug("Using yesterday's last record as baseline for {PlayerName}: Yesterday={Yesterday}, Today={Today}, Difference={Diff}, Updated Today={UpdatedToday}, PrestigesToday={PrestigesToday}",
                                player.PlayerName, baselineToday, player.Prestiges.Value, diff, updatedToday, prestigesToday);
                        }
                    }
                    else
                    {
                        // No history from yesterday either
                        // We need to determine if the player has actually prestiged today
                        // Check if the player has prestiged at all (Prestiges > 0)
                        if (player.Prestiges.Value > 0)
                        {
                            // Check if the player has prestiged today by looking at the last update time
                            var lastUpdate = player.Updated;
                            var todayStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0, DateTimeKind.Local);

                            // If the player was updated today AND has prestiged at least once ever,
                            // we'll assume they've prestiged today (1) unless we know otherwise
                            bool updatedToday = lastUpdate >= todayStart;

                            // If no history today or yesterday, prestiges today must be 0.
                            // We cannot assume a prestige happened just because the record was updated.
                            prestigesToday = 0;

                            // Corrected LogDebug call: Matched placeholder {updatedToday} and removed extra argument prestigesToday
                            _logger.LogDebug("No history from yesterday for {PlayerName}, setting prestigesToday to 0. Last update: {LastUpdate}, today start: {TodayStart}, updated today: {updatedToday}",
                                player.PlayerName, lastUpdate, todayStart, updatedToday);
                        }
                        else
                        {
                            // Player has never prestiged
                            prestigesToday = 0;
                            _logger.LogDebug($"No history from yesterday for {player.PlayerName} and player has never prestiged, setting prestigesToday to 0");
                        }
                    }
                }
                else
                {
                    // We have history records for today
                    // Get the starting prestiges for today (earliest record of the day)
                    var firstToday = playerHistoryToday.OrderBy(x => x.Updated).FirstOrDefault();

                    if (firstToday != null && firstToday.Prestiges.HasValue)
                    {
                        // Use the first record of today as baseline
                        int baselineToday = firstToday.Prestiges.Value;

                        // Corrected logic: Only add 1 if current > baseline
                        int diff = player.Prestiges.Value - baselineToday;
                        prestigesToday = Math.Max(0, diff); // Calculate difference, ensure non-negative

                        // Log the calculation WITHOUT the +1 adjustment for clarity
                        _logger.LogWarning("Daily prestiges calculation for {PlayerName}: Current={Current} - Baseline={Baseline} = Calculated={Calculated}, Assigned={Assigned}",
                            player.PlayerName, player.Prestiges.Value, baselineToday, diff, prestigesToday);
                    }
                    else
                    {
                        prestigesToday = 0;
                        _logger.LogWarning("Invalid first record for {PlayerName} today, defaulting daily prestiges to 0", player.PlayerName);
                    }
                }

                // --- WEEKLY PRESTIGE CALCULATION ---
                // Get the starting prestiges for the week (earliest record of the week)
                var firstThisWeek = playerHistoryWeek?.OrderBy(x => x.Updated).FirstOrDefault();

                if (firstThisWeek != null && firstThisWeek.Prestiges.HasValue)
                {
                    int baselineWeek = firstThisWeek.Prestiges.Value;
                    // Corrected logic: Only add 1 if current > baseline
                    int diffWeek = player.Prestiges.Value - baselineWeek;
                    prestigesThisWeek = Math.Max(0, diffWeek); // Calculate difference, ensure non-negative

                    // Log weekly calculation WITHOUT the +1 adjustment for clarity
                    _logger.LogWarning("Weekly prestiges calculation for {PlayerName}: Current={Current} - Baseline={Baseline} = Calculated={Calculated}, Assigned={Assigned}",
                        player.PlayerName, player.Prestiges.Value, baselineWeek, diffWeek, prestigesThisWeek);
                }
                else
                {
                    // Fallback - if we don't have weekly data, use daily prestiges
                    // This ensures that players who have prestiged today (like King Sunday) will show the correct count
                    // For players who haven't prestiged this week, this will show the same as prestigesToday
                    prestigesThisWeek = prestigesToday;
                    _logger.LogDebug("No weekly history for {PlayerName}, using daily prestiges: {DailyPrestiges}",
                        player.PlayerName, prestigesToday);
                }

                // Make sure the value is not accidentally negative or null
                prestigesToday = Math.Max(0, prestigesToday ?? 0);
                prestigesThisWeek = Math.Max(0, prestigesThisWeek ?? 0);

                // Ensure PrestigesThisWeek is at least equal to PrestigesToday
                if (prestigesToday.HasValue && (!prestigesThisWeek.HasValue || prestigesThisWeek.Value < prestigesToday.Value))
                {
                    prestigesThisWeek = prestigesToday;
                }

                // Log the final calculated values for debugging
                _logger.LogDebug("Final calculated prestige values for {PlayerName}: Today={Today}, Week={Week}",
                    player.PlayerName, prestigesToday, prestigesThisWeek);

                // Critical check - verify final values before returning
                _logger.LogWarning("FINAL prestige counts for {PlayerName}: Today={Today}, Week={Week}, Current={Current}, PlayerName={PlayerNameValue}",
                    player.PlayerName, prestigesToday, prestigesThisWeek, player.Prestiges, player.PlayerName);
            }
            else
            {
                prestigesToday = 0;
                prestigesThisWeek = 0;
                _logger.LogWarning("Player {PlayerName} prestige count is null, setting Today/Week to 0", player.PlayerName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating prestiges for player {PlayerName}", player.PlayerName);
            prestigesToday = 0; // Default to 0 on error
            prestigesThisWeek = 0;
        }

        return (prestigesToday, prestigesThisWeek);
    }


    // Calculation for SE This Week
    public async Task<BigInteger?> CalculateSEThisWeekAsync(PlayerDto player) // Return BigInteger?
    {
        if (player == null || string.IsNullOrEmpty(player.PlayerName))
        {
            _logger.LogWarning("CalculateSEThisWeekAsync called with null or invalid player.");
            return null; // Return null for invalid input
        }

        BigInteger? seDifference = null; // Initialize result as null BigInteger
        try
        {
            // Get the current time in local time instead of UTC to match the user's day
            var localNow = DateTime.Now;
            var localToday = new DateTime(localNow.Year, localNow.Month, localNow.Day, 0, 0, 0, DateTimeKind.Local);

            // Calculate the start of the week (Monday) in local time, then convert to UTC
            int daysSinceMonday = ((int)localToday.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7; // Monday as start
            var startOfWeekLocal = localToday.AddDays(-daysSinceMonday);
            var startOfWeekUtc = startOfWeekLocal.ToUniversalTime();

            // Log the time range for debugging
            _logger.LogDebug("[PlayerDataService] Requesting player history for {PlayerName} from {From} to {To} (startOfWeekUtc to UtcNow)", player.PlayerName, startOfWeekUtc, DateTime.UtcNow);

            // Get player history for this week
            var playerHistoryWeek = await _apiService.GetPlayerHistoryAsync(
                player.PlayerName,
                startOfWeekUtc,
                DateTime.UtcNow); // Use UtcNow for the end date

            // If weekly data is empty or null, try getting just today's history as a fallback
            if (playerHistoryWeek == null || playerHistoryWeek.Count == 0)
            {
                var todayUtcStart = localToday.ToUniversalTime();
                var playerHistoryToday = await _apiService.GetPlayerHistoryAsync(
                   player.PlayerName,
                   todayUtcStart,
                   DateTime.UtcNow);

                if (playerHistoryToday != null && playerHistoryToday.Count > 0)
                {
                    _logger.LogDebug("No weekly history found for {PlayerName}, using daily history for weekly SE calculation", player.PlayerName);
                    playerHistoryWeek = playerHistoryToday;
                }
            }


            var firstThisWeek = playerHistoryWeek?.OrderBy(x => x.Updated).FirstOrDefault();
            var lastThisWeek = playerHistoryWeek?.OrderByDescending(x => x.Updated).FirstOrDefault();

            // Calculate SE gain this week
            if (playerHistoryWeek != null && playerHistoryWeek.Count > 0 && firstThisWeek != null && lastThisWeek != null)
            {
                var earliestSEStr = firstThisWeek.SoulEggsFull; // Use the full SE value from the earliest record this week
                var latestSEStr = lastThisWeek.SoulEggsFull;    // Use the full SE value from the latest record this week

                _logger.LogDebug("[PlayerDataService] Calculating SE gain for {PlayerName}: LatestSE='{LatestSE}', EarliestSE='{EarliestSE}' (from {Timestamp})",
                    player.PlayerName, latestSEStr, earliestSEStr, firstThisWeek.Updated);

                try
                {
                    // Parse strings to BigInteger
                    BigInteger latestSE = BigNumberCalculator.ParseBigNumber(latestSEStr);
                    BigInteger earliestSE = BigNumberCalculator.ParseBigNumber(earliestSEStr);

                    _logger.LogDebug("[PlayerDataService] Parsed SE values for {PlayerName}: LatestSE={LatestSE}, EarliestSE={EarliestSE}", player.PlayerName, latestSE, earliestSE);

                    // Calculate the difference directly as BigInteger
                    seDifference = BigInteger.Abs(latestSE - earliestSE);
                    _logger.LogDebug("[PlayerDataService] Calculated SE gain this week for {PlayerName}: Result={SEGain}", player.PlayerName, seDifference);
                }
                catch (Exception parseEx)
                {
                    _logger.LogError(parseEx, "[PlayerDataService] Error parsing SE values for BigInteger calculation for {PlayerName}. Earliest='{EarliestSE}', Latest='{LatestSE}'", player.PlayerName, earliestSEStr, latestSEStr);
                    seDifference = BigInteger.Zero; // Default on parsing/calculation error
                }
            }
            else
            {
                _logger.LogWarning("[PlayerDataService] No player history found for {PlayerName} this week, returning null for SE gain", player.PlayerName);
                return null; // Return null if no history is found, so UI can display "N/A"
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PlayerDataService] Error calculating SE this week for {PlayerName}", player.PlayerName);
            return null; // Return null on error
        }
        return seDifference; // Return the BigInteger difference (or null if input was invalid)
    }
}
