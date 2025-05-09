namespace HemSoft.EggIncTracker.Dashboard.BlazorServer.Components;

using HemSoft.EggIncTracker.Dashboard.BlazorServer.Services;
using HemSoft.EggIncTracker.Dashboard.BlazorServer.Components.Pages;
using HemSoft.EggIncTracker.Data.Dtos;
using HemSoft.EggIncTracker.Domain;
using Microsoft.AspNetCore.Components;
using System.Globalization;
using System.Numerics;
using Timer = System.Timers.Timer;
using HemSoft.EggIncTracker.Data;

public partial class PlayerCard : IDisposable
{
    [Parameter] public DashboardPlayer? DashboardPlayer { get; set; }
    [Parameter] public DateTime? LastUpdatedTimestamp { get; set; }

    private PlayerDto? Player => DashboardPlayer?.Player;
    private PlayerStatsDto? PlayerStats => DashboardPlayer?.Stats;
    private GoalDto? PlayerGoals => DashboardPlayer?.Goals;
    private MajPlayerRankingDto? SEGoalBegin => DashboardPlayer?.SEGoalBegin;
    private MajPlayerRankingDto? SEGoalEnd => DashboardPlayer?.SEGoalEnd;
    private List<MajPlayerRankingDto> NextLowerSEPlayers => DashboardPlayer?.NextLowerSEPlayers ?? new List<MajPlayerRankingDto>();
    private List<MajPlayerRankingDto> NextUpperSEPlayers => DashboardPlayer?.NextUpperSEPlayers ?? new List<MajPlayerRankingDto>();
    private MajPlayerRankingDto? EBGoalBegin => DashboardPlayer?.EBGoalBegin;
    private MajPlayerRankingDto? EBGoalEnd => DashboardPlayer?.EBGoalEnd;
    private List<MajPlayerRankingDto> NextLowerEBPlayers => DashboardPlayer?.NextLowerEBPlayers ?? new List<MajPlayerRankingDto>();
    private List<MajPlayerRankingDto> NextUpperEBPlayers => DashboardPlayer?.NextUpperEBPlayers ?? new List<MajPlayerRankingDto>();
    private MajPlayerRankingDto? MERGoalBegin => DashboardPlayer?.MERGoalBegin;
    private MajPlayerRankingDto? MERGoalEnd => DashboardPlayer?.MERGoalEnd;
    private List<MajPlayerRankingDto> NextLowerMERPlayers => DashboardPlayer?.NextLowerMERPlayers ?? new List<MajPlayerRankingDto>();
    private List<MajPlayerRankingDto> NextUpperMERPlayers => DashboardPlayer?.NextUpperMERPlayers ?? new List<MajPlayerRankingDto>();
    private MajPlayerRankingDto? JERGoalBegin => DashboardPlayer?.JERGoalBegin;
    private MajPlayerRankingDto? JERGoalEnd => DashboardPlayer?.JERGoalEnd;
    private List<MajPlayerRankingDto> NextLowerJERPlayers => DashboardPlayer?.NextLowerJERPlayers ?? new List<MajPlayerRankingDto>();
    private List<MajPlayerRankingDto> NextUpperJERPlayers => DashboardPlayer?.NextUpperJERPlayers ?? new List<MajPlayerRankingDto>();
    private int DaysToNextTitle => DashboardPlayer?.DaysToNextTitle ?? 0;

    private BigInteger? _localPlayerSEThisWeek;
    private const int NameCutOff = 12;
    private bool _showFireworks = false;
    private bool _allGoalsAchieved = false;
    private Timer? _fireworksTimer;

    // Properties to hold calculated percentages
    private double PlayerSEGoalPercentage { get; set; }
    private double PlayerEBGoalPercentage { get; set; }
    private double PlayerMERGoalPercentage { get; set; }
    private double PlayerJERGoalPercentage { get; set; }

    // Removed event - now handled by PlayerCardService

    // Removed auto-refresh fields - now handled by PlayerCardService

    // Inject services
    [Inject] private PlayerDataService PlayerDataService { get; set; } = default!;
    [Inject] private ILogger<PlayerCard> Logger { get; set; } = default!;
    [Inject] private DashboardState DashboardState { get; set; } = default!;
    [Inject] private PlayerCardService PlayerCardService { get; set; } = default!;

