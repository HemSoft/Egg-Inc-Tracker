namespace EggDash.Client.Pages;

using System.Globalization;

using EggDash.Client.Services;

using HemSoft.EggIncTracker.Data;
using HemSoft.EggIncTracker.Data.Dtos;
using HemSoft.EggIncTracker.Domain;

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

using MudBlazor;

using ST = System.Timers;

public partial class Dashboard
{
    [Inject]
    private ILogger<Dashboard> Logger { get; set; } = default!;

    [Inject]
    private EggIncContext DbContext { get; set; } = default!;

    [Inject]
    private DashboardState DashboardState { get; set; } = default!;


    private ST.Timer? _timer;
    private DateTime _lastUpdated;
    private string _timeSinceLastUpdate = "Never";

    private double[] _kingFridayTitleProgressData = Array.Empty<double>();
    private string[] _kingFridayTitleProgressLabels = Array.Empty<string>();

    private double[] _kingSaturdayTitleProgressData = Array.Empty<double>();
    private string[] _kingSaturdayTitleProgressLabels = Array.Empty<string>();

    private double[] _kingSundayTitleProgressData = Array.Empty<double>();
    private string[] _kingSundayTitleProgressLabels = Array.Empty<string>();

    private double[] _kingMondayTitleProgressData = Array.Empty<double>();
    private string[] _kingMondayTitleProgressLabels = Array.Empty<string>();

    private PlayerDto? _kingFriday;
    private PlayerStatsDto? _kingFridayStats;
    private MajPlayerRankingDto _SEGoalBegin;
    private MajPlayerRankingDto _SEGoalEnd;
    private MajPlayerRankingDto _EBGoalBegin;
    private MajPlayerRankingDto _EBGoalEnd;

    private PlayerDto? _kingSaturday;
    private PlayerStatsDto? _kingSaturdayStats;

    private PlayerDto? _kingSunday;
    private PlayerStatsDto? _kingSundayStats;

    private PlayerDto? _kingMonday;
    private PlayerStatsDto? _kingMondayStats;

    private readonly string _seGoalBaseline = "34.000s";
    private readonly string _ebGoalBaseline = "3.000d";
    private readonly string _merGoalBaseline = "39.900";
    private readonly string _jerGoalBaseline = "85.000";

    private readonly ChartOptions _options = new()
    {
        ChartPalette = new[] { "#4CAF50", "#666666" }
    };

