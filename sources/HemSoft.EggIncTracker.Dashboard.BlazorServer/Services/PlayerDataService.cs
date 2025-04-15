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
    // Commented out ApiService dependency - will be replaced by Domain/Data services
    // private readonly IApiService _apiService;
    private readonly ILogger<PlayerDataService> _logger;

    // Removed fields for static Domain Managers - they will be called directly
    // private readonly PlayerManager _playerManager;
    // private readonly MajPlayerRankingManager _majPlayerRankingManager;
    // private readonly PlayerContractManager _playerContractManager;
    // private readonly EventManager _eventManager;

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

    // Updated constructor signature - Only inject Logger (and potentially DbContextFactory if needed)
    public PlayerDataService(ILogger<PlayerDataService> logger)
    {
        _logger = logger;
        // Domain managers are static, no need to assign injected instances
    }

    // --- Common Calculation Methods (Keep for now, might be useful) ---

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
                return currentValue >= targetValue ? 100.0 : 0.0;
            }

            double range = targetValue - previousValue;
            if (range <= 0)
            {
                return currentValue >= targetValue ? 100.0 : 0.0;
            }

            double progress = currentValue - previousValue;
            progress = Math.Max(0.0, Math.Min(progress, range));
            double percentage = (progress / range) * 100.0;
            var finalPercentage = Math.Min(Math.Max(percentage, 0.0), 100.0);
            return finalPercentage;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating progress percentage for Current={Current}, Target={Target}, Previous={Previous}", current, target, previous);
            // Fallback logic remains the same
            try
            {
                double currentValue = ParseBigNumber(current);
                double targetValue = ParseBigNumber(target);
                if (targetValue > 0 && currentValue > 0)
                {
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

        var cleanValue = value.Trim().TrimEnd('%');
        _logger.LogDebug("ParseBigNumber cleanValue: {CleanValue}", cleanValue); // Log cleaned value

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

        double result = 0;
        try
        {
            if (double.TryParse(cleanValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out result))
            {
                _logger.LogDebug("ParseBigNumber parsed as plain number: {Result}", result); // Log result
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

    public string GetSEProgressColorStyle(BigInteger? seThisWeekValue, string? weeklySEGainGoalStr)
    {
        if (!seThisWeekValue.HasValue || string.IsNullOrEmpty(weeklySEGainGoalStr))
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
            weeklySEGoal = BigNumberCalculator.ParseBigNumber(weeklySEGainGoalStr);

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
            _logger.LogError(ex, "Error parsing or calculating SE gain goal for color style. Goal string: {GoalString}", weeklySEGainGoalStr);
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

    public (bool IsGoalMet, string MissingAmount, string ExpectedAmount, int PercentComplete) CalculateMissingSEGain(BigInteger? seThisWeekValue, string? weeklySEGainGoalStr)
    {
        if (!seThisWeekValue.HasValue || string.IsNullOrEmpty(weeklySEGainGoalStr))
            return (false, "0", "0", 0);

        var localNow = DateTime.Now;
        var dayOfWeek = (int)localNow.DayOfWeek;
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
            _logger.LogError(ex, "Error parsing or calculating SE gain goal. Goal string: {GoalString}", weeklySEGainGoalStr);
            return (false, "Error", "Error", 0);
        }

        string formattedMissingAmount = Utils.FormatBigInteger(missingAmount.ToString(CultureInfo.InvariantCulture), false, false);
        string formattedExpectedAmount = Utils.FormatBigInteger(expectedSEGain.ToString(CultureInfo.InvariantCulture), false, false);

        _logger.LogDebug("Formatted SE Gain Tooltip Values (BigInt): Missing='{FormattedMissingAmount}', Expected='{FormattedExpectedAmount}' (Raw Missing={MissingAmount}, Raw Expected={ExpectedSEGain})",
            formattedMissingAmount, formattedExpectedAmount, missingAmount, expectedSEGain);

        return (isGoalMet, formattedMissingAmount, formattedExpectedAmount, percentComplete);
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

            var playerHistoryToday = await PlayerManager.GetPlayerHistoryAsync(player.PlayerName, todayUtcStart, _logger);
            var playerHistoryWeek = await PlayerManager.GetPlayerHistoryAsync(player.PlayerName, startOfWeekUtc, _logger);

            if (player.Prestiges.HasValue)
            {
                var firstToday = playerHistoryToday.OrderBy(x => x.Updated).FirstOrDefault();
                if (firstToday is { Prestiges: not null })
                {
                    var baselineToday = firstToday.Prestiges.Value;
                    var diff = player.Prestiges.Value - baselineToday + 1;
                    prestigesToday = Math.Max(0, diff);
                }
                else
                {
                    prestigesToday = 0;
                }

                var firstThisWeek = playerHistoryWeek?.OrderBy(x => x.Updated).FirstOrDefault();
                if (firstThisWeek != null && firstThisWeek.Prestiges.HasValue)
                {
                    var baselineWeek = firstThisWeek.Prestiges.Value;
                    var diffWeek = player.Prestiges.Value - baselineWeek + 1;
                    prestigesThisWeek = Math.Max(0, diffWeek);
                }
                else
                {
                    prestigesThisWeek = prestigesToday;
                }
            }
            else
            {
                prestigesToday = 0;
                prestigesThisWeek = 0;
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
