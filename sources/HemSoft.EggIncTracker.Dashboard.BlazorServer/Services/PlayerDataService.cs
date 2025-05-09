// Updated namespace for Blazor Server project
namespace HemSoft.EggIncTracker.Dashboard.BlazorServer.Services;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics; // Added using statement for BigInteger
using System.Linq;
using System.Threading.Tasks;
using HemSoft.EggIncTracker.Data; // Added using statement for Utils
using HemSoft.EggIncTracker.Data.Dtos;
using HemSoft.EggIncTracker.Domain; // Keep Domain namespace
using Microsoft.Extensions.Logging;

public class PlayerDataService
{
    private readonly ILogger<PlayerDataService> _logger;

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

    private static Dictionary<string, double> Suffixes => SuffixesLazy.Value;

    public PlayerDataService(ILogger<PlayerDataService> logger)
    {
        _logger = logger;
    }

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

            if (targetValue <= previousValue)
            {
                _logger.LogWarning("Target value {Target} is less than or equal to previous value {Previous}", targetValue, previousValue);
                return currentValue >= targetValue ? 100.0 : 0.0;
            }

            if (currentValue >= targetValue)
            {
                return 100.0;
            }

            // Handle case where current is below previous
            if (currentValue <= previousValue)
            {
                return 0.0;
            }

            // Normal case: current is between previous and target
            double range = targetValue - previousValue;
            double progress = currentValue - previousValue;
            double percentage = (progress / range) * 100.0;

