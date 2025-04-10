using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using EggDash.Client.Pages; // Might need adjustment if DTOs are moved
using HemSoft.EggIncTracker.Data.Dtos;
using HemSoft.EggIncTracker.Domain; // For BigNumberCalculator
using Microsoft.Extensions.Logging;

namespace EggDash.Client.Services
{
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
                _logger.LogDebug("Calculated percentage details: Progress={Progress}, Range={Range}, RawPercentage={RawPercentage}, FinalPercentage={FinalPercentage}",
                    progress, range, percentage, finalPercentage);
                return finalPercentage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating progress percentage for Current={Current}, Target={Target}, Previous={Previous}", current, target, previous);
                return 0; // Return 0 on error
            }
        }


        // Make ParseBigNumber public so it can be called from components
        public double ParseBigNumber(string? value) // Changed to public and nullable string
        {
            _logger.LogDebug("ParseBigNumber received value: {Value}", value); // Log input

            if (string.IsNullOrEmpty(value))
                return 0;

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
        /// Determines if the daily prestige goal has been reached
        /// </summary>
        public bool HasReachedDailyPrestigeGoal(int? prestigesToday, int dailyPrestigeGoal)
        {
            if (prestigesToday == null || dailyPrestigeGoal <= 0)
                return false;

            return prestigesToday.Value >= dailyPrestigeGoal;
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

        // --- More Complex Methods (To be added) ---

        // Placeholder for Prestige Calculation
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

                // Get player history for today and this week
                var playerHistoryTodayTask = _apiService.GetPlayerHistoryAsync(player.PlayerName, todayUtcStart, DateTime.UtcNow);
                var playerHistoryWeekTask = _apiService.GetPlayerHistoryAsync(player.PlayerName, startOfWeekUtc, DateTime.UtcNow);

                await Task.WhenAll(playerHistoryTodayTask, playerHistoryWeekTask);

                var playerHistoryToday = await playerHistoryTodayTask;
                var playerHistoryWeek = await playerHistoryWeekTask;


                // If weekly data is empty or null but we have daily data, use daily data for weekly calculation
                if ((playerHistoryWeek == null || !playerHistoryWeek.Any()) && playerHistoryToday != null && playerHistoryToday.Any())
                {
                    playerHistoryWeek = playerHistoryToday;
                }

                var firstToday = playerHistoryToday?.OrderBy(x => x.Updated).FirstOrDefault();
                var firstThisWeek = playerHistoryWeek?.OrderBy(x => x.Updated).FirstOrDefault();

                // Calculate prestige counts
                if (player.Prestiges.HasValue)
                {
                    // Use current prestiges as baseline if no history record found for the start of the period
                    int baselineToday = firstToday?.Prestiges ?? player.Prestiges.Value;
                    int baselineWeek = firstThisWeek?.Prestiges ?? player.Prestiges.Value;

                    prestigesToday = player.Prestiges.Value - baselineToday;
                    prestigesThisWeek = player.Prestiges.Value - baselineWeek;

                    // Ensure prestiges are not negative
                    prestigesToday = Math.Max(0, prestigesToday ?? 0);
                    prestigesThisWeek = Math.Max(0, prestigesThisWeek ?? 0);


                    // Ensure PrestigesThisWeek is at least equal to PrestigesToday
                    if (prestigesToday.HasValue && (!prestigesThisWeek.HasValue || prestigesThisWeek.Value < prestigesToday.Value))
                    {
                        prestigesThisWeek = prestigesToday;
                    }
                    _logger.LogInformation("Calculated prestiges for {PlayerName}: Today={Today}, Week={Week} (Current: {Current}, BaselineToday: {BaselineToday}, BaselineWeek: {BaselineWeek})",
                       player.PlayerName, prestigesToday, prestigesThisWeek, player.Prestiges, baselineToday, baselineWeek);
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


        // Placeholder for SE This Week Calculation
        public async Task<string> CalculateSEThisWeekAsync(PlayerDto player)
        {
            if (player == null || string.IsNullOrEmpty(player.PlayerName))
            {
                _logger.LogWarning("CalculateSEThisWeekAsync called with null or invalid player.");
                return "0"; // Return default value
            }

            string seThisWeek = "0";
            try
            {
                // Get the current time in local time instead of UTC to match the user's day
                var localNow = DateTime.Now;
                var localToday = new DateTime(localNow.Year, localNow.Month, localNow.Day, 0, 0, 0, DateTimeKind.Local);

                // Calculate the start of the week (Monday) in local time, then convert to UTC
                int daysSinceMonday = ((int)localToday.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7; // Monday as start
                var startOfWeekLocal = localToday.AddDays(-daysSinceMonday);
                var startOfWeekUtc = startOfWeekLocal.ToUniversalTime();

                // Get player history for this week
                var playerHistoryWeek = await _apiService.GetPlayerHistoryAsync(
                    player.PlayerName,
                    startOfWeekUtc,
                    DateTime.UtcNow); // Use UtcNow for the end date

                // If weekly data is empty or null, try getting just today's history as a fallback
                if (playerHistoryWeek == null || !playerHistoryWeek.Any())
                {
                    var todayUtcStart = localToday.ToUniversalTime();
                    var playerHistoryToday = await _apiService.GetPlayerHistoryAsync(
                       player.PlayerName,
                       todayUtcStart,
                       DateTime.UtcNow);

                    if (playerHistoryToday != null && playerHistoryToday.Any())
                    {
                        _logger.LogInformation("No weekly history found for {PlayerName}, using daily history for weekly SE calculation", player.PlayerName);
                        playerHistoryWeek = playerHistoryToday;
                    }
                }


                var firstThisWeek = playerHistoryWeek?.OrderBy(x => x.Updated).FirstOrDefault();

                // Calculate SE gain this week
                if (playerHistoryWeek != null && playerHistoryWeek.Any() && firstThisWeek != null)
                {
                    var latestSE = player.SoulEggsFull; // Use the full SE value from the current player DTO
                    var earliestSE = firstThisWeek.SoulEggsFull; // Use the full SE value from the earliest record this week

                    _logger.LogInformation("Calculating SE gain for {PlayerName}: LatestSE={LatestSE}, EarliestSE={EarliestSE} (from {Timestamp})",
                        player.PlayerName, latestSE, earliestSE, firstThisWeek.Updated);

                    try
                    {
                        // Use BigNumberCalculator to calculate the difference
                        seThisWeek = BigNumberCalculator.CalculateDifference(earliestSE, latestSE);
                        _logger.LogInformation("Calculated SE gain this week for {PlayerName}: {SEGain}", player.PlayerName, seThisWeek);
                    }
                    catch (Exception calcEx)
                    {
                        _logger.LogError(calcEx, "Error using BigNumberCalculator for SE gain for {PlayerName}", player.PlayerName);
                        seThisWeek = "0"; // Default on calculation error
                    }
                }
                else
                {
                    _logger.LogWarning("No player history found for {PlayerName} this week, setting SE gain to 0", player.PlayerName);
                    seThisWeek = "0";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating SE this week for {PlayerName}", player.PlayerName);
                seThisWeek = "0"; // Default to 0 on error
            }
            return seThisWeek;
        }
    }
}