    protected override async Task OnInitializedAsync()
    {
        await RefreshData();

        _timer = new ST.Timer(30000); // 30 seconds
        _timer.Elapsed += async (sender, e) =>
        {
            try
            {
                // Ensure we're on the UI thread
                await InvokeAsync(async () =>
                {
                    await RefreshData();
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Timer refresh error");
            }
        };
        _timer.AutoReset = true;
        _timer.Enabled = true;
    }

    private async Task RefreshData()
    {
        try
        {
            // Fetch new data
            await UpdateKingFriday();
            await UpdateKingSaturday();
            await UpdateKingSunday();
            await UpdateKingMonday();

            _lastUpdated = DateTime.Now;
            _timeSinceLastUpdate = GetTimeSinceLastUpdate();
            // Update the shared state instead of local _lastUpdated
            DashboardState.SetLastUpdated(DateTime.Now);

            // StateHasChanged is now called on the UI thread
            StateHasChanged();

        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error refreshing data");
        }
    }

    private async Task UpdateKingFriday()
    {
        // Get the UTC date boundaries that correspond to "today" and "start of week" in CST
        TimeZoneInfo cstZone;
        try
        {
            cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            cstZone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        }
        catch (InvalidTimeZoneException)
        {
            cstZone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        }

        // Get current date in CST
        var cstDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, cstZone);
        var cstToday = cstDateTime.Date;

        // Convert CST today back to UTC for database comparison
        var todayUtcStart = TimeZoneInfo.ConvertTimeToUtc(cstToday, cstZone);

        // Calculate start of week in CST, then convert to UTC
        var cstStartOfWeek = cstToday.AddDays(-(int)cstToday.DayOfWeek + (int)DayOfWeek.Monday);
        var startOfWeekUtc = TimeZoneInfo.ConvertTimeToUtc(cstStartOfWeek, cstZone);

        // Fetch data using DbContext
        _kingFriday = await DbContext.Players
            .OrderByDescending(x => x.Updated)
            .FirstOrDefaultAsync(x => x.PlayerName == "King Friday!");
        _kingFridayStats = await PlayerManager.GetRankedPlayersAsync("King Friday!", 1, 30, Logger);

        if (_kingFriday != null)
        {
            // Update the player's last update time in DashboardState
            DashboardState.SetPlayerLastUpdated(_kingFriday.Updated);

            var firstToday = await DbContext.Players
                .Where(x => x.PlayerName == "King Friday!" && x.Updated >= todayUtcStart)
                .OrderBy(x => x.Updated)
                .FirstOrDefaultAsync();

            var firstThisWeek = await DbContext.Players
                .Where(x => x.PlayerName == "King Friday!" && x.Updated >= startOfWeekUtc)
                .OrderBy(x => x.Updated)
                .FirstOrDefaultAsync();

            _kingFriday.PrestigesToday = _kingFriday.Prestiges - (firstToday?.Prestiges ?? _kingFriday.Prestiges);
            _kingFriday.PrestigesThisWeek = _kingFriday.Prestiges - (firstThisWeek?.Prestiges ?? _kingFriday.Prestiges);

            _kingFridayTitleProgressData = new double[] { _kingFriday.TitleProgress, 100 - _kingFriday.TitleProgress };
            _kingFridayTitleProgressLabels = new string[]
            {
            FormatTitleChangeLabel(DateTime.Parse(_kingFridayStats?.ProjectedTitleChange)),
            _kingFriday.NextTitle
            };

            (_SEGoalBegin, _SEGoalEnd) = MajPlayerRankingManager.GetSurroundingSEPlayers("King Friday!", _kingFriday.SoulEggs);
            (_EBGoalBegin, _EBGoalEnd) = MajPlayerRankingManager.GetSurroundingEBPlayers("King Friday!", _kingFriday.EarningsBonusPercentage);
        }
    }

    private async Task UpdateKingSaturday()
    {
        // Fetch data using DbContext
        _kingSaturday = await DbContext.Players
            .OrderByDescending(x => x.Updated)
            .FirstOrDefaultAsync(x => x.PlayerName == "King Saturday!");
        _kingSaturdayStats = await PlayerManager.GetRankedPlayersAsync("King Saturday!", 1, 30, Logger);

        _kingSaturdayTitleProgressData = new double[] { _kingSaturday.TitleProgress, 100 - _kingSaturday.TitleProgress };
        _kingSaturdayTitleProgressLabels = new string[]
        {
            FormatTitleChangeLabel(DateTime.Parse(_kingSaturdayStats.ProjectedTitleChange)),
            _kingSaturday.NextTitle
        };
    }

    private async Task UpdateKingSunday()
    {
        _kingSunday = await DbContext.Players
            .OrderByDescending(x => x.Updated)
            .FirstOrDefaultAsync(x => x.PlayerName == "King Sunday!");
        _kingSundayStats = await PlayerManager.GetRankedPlayersAsync("King Sunday!", 1, 30, Logger);

        _kingSundayTitleProgressData = new double[] { _kingSunday.TitleProgress, 100 - _kingSunday.TitleProgress };
        _kingSundayTitleProgressLabels = new string[]
        {
            FormatTitleChangeLabel(DateTime.Parse(_kingSundayStats.ProjectedTitleChange)),
            _kingSundayStats.NextTitle
        };
    }

    private async Task UpdateKingMonday()
    {
        _kingMonday = await DbContext.Players
            .OrderByDescending(x => x.Updated)
            .FirstOrDefaultAsync(x => x.PlayerName == "King Monday!");
        _kingMondayStats = await PlayerManager.GetRankedPlayersAsync("King Monday!", 1, 30, Logger);

        _kingMondayTitleProgressData = new double[] { _kingMonday.TitleProgress, 100 - _kingMonday.TitleProgress };
        _kingMondayTitleProgressLabels = new string[]
        {
            FormatTitleChangeLabel(DateTime.Parse(_kingMondayStats.ProjectedTitleChange)),
            _kingMondayStats.NextTitle
        };
    }

    private string GetTimeSinceLastUpdate()
    {
        var timeSince = DateTime.Now - _lastUpdated;

        if (timeSince.TotalSeconds < 60)
        {
            return $"{timeSince.Seconds:D} seconds ago";
        }

        if (timeSince.TotalMinutes < 60)
        {
            return $"{timeSince.Minutes:D} minutes, {timeSince.Seconds:D} seconds ago";
        }

        if (timeSince.TotalHours < 24)
        {
            return $"{timeSince.Hours:D} hours, {timeSince.Minutes:D} minutes, {timeSince.Seconds:D} seconds ago";
        }

        return $"{timeSince.Days:D} days, {timeSince.Hours:D} hours, {timeSince.Minutes:D} minutes ago";
    }

    private string FormatTitleChangeLabel(DateTime projectedDate)
    {
        var timeSpan = projectedDate - DateTime.Now;

        if (timeSpan.Days < 0)
            return "Title change overdue";

        if (timeSpan.Days == 0)
            return "Title change today";

        if (timeSpan.Days == 1)
            return "Next title tomorrow";

        return $"Next title in {timeSpan.Days} days";
    }

    private double CalculateProgressPercentage(string? current, string? goal, string? baseline = null)
    {
        try
        {
            // Handle null or empty inputs
            if (string.IsNullOrEmpty(current) || string.IsNullOrEmpty(goal))
            {
                return 0;
            }

            // Remove the 's' suffix, '%' character, and trim whitespace
            current = current.TrimEnd('d', 's', '%').Trim();
            goal = goal.TrimEnd('d', 's', '%').Trim();
            baseline = baseline?.TrimEnd('d', 's', '%').Trim();

            decimal currentValue, goalValue, baselineValue = 0;

            // Parse all values
            if (!TryParseAnyNumber(current, out currentValue) ||
                !TryParseAnyNumber(goal, out goalValue))
            {
                return 0;
            }

            // If baseline is provided, use it to adjust the calculation
            if (!string.IsNullOrEmpty(baseline) && TryParseAnyNumber(baseline, out baselineValue))
            {
                // Adjust current and goal values relative to baseline
                currentValue = Math.Max(0, currentValue - baselineValue);
                goalValue = Math.Max(0, goalValue - baselineValue);
            }

            // Avoid division by zero
            if (goalValue == 0)
            {
                return 0;
            }

            // Calculate percentage and ensure it doesn't exceed 100
            return Math.Min(100, (double)((currentValue / goalValue) * 100));
        }
        catch (Exception)
        {
            return 0;
        }
    }

    private bool TryParseAnyNumber(string input, out decimal result)
    {
        result = 0;

        // Try parsing as regular decimal first
        if (decimal.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out result))
        {
            return true;
        }

        // If that fails, try scientific notation
        return TryParseScientificNotation(input, out result);
    }

    private bool TryParseScientificNotation(string input, out decimal result)
    {
        result = 0;

        try
        {
            if (string.IsNullOrEmpty(input)) return false;

            // Split number and exponent
            string[] parts = input.Split(new[] { 'e' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2) return false;

            decimal baseNumber = decimal.Parse(parts[0], CultureInfo.InvariantCulture);
            int exponent = int.Parse(parts[1]);

            // Calculate the result using power of 10
            result = baseNumber * (decimal)Math.Pow(10, exponent);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private string GetLocalTimeString(DateTime utcTime)
    {
        // Convert UTC time to local time
        DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.Local);

        // Format for display with date if not today
        if (localTime.Date == DateTime.Today)
        {
            return $"Today {localTime.ToString("HH:mm")}";
        }
        else if (localTime.Date == DateTime.Today.AddDays(-1))
        {
            return $"Yesterday {localTime.ToString("HH:mm")}";
        }
        else
        {
            return localTime.ToString("MM/dd HH:mm");
        }
    }
}