            // Ensure percentage is between 0 and 100
            var finalPercentage = Math.Min(Math.Max(percentage, 0.0), 100.0);
            return finalPercentage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating progress percentage for Current={Current}, Target={Target}, Previous={Previous}", current, target, previous);
            try
            {
                double currentValue = ParseBigNumber(current);
                double targetValue = ParseBigNumber(target);
                if (targetValue > 0 && currentValue > 0)
                {
                    // Simple percentage calculation as fallback
                    if (currentValue >= targetValue)
                    {
                        _logger.LogWarning("Fallback: Current value {Current} is at or beyond target {Target}, returning 100%", currentValue, targetValue);
                        return 100.0;
                    }

                    double simplePercentage = (currentValue / targetValue) * 100.0;
                    simplePercentage = Math.Min(Math.Max(simplePercentage, 0.0), 100.0);
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

    public double ParseBigNumber(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            _logger.LogWarning("ParseBigNumber received null or empty value");
            return 0;
        }

        var cleanValue = value.Trim().TrimEnd('%');
        if (cleanValue.Contains('e', StringComparison.OrdinalIgnoreCase))
        {
            if (double.TryParse(cleanValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double sciResult))
            {
                return sciResult;
            }
            _logger.LogWarning("Failed to parse scientific notation: {Value}", value);
            return 0;
        }

        foreach (var suffixPair in Suffixes)
        {
            if (cleanValue.EndsWith(suffixPair.Key, StringComparison.OrdinalIgnoreCase))
            {
                var numericPart = cleanValue.Substring(0, cleanValue.Length - suffixPair.Key.Length);
                if (double.TryParse(numericPart, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                {
                    return number * suffixPair.Value;
                }
                else
                {
                    _logger.LogWarning("Failed to parse numeric part '{NumericPart}' with suffix '{Suffix}' from value '{Value}'", numericPart, suffixPair.Key, value);
                    return 0;
                }
            }
        }

        double result;
        try
        {
            if (double.TryParse(cleanValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result))
            {
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing plain number: {CleanValue}", cleanValue);
            result = 0;
        }

        _logger.LogWarning("Failed to parse big number: {Value}. Returning 0.", value);
        return result;
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

    public string GetSEProgressColorStyle(BigInteger? seThisWeekValue, string? weeklySEGainGoalStr, string? eggDayGoalStr = null)
    {
        if (!seThisWeekValue.HasValue)
            return "color: white";

        if (string.IsNullOrEmpty(weeklySEGainGoalStr) && string.IsNullOrEmpty(eggDayGoalStr))
            return "color: white";

        var localNow = DateTime.Now;
        var dayOfWeek = (int)localNow.DayOfWeek;
        int daysSinceMonday = dayOfWeek == 0 ? 7 : dayOfWeek;

        BigInteger currentSEGain = seThisWeekValue.Value;
        BigInteger weeklySEGoal;
        BigInteger expectedSEGain;
        double percentage = 0;

        try
        {
            // Use the same calculation logic as in CalculateMissingSEGain
            // If EggDayGoal is present, it overrides the WeeklySEGainGoal
            if (!string.IsNullOrEmpty(eggDayGoalStr))
            {
                // Calculate weeks remaining until Egg Day
                var today = DateTime.Today;
                var currentYear = today.Year;
                var eggDay = new DateTime(currentYear, 7, 14);

                // If Egg Day has already passed this year, use next year's date
                if (today > eggDay)
                {
                    eggDay = new DateTime(currentYear + 1, 7, 14);
                }

                // Calculate days and weeks remaining
                var daysUntilEggDay = (eggDay - today).Days;
                var weeksRemaining = Math.Max(1, daysUntilEggDay / 7.0); // Calculate exact weeks (with decimals)

                // Parse the Egg Day goal
                BigInteger eggDayGoal = BigNumberCalculator.ParseBigNumber(eggDayGoalStr);

                // Use a hardcoded value for King Friday's SE to avoid blocking database calls
                // This is a temporary solution to prevent UI freezing
                string kingFridaySE = "72.213s"; // Default value for King Friday's SE

                // Use the hardcoded value for calculations
                BigInteger currentSE = BigNumberCalculator.ParseBigNumber(kingFridaySE);

                // Calculate SE left to reach Egg Day goal
                BigInteger seLeft = eggDayGoal - currentSE;

                // Calculate how much SE is needed per week to reach the Egg Day goal
                weeklySEGoal = seLeft / (BigInteger)weeksRemaining;
            }
            else
            {
                weeklySEGoal = BigNumberCalculator.ParseBigNumber(weeklySEGainGoalStr!);
            }

            if (weeklySEGoal <= BigInteger.Zero)
                return "color: white";

            expectedSEGain = (weeklySEGoal * daysSinceMonday) / 7;

            if (expectedSEGain > BigInteger.Zero)
            {
                decimal currentDecimal = (decimal)currentSEGain;
                decimal expectedDecimal = (decimal)expectedSEGain;
                percentage = Math.Min((double)(currentDecimal / expectedDecimal * 100m), 100.0);
            }
            else
            {
                percentage = (currentSEGain > BigInteger.Zero) ? 100.0 : 0.0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing or calculating SE gain goal for color style. Weekly Goal: {WeeklyGoal}, Egg Day Goal: {EggDayGoal}",
                weeklySEGainGoalStr, eggDayGoalStr);
            return "color: white";
        }

        percentage = Math.Max(0.0, Math.Min(percentage, 100.0));
        int red = (int)(255 * (1 - percentage / 100.0));
        int green = (int)(255 * (percentage / 100.0));
        int blue = 0;

        return $"color: rgb({red}, {green}, {blue})";
    }

    public (bool IsGoalMet, int MissingAmount, int ExpectedAmount, int PercentComplete) CalculateMissingDailyPrestiges(int? prestigesToday, int dailyPrestigeGoal)
    {
        if (prestigesToday == null || dailyPrestigeGoal <= 0)
            return (false, 0, 0, 0);

        bool isGoalMet = prestigesToday.Value >= dailyPrestigeGoal;
        int missingAmount = Math.Max(0, dailyPrestigeGoal - prestigesToday.Value);
        int percentComplete = dailyPrestigeGoal > 0
            ? (int)Math.Min(Math.Round((double)prestigesToday.Value / dailyPrestigeGoal * 100), 100)
            : 0;

        return (isGoalMet, missingAmount, dailyPrestigeGoal, percentComplete);
    }

    public (bool IsGoalMet, int MissingAmount, int ExpectedAmount, int PercentComplete) CalculateMissingPrestiges(int? prestigesThisWeek, int dailyPrestigeGoal)
    {
        if (prestigesThisWeek == null || dailyPrestigeGoal <= 0)
            return (false, 0, 0, 0);

        var localNow = DateTime.Now;
        var dayOfWeek = (int)localNow.DayOfWeek;
        int daysSinceMonday = dayOfWeek == 0 ? 7 : dayOfWeek;
        int expectedPrestiges = dailyPrestigeGoal * daysSinceMonday;

        bool isGoalMet = prestigesThisWeek.Value >= expectedPrestiges;
        int missingAmount = Math.Max(0, expectedPrestiges - prestigesThisWeek.Value);
        int percentComplete = expectedPrestiges > 0
            ? (int)Math.Min(Math.Round((double)prestigesThisWeek.Value / expectedPrestiges * 100), 100)
            : 0;

        return (isGoalMet, missingAmount, expectedPrestiges, percentComplete);
    }

    public (bool IsGoalMet, string MissingAmount, string ExpectedAmount, int PercentComplete,
            string? EggDayGoalStr, string? CurrentSEStr, string? SELeftStr, double? WeeksRemaining, string? WeeklySEGoalStr)
        CalculateMissingSEGain(BigInteger? seThisWeekValue, string? weeklySEGainGoalStr, string? eggDayGoalStr = null)
    {
        if (!seThisWeekValue.HasValue)
            return (false, "0", "0", 0, null, null, null, null, null);

        var localNow = DateTime.Now;
        var dayOfWeek = (int)localNow.DayOfWeek;
        int daysSinceMonday = dayOfWeek == 0 ? 7 : dayOfWeek;

        BigInteger currentSEGain = seThisWeekValue.Value;
        BigInteger weeklySEGoal;
        BigInteger expectedSEGain;
        BigInteger missingAmount;
        int percentComplete = 0;
        bool isGoalMet = false;

        // Additional calculation details for Egg Day goal
        string? calculatedEggDayGoalStr = null;
        string? currentSEStr = null;
        string? seLeftStr = null;
        double? weeksRemainingValue = null;
        string? calculatedWeeklySEGoalStr = null;

        try
        {
            // If EggDayGoal is present, it overrides the WeeklySEGainGoal
            if (!string.IsNullOrEmpty(eggDayGoalStr))
            {
                // Calculate weeks remaining until Egg Day
                var today = DateTime.Today;
                var currentYear = today.Year;
                var eggDay = new DateTime(currentYear, 7, 14);

                // If Egg Day has already passed this year, use next year's date
                if (today > eggDay)
                {
                    eggDay = new DateTime(currentYear + 1, 7, 14);
                }

                // Calculate days and weeks remaining
                var daysUntilEggDay = (eggDay - today).Days;
                weeksRemainingValue = daysUntilEggDay / 7.0; // Calculate exact weeks (with decimals)
                var weeksRemaining = Math.Max(1, weeksRemainingValue.Value);

                _logger.LogInformation("Using Egg Day goal: {EggDayGoal}, Days until Egg Day: {DaysUntilEggDay}, Weeks remaining: {WeeksRemaining}",
                    eggDayGoalStr, daysUntilEggDay, weeksRemaining);

                // Parse the Egg Day goal
                BigInteger eggDayGoal = BigNumberCalculator.ParseBigNumber(eggDayGoalStr);
                calculatedEggDayGoalStr = eggDayGoalStr;

                // Use a hardcoded value for King Friday's SE to avoid blocking database calls
                // This is a temporary solution to prevent UI freezing
                string kingFridaySE = "72.213s"; // Default value for King Friday's SE
                currentSEStr = kingFridaySE;

                // Use the hardcoded value for calculations
                BigInteger currentSE = BigNumberCalculator.ParseBigNumber(kingFridaySE);

                // Calculate SE left to reach Egg Day goal
                BigInteger seLeft = eggDayGoal - currentSE;
                seLeftStr = Utils.FormatBigInteger(seLeft.ToString(CultureInfo.InvariantCulture), false, false);

                // Calculate how much SE is needed per week to reach the Egg Day goal
                weeklySEGoal = seLeft / (BigInteger)weeksRemaining;
                calculatedWeeklySEGoalStr = Utils.FormatBigInteger(weeklySEGoal.ToString(CultureInfo.InvariantCulture), false, false);

                _logger.LogInformation("Calculated weekly SE goal from Egg Day goal: {WeeklySEGoal}",
                    Utils.FormatBigInteger(weeklySEGoal.ToString(CultureInfo.InvariantCulture), false, false));
            }
            else if (!string.IsNullOrEmpty(weeklySEGainGoalStr))
            {
                weeklySEGoal = BigNumberCalculator.ParseBigNumber(weeklySEGainGoalStr);
                calculatedWeeklySEGoalStr = weeklySEGainGoalStr;
            }
            else
            {
                return (false, "0", "0", 0, null, null, null, null, null);
            }

            if (weeklySEGoal <= BigInteger.Zero)
                return (false, "0", "0", 0, null, null, null, null, null);

            expectedSEGain = (weeklySEGoal * daysSinceMonday) / 7;
            isGoalMet = currentSEGain >= expectedSEGain;
            missingAmount = BigInteger.Max(BigInteger.Zero, expectedSEGain - currentSEGain);

            if (expectedSEGain > BigInteger.Zero)
            {
                BigInteger scaledCurrent = currentSEGain * 10000;
                BigInteger percentageScaled = scaledCurrent / expectedSEGain;
                percentageScaled = BigInteger.Min(BigInteger.Max(percentageScaled, BigInteger.Zero), 10000);
                percentComplete = (int)(percentageScaled / 100);
            }
            else
            {
                percentComplete = (currentSEGain > BigInteger.Zero) ? 100 : 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing or calculating SE gain goal. Weekly Goal: {WeeklyGoal}, Egg Day Goal: {EggDayGoal}",
                weeklySEGainGoalStr, eggDayGoalStr);
            return (false, "Error", "Error", 0, null, null, null, null, null);
        }

        string formattedMissingAmount = Utils.FormatBigInteger(missingAmount.ToString(CultureInfo.InvariantCulture), false, false);
        string formattedExpectedAmount = Utils.FormatBigInteger(expectedSEGain.ToString(CultureInfo.InvariantCulture), false, false);

        return (isGoalMet, formattedMissingAmount, formattedExpectedAmount, percentComplete,
                calculatedEggDayGoalStr, currentSEStr, seLeftStr, weeksRemainingValue, calculatedWeeklySEGoalStr);
    }

    public async Task<(int? PrestigesToday, int? PrestigesThisWeek)> CalculatePrestigesAsync(PlayerDto player)
    {
        if (string.IsNullOrEmpty(player.PlayerName))
        {
            _logger.LogWarning("CalculatePrestigesAsync called with null or invalid player.");
            return (0, 0);
        }

        int? prestigesToday = 0;
        int? prestigesThisWeek = 0;

        try
        {
            var localNow = DateTime.Now;
            var localToday = new DateTime(localNow.Year, localNow.Month, localNow.Day, 0, 0, 0, DateTimeKind.Local);
            var todayUtcStart = localToday.ToUniversalTime();

            int daysSinceMonday = ((int)localToday.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            var startOfWeekLocal = localToday.AddDays(-daysSinceMonday);
            var startOfWeekUtc = startOfWeekLocal.ToUniversalTime();

            // Calculate Prestiges Today:
            var playerHistoryToday = await PlayerManager.GetPlayerHistoryAsync(player.PlayerName, todayUtcStart, _logger);
            if (playerHistoryToday == null || playerHistoryToday.Count < 2)
            {
                prestigesToday = 0;
            }
            else
            {
                prestigesToday = playerHistoryToday[playerHistoryToday.Count - 1].Prestiges - playerHistoryToday[0].Prestiges;
            }

            // Calculate Prestiges This Week:
            var playerHistoryWeek = await PlayerManager.GetPlayerHistoryAsync(player.PlayerName, startOfWeekUtc, _logger);
            if (playerHistoryWeek == null || playerHistoryWeek.Count < 2)
            {
                prestigesThisWeek = 0;
            }
            else
            {
                prestigesThisWeek = playerHistoryWeek[playerHistoryWeek.Count - 1].Prestiges - playerHistoryWeek[0].Prestiges;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating prestiges for player {PlayerName}", player.PlayerName);
            prestigesToday = 0;
            prestigesThisWeek = 0;
        }

        return (prestigesToday, prestigesThisWeek);
    }


    public async Task<BigInteger?> CalculateSEThisWeekAsync(PlayerDto player)
    {
        if (string.IsNullOrEmpty(player.PlayerName))
        {
            _logger.LogWarning("CalculateSEThisWeekAsync called with null or invalid player.");
            return null;
        }

        BigInteger? seDifference;
        try
        {
            var localNow = DateTime.Now;
            var localToday = new DateTime(localNow.Year, localNow.Month, localNow.Day, 0, 0, 0, DateTimeKind.Local);
            var daysSinceMonday = ((int)localToday.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            var startOfWeekLocal = localToday.AddDays(-daysSinceMonday);
            var startOfWeekUtc = startOfWeekLocal.ToUniversalTime();

            var playerHistoryWeek = await PlayerManager.GetPlayerHistoryAsync(player.PlayerName, startOfWeekUtc, _logger);
            var firstThisWeek = playerHistoryWeek?.OrderBy(x => x.Updated).FirstOrDefault();
            var lastThisWeek = playerHistoryWeek?.OrderByDescending(x => x.Updated).FirstOrDefault();

            if (playerHistoryWeek != null && playerHistoryWeek.Count > 0 && firstThisWeek != null && lastThisWeek != null)
            {
                var earliestSEStr = firstThisWeek.SoulEggsFull;
                var latestSEStr = lastThisWeek.SoulEggsFull;

                try
                {
                    BigInteger latestSE = BigNumberCalculator.ParseBigNumber(latestSEStr);
                    BigInteger earliestSE = BigNumberCalculator.ParseBigNumber(earliestSEStr);
                    seDifference = BigInteger.Abs(latestSE - earliestSE);
                }
                catch (Exception parseEx)
                {
                    _logger.LogError(parseEx, "[PlayerDataService] Error parsing SE values for BigInteger calculation for {PlayerName}. Earliest='{EarliestSE}', Latest='{LatestSE}'", player.PlayerName, earliestSEStr, latestSEStr);
                    seDifference = BigInteger.Zero;
                }
            }
            else
            {
                _logger.LogWarning("[PlayerDataService] No player history found for {PlayerName} this week, returning null for SE gain", player.PlayerName);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PlayerDataService] Error calculating SE this week for {PlayerName}", player.PlayerName);
            return null;
        }
        return seDifference;
    }
}