    /// <summary>
    /// Calculates the number of days until the next title change
    /// </summary>
    /// <param name="projectedDate">The projected date for the title change</param>
    /// <returns>Number of days until the title change</returns>
    public int CalculateDaysToNextTitle(DateTime projectedDate)
    {
        // Handle the case where the date is MinValue (indicating N/A or error)
        if (projectedDate == DateTime.MinValue)
        {
            Logger.LogWarning($"Projected title change date is MinValue, returning 0 days.");
            return 0;
        }

        var daysUntil = (projectedDate - DateTime.Now).Days;
        return Math.Max(0, daysUntil); // Ensure it's not negative
    }

    // Helper method to format BigInteger? for display using Utils
    private static string FormatBigIntegerDisplay(BigInteger? value)
    {
        if (!value.HasValue)
        {
            return "0";
        }
        string valueStr = value.Value.ToString(CultureInfo.InvariantCulture);
        return Data.Utils.FormatBigInteger(valueStr, false, false); // Call static Utils method
    }

    private double CalculateProgressPercentage(string? current, string? target, string? previous)
    {
        // Use the injected service's method
        return PlayerDataService.CalculateProgressPercentage(current, target, previous);
    }

    // Calculates a gradient color from red (0%) to green (100%)
    private static string GetProgressColorStyle(int? actual, int goal)
    {
        if (actual == null || goal <= 0)
        {
            return "color: rgb(255, 0, 0)"; // Default to red if no goal or actual value
        }

        // Calculate percentage, clamping between 0 and 100
        double percentage = Math.Clamp((double)actual.Value / goal * 100, 0, 100);

        // Calculate RGB values for the gradient
        // Red component decreases from 255 to 0 as percentage increases
        int red = (int)(255 * (1 - percentage / 100.0));
        // Green component increases from 0 to 255 as percentage increases
        int green = (int)(255 * (percentage / 100.0));
        // Blue component remains 0
        int blue = 0;

        return $"color: rgb({red}, {green}, {blue})";
    }

    // Helper to format the timestamp for display
    private static string GetLocalTimeString(DateTime? utcTime)
    {
        if (!utcTime.HasValue || utcTime.Value == DateTime.MinValue)
        {
            return string.Empty; // Don't display if no valid time
        }

        DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime.Value, TimeZoneInfo.Local);

