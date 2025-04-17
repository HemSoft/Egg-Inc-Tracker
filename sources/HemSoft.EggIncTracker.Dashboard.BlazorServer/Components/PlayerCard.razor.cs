namespace HemSoft.EggIncTracker.Dashboard.BlazorServer.Components;

using HemSoft.EggIncTracker.Dashboard.BlazorServer.Services;
using HemSoft.EggIncTracker.Dashboard.BlazorServer.Components.Pages;
using HemSoft.EggIncTracker.Data.Dtos;
using HemSoft.EggIncTracker.Domain;
using Microsoft.AspNetCore.Components;
using System.Globalization;
using System.Numerics;
using Timer = System.Timers.Timer;

public partial class PlayerCard : IDisposable
{
    [Parameter] public DashboardPlayer? DashboardPlayer { get; set; }
    [Parameter] public DateTime? LastUpdatedTimestamp { get; set; }

    private PlayerDto? Player => DashboardPlayer?.Player;
    private PlayerStatsDto? PlayerStats => DashboardPlayer?.Stats;
    private GoalDto? PlayerGoals => DashboardPlayer?.Goals;
    private MajPlayerRankingDto? SEGoalBegin => DashboardPlayer?.SEGoalBegin;
    private MajPlayerRankingDto? SEGoalEnd => DashboardPlayer?.SEGoalEnd;
    private MajPlayerRankingDto? EBGoalBegin => DashboardPlayer?.EBGoalBegin;
    private MajPlayerRankingDto? EBGoalEnd => DashboardPlayer?.EBGoalEnd;
    private MajPlayerRankingDto? MERGoalBegin => DashboardPlayer?.MERGoalBegin;
    private MajPlayerRankingDto? MERGoalEnd => DashboardPlayer?.MERGoalEnd;
    private MajPlayerRankingDto? JERGoalBegin => DashboardPlayer?.JERGoalBegin;
    private MajPlayerRankingDto? JERGoalEnd => DashboardPlayer?.JERGoalEnd;
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
        if (PlayerGoals?.WeeklySEGainGoal == null || string.IsNullOrEmpty(PlayerGoals.WeeklySEGainGoal) || !_localPlayerSEThisWeek.HasValue)
        {
            return false;
        }

        // Check SE This Week goal using BigInteger? for CalculateMissingSEGain
        var (isSEGoalMet, _, _, _) = PlayerDataService.CalculateMissingSEGain(_localPlayerSEThisWeek, PlayerGoals.WeeklySEGainGoal);

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
                Logger.LogDebug("PlayerCard updated from refresh event for {PlayerName}", playerName);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error handling player data refresh for {PlayerName}", playerName);
        }
    }

    // Removed SetupAutoRefreshTimer - now handled by PlayerCardService

    // Removed OnUpdateTimerElapsed - now handled by PlayerCardService

    // Removed OnRefreshTimerElapsed - now handled by PlayerCardService

    // Removed RefreshPlayerData - now handled by PlayerCardService

    // Removed InitialDataLoad - now handled by PlayerCardService

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

            Logger.LogInformation("All goals met for {PlayerName}! Showing fireworks for 30 seconds.", Player?.PlayerName);
        }
    }

    // Removed CreateEmptyDashboardPlayer - now handled by PlayerCardService

    // Removed GetTimeSinceLastUpdate - now handled by PlayerCardService

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