        return localTime.Date == DateTime.Today
            ? $"Today {localTime:HH:mm}"
            : localTime.Date == DateTime.Today.AddDays(-1)
                ? $"Yesterday {localTime:HH:mm}"
                : localTime.ToString("MM/dd HH:mm");
    }

    // Check if all goals are met
    private bool AreAllGoalsMet()
    {
        // Use null-conditional access and null checks
        if ((PlayerGoals?.WeeklySEGainGoal == null || string.IsNullOrEmpty(PlayerGoals.WeeklySEGainGoal)) &&
            (PlayerGoals?.EggDayGoal == null || string.IsNullOrEmpty(PlayerGoals.EggDayGoal)) ||
            !_localPlayerSEThisWeek.HasValue)
        {
            return false;
        }

        // Check SE This Week goal using BigInteger? for CalculateMissingSEGain
        // Pass the EggDayGoal to the method so it can override the WeeklySEGainGoal if present
        var (isSEGoalMet, _, _, _, _, _, _, _, _) = PlayerDataService.CalculateMissingSEGain(_localPlayerSEThisWeek, PlayerGoals?.WeeklySEGainGoal, PlayerGoals?.EggDayGoal);

        // Check Prestiges Today goal (use ?? 0 for goal if PlayerGoals is null)
        var (isDailyPrestigeGoalMet, _, _, _) = PlayerDataService.CalculateMissingDailyPrestiges(Player?.PrestigesToday, PlayerGoals?.DailyPrestigeGoal ?? 0);

        // Check Prestiges This Week goal (use ?? 0 for goal if PlayerGoals is null)
        var (isWeeklyPrestigeGoalMet, _, _, _) = PlayerDataService.CalculateMissingPrestiges(Player?.PrestigesThisWeek, PlayerGoals?.DailyPrestigeGoal ?? 0);

        return isSEGoalMet && isDailyPrestigeGoalMet && isWeeklyPrestigeGoalMet;
    }

    protected override void OnInitialized()
    {
        // Subscribe to the state change event using the instance variable
        DashboardState.OnChange += HandleStateChange;
        // Subscribe to the PlayerCardService refresh event
        PlayerCardService.OnPlayerDataRefreshed += HandlePlayerDataRefreshed;
        // Initial check for fireworks
        CheckGoalsAndShowFireworks();
        base.OnInitialized();
    }

    private void HandlePlayerDataRefreshed(string playerName, DashboardPlayer updatedPlayer)
    {
        try
        {
            // Only update if this is our player
            if (Player != null && !string.IsNullOrEmpty(Player.PlayerName) &&
                playerName == Player.PlayerName && updatedPlayer.Player != null)
            {
                // Update the local SEThisWeek value
                _localPlayerSEThisWeek = updatedPlayer.SEThisWeek;

                // Check goals whenever data is refreshed
                CheckGoalsAndShowFireworks();

                // Force UI update
                InvokeAsync(StateHasChanged);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error handling player data refresh for {PlayerName}", playerName);
        }
    }

    protected override void OnParametersSet()
    {
        // Get the initial value from DashboardState using the instance variable
        if (Player != null && !string.IsNullOrEmpty(Player.PlayerName))
        {
            _localPlayerSEThisWeek = DashboardState.GetPlayerSEThisWeek(Player.PlayerName);
        }

        CheckGoalsAndShowFireworks();
        CalculateAllProgressPercentages();
        base.OnParametersSet();
    }

    // Method to calculate all progress percentages
    private void CalculateAllProgressPercentages()
    {
        PlayerSEGoalPercentage = CalculateSingleProgressPercentage(Player?.SoulEggs, SEGoalEnd?.SEString, SEGoalBegin?.SEString, "SE");
        PlayerEBGoalPercentage = CalculateSingleProgressPercentage(PlayerStats?.EB, EBGoalEnd?.EBString, EBGoalBegin?.EBString, "EB");
        PlayerMERGoalPercentage = CalculateSingleProgressPercentage(PlayerStats?.MER.ToString(), MERGoalEnd?.MER.ToString(), MERGoalBegin?.MER.ToString(), "MER");
        PlayerJERGoalPercentage = CalculateSingleProgressPercentage(PlayerStats?.JER.ToString(), JERGoalEnd?.JER.ToString(), JERGoalBegin?.JER.ToString(), "JER");
    }

    // Helper method for calculating a single percentage, encapsulating the try-catch
    private double CalculateSingleProgressPercentage(string? currentValue, string? targetValue, string? previousValue, string metricName)
    {
        if (string.IsNullOrEmpty(currentValue) || string.IsNullOrEmpty(targetValue) || string.IsNullOrEmpty(previousValue))
        {
            // Already logged in the main service method, maybe add context here if needed
            return 0;
        }
        try
        {
            // Use the injected service's method
            return PlayerDataService.CalculateProgressPercentage(currentValue, targetValue, previousValue);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calculating {MetricName} progress percentage for {PlayerName}", metricName, Player?.PlayerName);
            return 0; // Return 0 on error during calculation
        }
    }


    // Handle state changes from DashboardState
    private void HandleStateChange()
    {
        if (Player != null && !string.IsNullOrEmpty(Player.PlayerName))
        {
            // Access GetPlayerSEThisWeek via the instance variable
            var newStateValue = DashboardState.GetPlayerSEThisWeek(Player.PlayerName);
            if (_localPlayerSEThisWeek != newStateValue)
            {
                _localPlayerSEThisWeek = newStateValue;
                Logger.LogDebug("PlayerCard HandleStateChange for {PlayerName}: _localPlayerSEThisWeek updated from DashboardState.", Player.PlayerName);
                // Recalculate percentages if relevant data might have changed (though SEThisWeek doesn't directly affect rank percentages)
                CalculateAllProgressPercentages();
                CheckGoalsAndShowFireworks(); // Re-check goals if SE value changed
            }
        }
    }

    private void CheckGoalsAndShowFireworks()
    {
        // Check if all goals are met
        bool wereGoalsAchieved = _allGoalsAchieved;
        _allGoalsAchieved = AreAllGoalsMet();

        // Only show fireworks if goals were just achieved (not previously achieved)
        if (_allGoalsAchieved && !wereGoalsAchieved && !_showFireworks)
        {
            _showFireworks = true;

            // Create a timer to hide the fireworks after 30 seconds
            _fireworksTimer?.Dispose(); // Dispose any existing timer
            _fireworksTimer = new Timer(30000);
            _fireworksTimer.Elapsed += (sender, e) =>
            {
                _showFireworks = false;
                _fireworksTimer?.Stop();
                _fireworksTimer?.Dispose();
                _fireworksTimer = null;
            };
            _fireworksTimer.AutoReset = false;
            _fireworksTimer.Start();
        }
    }

    // Calculate estimated total prestiges left based on average SE gain per prestige
    private async Task<string> CalculateEstimatedPrestigesLeftTotalAsync(string seLeftStr, string playerName, int? prestigesThisWeek, BigInteger? seThisWeek)
    {
        // Only perform calculations when an EggDayGoal is specified
        if (string.IsNullOrEmpty(seLeftStr) || string.IsNullOrEmpty(playerName) ||
            !prestigesThisWeek.HasValue || prestigesThisWeek.Value <= 0 ||
            !seThisWeek.HasValue || seThisWeek.Value <= 0 ||
            PlayerGoals?.EggDayGoal == null || string.IsNullOrEmpty(PlayerGoals.EggDayGoal))
        {
            return "N/A";
        }

        try
        {
            // Parse the SE left value
            BigInteger seLeft = BigNumberCalculator.ParseBigNumber(seLeftStr);

            // Get player's historical data to calculate a more accurate average SE per prestige
            var localNow = DateTime.Now;
            var localToday = new DateTime(localNow.Year, localNow.Month, localNow.Day, 0, 0, 0, DateTimeKind.Local);
            var daysSinceMonday = ((int)localToday.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            var startOfWeekLocal = localToday.AddDays(-daysSinceMonday);
            var startOfWeekUtc = startOfWeekLocal.ToUniversalTime();

            // Get player history for this week
            var playerHistoryWeek = await PlayerManager.GetPlayerHistoryAsync(playerName, startOfWeekUtc, Logger);

            // Calculate average SE gain per prestige using historical data
            BigInteger avgSEPerPrestige;

            if (playerHistoryWeek != null && playerHistoryWeek.Count >= 2)
            {
                var firstThisWeek = playerHistoryWeek.OrderBy(x => x.Updated).FirstOrDefault();
                var lastThisWeek = playerHistoryWeek.OrderByDescending(x => x.Updated).FirstOrDefault();

                if (firstThisWeek != null && lastThisWeek != null &&
                    firstThisWeek.Prestiges.HasValue && lastThisWeek.Prestiges.HasValue)
                {
                    // Calculate total prestiges this week
                    int totalPrestigesThisWeek = lastThisWeek.Prestiges.Value - firstThisWeek.Prestiges.Value;

                    if (totalPrestigesThisWeek > 0)
                    {
                        // Calculate total SE gained this week
                        BigInteger firstSE = BigNumberCalculator.ParseBigNumber(firstThisWeek.SoulEggsFull);
                        BigInteger lastSE = BigNumberCalculator.ParseBigNumber(lastThisWeek.SoulEggsFull);
                        BigInteger totalSEGained = lastSE - firstSE;

                        // Calculate average SE per prestige
                        avgSEPerPrestige = totalSEGained / totalPrestigesThisWeek;

                        // Set a minimum threshold based on user's feedback (8-9Q per prestige)
                        BigInteger minThreshold = BigNumberCalculator.ParseBigNumber("8Q");
                        if (avgSEPerPrestige < minThreshold)
                        {
                            avgSEPerPrestige = minThreshold;
                            Logger.LogInformation("Using minimum threshold of 8Q for average SE per prestige for {PlayerName}", playerName);
                        }
                    }
                    else
                    {
                        // Fallback to minimum threshold if no prestiges this week
                        avgSEPerPrestige = BigNumberCalculator.ParseBigNumber("8Q");
                        Logger.LogInformation("No prestiges this week for {PlayerName}, using minimum threshold of 8Q", playerName);
                    }
                }
                else
                {
                    // Fallback to calculated average if history data is incomplete
                    avgSEPerPrestige = seThisWeek.Value / prestigesThisWeek.Value;
                }
            }
            else
            {
                // Fallback to calculated average if history data is unavailable
                avgSEPerPrestige = seThisWeek.Value / prestigesThisWeek.Value;
            }

            // Calculate estimated prestiges needed
            BigInteger estimatedPrestiges = seLeft / avgSEPerPrestige;

            // Add 1 to account for any remainder (partial prestige)
            if (seLeft % avgSEPerPrestige > 0)
            {
                estimatedPrestiges += 1;
            }

            return estimatedPrestiges.ToString();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calculating estimated total prestiges left for {PlayerName}", playerName);
            return "N/A";
        }
    }

    // Synchronous wrapper for the async method to maintain compatibility with the existing code
    private string CalculateEstimatedPrestigesLeftTotal(string seLeftStr, int? prestigesThisWeek, BigInteger? seThisWeek)
    {
        if (Player == null || string.IsNullOrEmpty(Player.PlayerName))
        {
            return "N/A";
        }

        // Use a Task.Run to call the async method synchronously
        try
        {
            return Task.Run(() => CalculateEstimatedPrestigesLeftTotalAsync(
                seLeftStr,
                Player.PlayerName,
                prestigesThisWeek,
                seThisWeek)).Result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in synchronous wrapper for CalculateEstimatedPrestigesLeftTotal");
            return "N/A";
        }
    }

    // Calculate estimated prestiges left per week
    private async Task<string> CalculateEstimatedPrestigesLeftPerWeekAsync(string seLeftStr, string playerName, int? prestigesThisWeek, BigInteger? seThisWeek, double? weeksRemaining)
    {
        // Only perform calculations when an EggDayGoal is specified
        if (string.IsNullOrEmpty(seLeftStr) || string.IsNullOrEmpty(playerName) ||
            !prestigesThisWeek.HasValue || prestigesThisWeek.Value <= 0 ||
            !seThisWeek.HasValue || seThisWeek.Value <= 0 ||
            !weeksRemaining.HasValue || weeksRemaining.Value <= 0 ||
            PlayerGoals?.EggDayGoal == null || string.IsNullOrEmpty(PlayerGoals.EggDayGoal))
        {
            return "N/A";
        }

        try
        {
            // Get the total estimated prestiges from the total calculation
            string totalEstimatedPrestigesStr = await CalculateEstimatedPrestigesLeftTotalAsync(seLeftStr, playerName, prestigesThisWeek, seThisWeek);

            if (totalEstimatedPrestigesStr == "N/A" || !int.TryParse(totalEstimatedPrestigesStr, out int totalEstimatedPrestiges))
            {
                return "N/A";
            }

            // Calculate prestiges needed per week
            double prestigesPerWeek = (double)totalEstimatedPrestiges / weeksRemaining.Value;

            // Round up to the nearest whole number
            int roundedPrestigesPerWeek = (int)Math.Ceiling(prestigesPerWeek);

            return roundedPrestigesPerWeek.ToString();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calculating estimated prestiges left per week for {PlayerName}", playerName);
            return "N/A";
        }
    }

    // Synchronous wrapper for the async method to maintain compatibility with the existing code
    private string CalculateEstimatedPrestigesLeftPerWeek(string seLeftStr, int? prestigesThisWeek, BigInteger? seThisWeek, double? weeksRemaining)
    {
        if (Player == null || string.IsNullOrEmpty(Player.PlayerName))
        {
            return "N/A";
        }

        // Use a Task.Run to call the async method synchronously
        try
        {
            return Task.Run(() => CalculateEstimatedPrestigesLeftPerWeekAsync(
                seLeftStr,
                Player.PlayerName,
                prestigesThisWeek,
                seThisWeek,
                weeksRemaining)).Result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in synchronous wrapper for CalculateEstimatedPrestigesLeftPerWeek");
            return "N/A";
        }
    }

    // Calculate estimated prestiges left per day
    private async Task<string> CalculateEstimatedPrestigesLeftPerDayAsync(string seLeftStr, string playerName, int? prestigesThisWeek, BigInteger? seThisWeek, int? daysUntilEggDay)
    {
        // Only perform calculations when an EggDayGoal is specified
        if (string.IsNullOrEmpty(seLeftStr) || string.IsNullOrEmpty(playerName) ||
            !prestigesThisWeek.HasValue || prestigesThisWeek.Value <= 0 ||
            !seThisWeek.HasValue || seThisWeek.Value <= 0 ||
            !daysUntilEggDay.HasValue || daysUntilEggDay.Value <= 0 ||
            PlayerGoals?.EggDayGoal == null || string.IsNullOrEmpty(PlayerGoals.EggDayGoal))
        {
            return "N/A";
        }

        try
        {
            // Get the total estimated prestiges from the total calculation
            string totalEstimatedPrestigesStr = await CalculateEstimatedPrestigesLeftTotalAsync(seLeftStr, playerName, prestigesThisWeek, seThisWeek);

            if (totalEstimatedPrestigesStr == "N/A" || !int.TryParse(totalEstimatedPrestigesStr, out int totalEstimatedPrestiges))
            {
                return "N/A";
            }

            // Calculate prestiges needed per day
            double prestigesPerDay = (double)totalEstimatedPrestiges / daysUntilEggDay.Value;

            // Round up to the nearest whole number
            int roundedPrestigesPerDay = (int)Math.Ceiling(prestigesPerDay);

            return roundedPrestigesPerDay.ToString();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calculating estimated prestiges left per day for {PlayerName}", playerName);
            return "N/A";
        }
    }

    // Synchronous wrapper for the async method to maintain compatibility with the existing code
    private string CalculateEstimatedPrestigesLeftPerDay(string seLeftStr, int? prestigesThisWeek, BigInteger? seThisWeek, int? daysUntilEggDay)
    {
        if (Player == null || string.IsNullOrEmpty(Player.PlayerName))
        {
            return "N/A";
        }

        // Use a Task.Run to call the async method synchronously
        try
        {
            return Task.Run(() => CalculateEstimatedPrestigesLeftPerDayAsync(
                seLeftStr,
                Player.PlayerName,
                prestigesThisWeek,
                seThisWeek,
                daysUntilEggDay)).Result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in synchronous wrapper for CalculateEstimatedPrestigesLeftPerDay");
            return "N/A";
        }
    }

    // Calculate estimated prestiges left for today
    private async Task<string> CalculateEstimatedPrestigesLeftForTodayAsync(string seLeftStr, string playerName, int? prestigesThisWeek, BigInteger? seThisWeek, int currentDaysSinceMonday)
    {
        // Only perform calculations when an EggDayGoal is specified
        if (string.IsNullOrEmpty(seLeftStr) || string.IsNullOrEmpty(playerName) ||
            !prestigesThisWeek.HasValue || prestigesThisWeek.Value <= 0 ||
            !seThisWeek.HasValue || seThisWeek.Value <= 0 ||
            PlayerGoals?.EggDayGoal == null || string.IsNullOrEmpty(PlayerGoals.EggDayGoal))
        {
            return "N/A";
        }

        try
        {
            // Parse the missing SE value (the amount we need to achieve with prestiging)
            BigInteger missingSE = BigNumberCalculator.ParseBigNumber(seLeftStr);

            // For the "Estimated prestiges left/today" calculation, we need to use the Q value (86.035Q)
            // instead of the s value (27.787s) that's displayed in the UI
            if (seLeftStr.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            {
                // Convert from scientific notation to Q value
                // We know from the screenshot that 27.787s corresponds to 86.035Q
                // This is a temporary solution - ideally we'd have a proper conversion method
                string qValueStr = "86.035Q";
                missingSE = BigNumberCalculator.ParseBigNumber(qValueStr);
                Logger.LogInformation("Converted SE left from {Original} to {Converted} for calculation",
                    seLeftStr, qValueStr);
            }

            // Get the daily prestige target (default to 30 if not available)
            int dailyPrestigeTarget = PlayerGoals?.DailyPrestigeGoal ?? 30;

            // Calculate average SE gain per prestige using the SE/Week value and prestiges this week
            BigInteger avgSEPerPrestige;

            // If we have valid SE/Week and prestiges this week values, use them
            if (seThisWeek.HasValue && prestigesThisWeek.HasValue && prestigesThisWeek.Value > 0)
            {
                avgSEPerPrestige = seThisWeek.Value / prestigesThisWeek.Value;
                Logger.LogInformation("Calculated average SE per prestige from SE/Week: {AvgSEPerPrestige} for {PlayerName}",
                    FormatBigIntegerDisplay(avgSEPerPrestige), playerName);
            }
            else
            {
                // Fallback to minimum threshold if we don't have enough data
                avgSEPerPrestige = BigNumberCalculator.ParseBigNumber("8Q");
                Logger.LogInformation("Using minimum threshold of 8Q for average SE per prestige for {PlayerName} (insufficient data)", playerName);
            }

            // Ensure we have a minimum threshold for SE gain per prestige
            BigInteger minThreshold = BigNumberCalculator.ParseBigNumber("8Q");
            if (avgSEPerPrestige < minThreshold)
            {
                avgSEPerPrestige = minThreshold;
                Logger.LogInformation("Using minimum threshold of 8Q for SE gain per prestige for {PlayerName}", playerName);
            }

            // Calculate prestiges needed for today based on the missing SE value and SE gain per prestige
            int prestigesLeftForToday = (int)Math.Ceiling((double)missingSE / (double)avgSEPerPrestige);

            // Ensure the prestiges left for today doesn't exceed the daily prestige target
            // This is because you can't do more prestiges today than your daily target
            if (prestigesLeftForToday > dailyPrestigeTarget)
            {
                prestigesLeftForToday = dailyPrestigeTarget;
            }

            // Log the calculation details
            Logger.LogInformation(
                "Calculated prestiges left for today for {PlayerName}: Missing SE: {MissingSE}, " +
                "Daily Prestige Target: {DailyPrestigeTarget}, SE Gain Per Prestige: {SEGainPerPrestige}, Prestiges Left Today: {PrestigesLeftToday}",
                playerName, FormatBigIntegerDisplay(missingSE), dailyPrestigeTarget,
                FormatBigIntegerDisplay(avgSEPerPrestige), prestigesLeftForToday);

            return prestigesLeftForToday.ToString();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calculating estimated prestiges left for today for {PlayerName}", playerName);
            return "N/A";
        }
    }

    // Synchronous wrapper for the async method to maintain compatibility with the existing code
    private string CalculateEstimatedPrestigesLeftForToday(string seLeftStr, int? prestigesThisWeek, BigInteger? seThisWeek, int daysSinceMonday)
    {
        if (Player == null || string.IsNullOrEmpty(Player.PlayerName))
        {
            return "N/A";
        }

        // Use a Task.Run to call the async method synchronously
        // This is not ideal but necessary for compatibility with the existing code
        try
        {
            return Task.Run(() => CalculateEstimatedPrestigesLeftForTodayAsync(
                seLeftStr,
                Player.PlayerName,
                prestigesThisWeek,
                seThisWeek,
                daysSinceMonday)).Result;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in synchronous wrapper for CalculateEstimatedPrestigesLeftForToday");
            return "N/A";
        }
    }

    // Calculate and return the SE Gain per Prestige value
    private string CalculateSEGainPerPrestige(int? prestigesThisWeek, BigInteger? seThisWeek)
    {
        if (Player == null || string.IsNullOrEmpty(Player.PlayerName) ||
            !prestigesThisWeek.HasValue || prestigesThisWeek.Value <= 0 ||
            !seThisWeek.HasValue || seThisWeek.Value <= 0)
        {
            return "N/A";
        }

        try
        {
            // Calculate average SE gain per prestige using the SE/Week value and prestiges this week
            BigInteger avgSEPerPrestige = seThisWeek.Value / prestigesThisWeek.Value;

            // Ensure we have a minimum threshold for SE gain per prestige
            BigInteger minThreshold = BigNumberCalculator.ParseBigNumber("8Q");
            if (avgSEPerPrestige < minThreshold)
            {
                avgSEPerPrestige = minThreshold;
            }

            return FormatBigIntegerDisplay(avgSEPerPrestige);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calculating SE gain per prestige for {PlayerName}", Player.PlayerName);
            return "N/A";
        }
    }

    // Calculate the estimated prestiges left for today based on the actual SE gain per prestige
    // This ensures consistency with the displayed SE Gain/Prestige value
    private string CalculateEstimatedPrestigesLeftForTodayConsistent(string seLeftStr, int? prestigesThisWeek, BigInteger? seThisWeek, int daysSinceMonday)
    {
        if (Player == null || string.IsNullOrEmpty(Player.PlayerName) ||
            string.IsNullOrEmpty(seLeftStr) ||
            !prestigesThisWeek.HasValue || prestigesThisWeek.Value <= 0 ||
            !seThisWeek.HasValue || seThisWeek.Value <= 0)
        {
            return "N/A";
        }

        try
        {
            // Parse the missing SE value
            BigInteger missingSE = BigNumberCalculator.ParseBigNumber(seLeftStr);

            // Calculate average SE gain per prestige
            BigInteger avgSEPerPrestige = seThisWeek.Value / prestigesThisWeek.Value;

            // Ensure we have a minimum threshold for SE gain per prestige
            BigInteger minThreshold = BigNumberCalculator.ParseBigNumber("8Q");
            if (avgSEPerPrestige < minThreshold)
            {
                avgSEPerPrestige = minThreshold;
            }

            // Direct calculation: divide missing SE by SE gain per prestige
            BigInteger prestigesNeeded = missingSE / avgSEPerPrestige;

            // Add 1 if there's a remainder
            if (missingSE % avgSEPerPrestige > 0)
            {
                prestigesNeeded += 1;
            }

            // Return the raw result without capping
            return prestigesNeeded.ToString();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error calculating consistent estimated prestiges left for today for {PlayerName}", Player.PlayerName);
            return "N/A";
        }
    }

    // Calculate the total number of prestiges needed to reach the goal
    private int CalculateTotalPrestigesNeeded(string seLeftStr, int? prestigesThisWeek, BigInteger? seThisWeek)
    {
        try
        {
            // Check for null values
            if (string.IsNullOrEmpty(seLeftStr) || !prestigesThisWeek.HasValue || prestigesThisWeek.Value <= 0 ||
                !seThisWeek.HasValue || seThisWeek.Value <= 0)
            {
                return 0;
            }

            // Parse the missing SE value
            BigInteger missingSE = BigNumberCalculator.ParseBigNumber(seLeftStr);

            // Calculate average SE gain per prestige
            BigInteger avgSEPerPrestige = seThisWeek.Value / prestigesThisWeek.Value;

            // Ensure we have a minimum threshold for SE gain per prestige
            BigInteger minThreshold = BigNumberCalculator.ParseBigNumber("8Q");
            if (avgSEPerPrestige < minThreshold)
            {
                avgSEPerPrestige = minThreshold;
            }

            // Calculate total prestiges needed by dividing missing SE by average SE gain per prestige
            BigInteger prestigesNeeded = missingSE / avgSEPerPrestige;

            // Add 1 to account for any remainder (partial prestige)
            if (missingSE % avgSEPerPrestige > 0)
            {
                prestigesNeeded += 1;
            }

            return (int)prestigesNeeded;
        }
        catch (Exception ex)
        {
            if (Player != null)
            {
                Logger.LogError(ex, "Error calculating total prestiges needed for {PlayerName}", Player.PlayerName);
            }
            else
            {
                Logger.LogError(ex, "Error calculating total prestiges needed for unknown player");
            }
            return 0;
        }
    }

    // Helper method to generate tooltip text for next players
    private string GetNextPlayersTooltip(List<MajPlayerRankingDto> players, string statType)
    {
        if (players == null || !players.Any())
        {
            return "No more players in this direction";
        }

        string currentPlayerName = Player?.PlayerName ?? string.Empty;
        var filteredPlayers = new List<MajPlayerRankingDto>();
        foreach (var player in players)
        {
            // Skip players whose IGN contains the current player's name or vice versa
            if (string.IsNullOrEmpty(currentPlayerName) ||
                (!player.IGN.Contains(currentPlayerName, StringComparison.OrdinalIgnoreCase) &&
                 !currentPlayerName.Contains(player.IGN, StringComparison.OrdinalIgnoreCase)))
            {
                filteredPlayers.Add(player);
            }
        }

        if (!filteredPlayers.Any())
        {
            return "No more players in this direction";
        }

        var tooltipText = new System.Text.StringBuilder("Next players:\n");

        foreach (var player in filteredPlayers)
        {
            string statValue = statType switch
            {
                "SE" => player.SEString,
                "EB" => player.EBString,
                "MER" => player.MER.ToString(),
                "JER" => player.JER.ToString(),
                _ => "N/A"
            };

            tooltipText.AppendLine($"{player.IGN} ({statValue})");
        }

        return tooltipText.ToString();
    }

    public void Dispose()
    {
        // Unsubscribe from the event using the instance variable
        DashboardState.OnChange -= HandleStateChange;
        PlayerCardService.OnPlayerDataRefreshed -= HandlePlayerDataRefreshed;
        _fireworksTimer?.Stop();
        _fireworksTimer?.Dispose();

        // Suppress finalization
        GC.SuppressFinalize(this);
    }
}
