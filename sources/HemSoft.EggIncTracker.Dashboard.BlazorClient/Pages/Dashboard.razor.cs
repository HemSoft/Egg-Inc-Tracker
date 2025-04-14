using HemSoft.EggIncTracker.Domain;

namespace HemSoft.EggIncTracker.Dashboard.BlazorClient.Pages;

using System.Globalization;
using System.Numerics; // Added using statement for BigInteger

using HemSoft.EggIncTracker.Dashboard.BlazorClient.Services;

using HemSoft.EggIncTracker.Data;
using HemSoft.EggIncTracker.Data.Dtos;
// Remove Domain reference and use only API service
// using HemSoft.EggIncTracker.Domain;

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

using MudBlazor;

using ST = System.Timers;

public partial class Dashboard : IDisposable, IAsyncDisposable
{
    [Inject]
    private ILogger<Dashboard> Logger { get; set; } = default!;

    [Inject]
    private DashboardState DashboardState { get; set; } = default!;

    [Inject]
    private HemSoft.EggIncTracker.Dashboard.BlazorClient.Services.ApiService ApiService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private PlayerDataService PlayerDataService { get; set; } = default!; // Inject the new service

    private const int NameCutOff = 12;

    private ST.Timer? _timer;
    private ST.Timer? _updateTimer; // Timer for updating the "Last updated" text
    private DateTime _lastUpdated;
    private string _timeSinceLastUpdate = "Never";
    private bool _isRefreshing = false; // Added flag

    // Removed TitleProgressData and TitleProgressLabels fields for all players

    private PlayerDto? _kingFriday;
    private PlayerStatsDto? _kingFridayStats;
    private PlayerGoalDto? _kingFridayGoals;
    private int _kingFridayDaysToNextTitle = 0; // Added
    private MajPlayerRankingDto? _kingFridaySEGoalBegin; // Declared
    private MajPlayerRankingDto? _kingFridaySEGoalEnd;   // Declared
    private MajPlayerRankingDto? _kingFridayEBGoalBegin; // Declared
    private MajPlayerRankingDto? _kingFridayEBGoalEnd;   // Declared
    private MajPlayerRankingDto? _kingFridayMERGoalBegin;// Declared
    private MajPlayerRankingDto? _kingFridayMERGoalEnd;  // Declared
    private MajPlayerRankingDto? _kingFridayJERGoalBegin;// Declared
    private MajPlayerRankingDto? _kingFridayJERGoalEnd;  // Declared
    private MajPlayerRankingDto? _SEGoalBegin; // Note: These seem shared, might need refactoring later if goals differ per player card instance
    private MajPlayerRankingDto? _SEGoalEnd;
    private MajPlayerRankingDto? _EBGoalBegin;
    private MajPlayerRankingDto? _EBGoalEnd;
    private MajPlayerRankingDto? _MERGoalBegin;
    private MajPlayerRankingDto? _MERGoalEnd;
    private MajPlayerRankingDto? _JERGoalBegin;
    private MajPlayerRankingDto? _JERGoalEnd;
    // Removed _kingFridaySEThisWeek

    // Removed Progress bar percentage fields for King Friday

    private PlayerDto? _kingSaturday;
    private PlayerStatsDto? _kingSaturdayStats;
    private PlayerGoalDto? _kingSaturdayGoals;
    private int _kingSaturdayDaysToNextTitle = 0; // Added
    private MajPlayerRankingDto? _kingSaturdaySEGoalBegin;
    private MajPlayerRankingDto? _kingSaturdaySEGoalEnd;
    private MajPlayerRankingDto? _kingSaturdayEBGoalBegin;
    private MajPlayerRankingDto? _kingSaturdayEBGoalEnd;
    private MajPlayerRankingDto? _kingSaturdayMERGoalBegin;
    private MajPlayerRankingDto? _kingSaturdayMERGoalEnd;
    private MajPlayerRankingDto? _kingSaturdayJERGoalBegin;
    private MajPlayerRankingDto? _kingSaturdayJERGoalEnd;
    // Removed _kingSaturdaySEThisWeek

    // Removed Progress bar percentage fields for King Saturday

    private PlayerDto? _kingSunday;
    private PlayerStatsDto? _kingSundayStats;
    private PlayerGoalDto? _kingSundayGoals;
    private int _kingSundayDaysToNextTitle = 0; // Added
    private MajPlayerRankingDto? _kingSundaySEGoalBegin;
    private MajPlayerRankingDto? _kingSundaySEGoalEnd;
    private MajPlayerRankingDto? _kingSundayEBGoalBegin;
    private MajPlayerRankingDto? _kingSundayEBGoalEnd;
    private MajPlayerRankingDto? _kingSundayMERGoalBegin;
    private MajPlayerRankingDto? _kingSundayMERGoalEnd;
    private MajPlayerRankingDto? _kingSundayJERGoalBegin;
    private MajPlayerRankingDto? _kingSundayJERGoalEnd;
    // Removed _kingSundaySEThisWeek

    // Removed Progress bar percentage fields for King Sunday

    private PlayerDto? _kingMonday;
    private PlayerStatsDto? _kingMondayStats;
    private PlayerGoalDto? _kingMondayGoals;
    private int _kingMondayDaysToNextTitle = 0; // Added
    private MajPlayerRankingDto? _kingMondaySEGoalBegin;
    private MajPlayerRankingDto? _kingMondaySEGoalEnd;
    private MajPlayerRankingDto? _kingMondayEBGoalBegin;
    private MajPlayerRankingDto? _kingMondayEBGoalEnd;
    private MajPlayerRankingDto? _kingMondayMERGoalBegin;
    private MajPlayerRankingDto? _kingMondayMERGoalEnd;
    private MajPlayerRankingDto? _kingMondayJERGoalBegin;
    private MajPlayerRankingDto? _kingMondayJERGoalEnd;
    // Removed _kingMondaySEThisWeek

    // Removed Progress bar percentage fields for King Monday

    private readonly ChartOptions _options = new() // This seems unused now? Keep for now.
    {
        ChartPalette = new[] { "#4CAF50", "#666666" },
        ShowLegend = false,
        ShowToolTips = false,
        ShowLabels = false
    };

    // Removed fields related to historical chart

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await Task.Delay(300);
            Logger.LogInformation("Starting initial data load");
            await InitialDataLoad();
            SetupAutoRefreshTimer();
            Logger.LogInformation("Automatic refresh timer started");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during initialization");
            await RefreshData(); // Try to load some data even if init fails
            SetupAutoRefreshTimer(); // Still try to set up timer
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            Dispose();
            await Task.Delay(100);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            Logger.LogInformation("Dashboard component async disposal completed");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during async disposal of Dashboard component");
        }
    }

    private void SetupAutoRefreshTimer()
    {
        try
        {
            _timer?.Dispose(); // Dispose existing timer if any
            _updateTimer?.Dispose();

            _timer = new ST.Timer(30000);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Start();
            Logger.LogInformation("Main refresh timer started (30 second interval)");

            _updateTimer = new ST.Timer(1000);
            _updateTimer.Elapsed += OnUpdateTimerElapsed;
            _updateTimer.AutoReset = true;
            _updateTimer.Start();
            Logger.LogInformation("Update timer started (1 second interval)");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error setting up timers");
        }
    }

    private async void OnUpdateTimerElapsed(object? sender, ST.ElapsedEventArgs e)
    {
        try
        {
            if (_lastUpdated != default)
            {
                await InvokeAsync(() =>
                {
                    _timeSinceLastUpdate = GetTimeSinceLastUpdate();
                    StateHasChanged();
                });
            }
        }
        catch (ObjectDisposedException) { _updateTimer?.Stop(); Logger.LogInformation("Update timer stopped due to component disposal"); }
        catch (Exception ex) { Logger.LogError(ex, "Error updating time display"); }
    }

    private async void OnTimerElapsed(object? sender, ST.ElapsedEventArgs e)
    {
        try
        {
            if (_isRefreshing)
                return;
            await InvokeAsync(RefreshData);
        }
        catch (ObjectDisposedException) { _timer?.Stop(); Logger.LogInformation("Refresh timer stopped due to component disposal"); }
        catch (Exception ex) { Logger.LogError(ex, "Timer refresh error"); }
    }

    private async Task InitialDataLoad()
    {
        const int maxRetries = 3;
        Exception? lastException = null;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                Logger.LogInformation($"Initial data load attempt {attempt + 1} of {maxRetries}");
                _isRefreshing = true;
                StateHasChanged();

                var tasks = new List<Task>
                {
                    UpdateKingFriday(),
                    UpdateKingSaturday(),
                    UpdateKingSunday(),
                    UpdateKingMonday()
                    // Removed LoadKingFridaySEHistory() call
                };
                await Task.WhenAll(tasks);

                // Removed UpdateMultiSeriesChart() call

                _lastUpdated = DateTime.Now;
                _timeSinceLastUpdate = GetTimeSinceLastUpdate();
                await InvokeAsync(() => DashboardState.SetLastUpdated(DateTime.Now)); // Use InvokeAsync here

                _isRefreshing = false;
                StateHasChanged(); // Call StateHasChanged after all updates
                Logger.LogInformation("Initial data load completed successfully");
                return;
            }
            catch (Exception ex)
            {
                lastException = ex;
                Logger.LogError(ex, $"Error during initial data load (attempt {attempt + 1})");
                if (attempt < maxRetries - 1)
                {
                    int delayMs = 500 * (int)Math.Pow(2, attempt);
                    Logger.LogInformation($"Retrying in {delayMs}ms...");
                    await Task.Delay(delayMs);
                }
            }
        }
        Logger.LogError(lastException, "All initial data load attempts failed");
        _isRefreshing = false;
        StateHasChanged();
    }

    private async Task RefreshData()
    {
        if (_isRefreshing)
            return;

        bool isInitialLoad = _lastUpdated == default;
        _isRefreshing = true;
        if (!isInitialLoad)
            StateHasChanged();

        try
        {
            Logger.LogInformation("Starting data refresh");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            try
            {
                var tasks = new List<Task>
                {
                    UpdateKingFriday(),
                    UpdateKingSaturday(),
                    UpdateKingSunday(),
                    UpdateKingMonday()
                    // Removed LoadKingFridaySEHistory() call
                };
                await Task.WhenAll(tasks).WaitAsync(cts.Token); // Use WaitAsync for timeout
                Logger.LogInformation("All data loaded successfully");
                // Removed UpdateMultiSeriesChart() call
            }
            catch (OperationCanceledException) { Logger.LogWarning("Data refresh timed out or was canceled"); }

            _lastUpdated = DateTime.Now;
            _timeSinceLastUpdate = GetTimeSinceLastUpdate();
            await InvokeAsync(() => DashboardState.SetLastUpdated(DateTime.Now)); // Use InvokeAsync

            // Logger.LogInformation($"Progress bar values after refresh: SE={_kingFridaySEGoalPercentage:F1}%, EB={_kingFridayEBGoalPercentage:F1}%, MER={_kingFridayMERGoalPercentage:F1}%, JER={_kingFridayJERGoalPercentage:F1}%"); // Removed logging of percentages
            Logger.LogInformation("Data refresh completed");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error refreshing data: {Message}", ex.Message);
            if (ex.InnerException != null)
                Logger.LogError("Inner exception: {Message}", ex.InnerException.Message);
        }
        finally
        {
            _isRefreshing = false;
            StateHasChanged();
        }
    }

    private async Task UpdateKingFriday()
    {
        _kingFriday = await ApiService.GetLatestPlayerAsync("King Friday!");
        _kingFridayStats = await ApiService.GetRankedPlayerAsync("King Friday!", 1, 30);
        Logger.LogInformation($"Loaded King Friday with EID: {_kingFriday?.EID}");

        if (_kingFriday != null)
        {
            await InvokeAsync(() => DashboardState.SetPlayerLastUpdated("King Friday!", _kingFriday.Updated));
            var seThisWeek = await PlayerDataService.CalculateSEThisWeekAsync(_kingFriday);
            DashboardState.SetPlayerSEThisWeek("King Friday!", seThisWeek); // Store in state
            var (prestigesToday, prestigesThisWeek) = await PlayerDataService.CalculatePrestigesAsync(_kingFriday);
            _kingFriday.PrestigesToday = prestigesToday;
            _kingFriday.PrestigesThisWeek = prestigesThisWeek;

            // Calculate DaysToNextTitle
            string? projectedDateString = (await ApiService.GetTitleInfoAsync("King Friday!"))?.ProjectedTitleChange ?? _kingFridayStats?.ProjectedTitleChange;
            _kingFridayDaysToNextTitle = CalculateDaysToNextTitle(projectedDateString);
            // Removed setting TitleProgressData and TitleProgressLabels

            _kingFridayGoals = await ApiService.GetPlayerGoalsAsync("King Friday!");

            var sePlayers = await ApiService.GetSurroundingSEPlayersAsync("King Friday!", _kingFriday.SoulEggs);
            Logger.LogWarning($"King Friday SE Players: Lower={sePlayers?.LowerPlayer?.IGN}, Upper={sePlayers?.UpperPlayer?.IGN}");

            var ebPlayers = await ApiService.GetSurroundingEBPlayersAsync("King Friday!", _kingFriday.EarningsBonusPercentage);
            Logger.LogWarning($"King Friday EB Players: Lower={ebPlayers?.LowerPlayer?.IGN}, Upper={ebPlayers?.UpperPlayer?.IGN}");

            var merPlayers = await ApiService.GetSurroundingMERPlayersAsync("King Friday!", (decimal)_kingFriday.MER);
            Logger.LogWarning($"King Friday MER Players: Lower={merPlayers?.LowerPlayer?.IGN}, Upper={merPlayers?.UpperPlayer?.IGN}");

            var jerPlayers = await ApiService.GetSurroundingJERPlayersAsync("King Friday!", (decimal)_kingFriday.JER);
            Logger.LogWarning($"King Friday JER Players: Lower={jerPlayers?.LowerPlayer?.IGN}, Upper={jerPlayers?.UpperPlayer?.IGN}");

            // Set goal values with fallbacks if API returns null
            _kingFridaySEGoalBegin = sePlayers?.LowerPlayer ?? new MajPlayerRankingDto { IGN = "Default", SEString = "50.000s", Ranking = 2 };
            _kingFridaySEGoalEnd = sePlayers?.UpperPlayer ?? new MajPlayerRankingDto { IGN = "Default", SEString = "60.000s", Ranking = 0 };

            _kingFridayEBGoalBegin = ebPlayers?.LowerPlayer ?? new MajPlayerRankingDto { IGN = "Default", EBString = "10.000d%", Ranking = 2 };
            _kingFridayEBGoalEnd = ebPlayers?.UpperPlayer ?? new MajPlayerRankingDto { IGN = "Default", EBString = "15.000d%", Ranking = 0 };

            _kingFridayMERGoalBegin = merPlayers?.LowerPlayer ?? new MajPlayerRankingDto { IGN = "Default", MER = 40, Ranking = 2 };
            _kingFridayMERGoalEnd = merPlayers?.UpperPlayer ?? new MajPlayerRankingDto { IGN = "Default", MER = 45, Ranking = 0 };

            _kingFridayJERGoalBegin = jerPlayers?.LowerPlayer ?? new MajPlayerRankingDto { IGN = "Default", JER = 80, Ranking = 2 };
            _kingFridayJERGoalEnd = jerPlayers?.UpperPlayer ?? new MajPlayerRankingDto { IGN = "Default", JER = 90, Ranking = 0 };

            // Log the values for debugging
            Logger.LogWarning($"Final King Friday SE Goal Values: Begin={_kingFridaySEGoalBegin?.SEString}, End={_kingFridaySEGoalEnd?.SEString}");
            Logger.LogWarning($"Final King Friday EB Goal Values: Begin={_kingFridayEBGoalBegin?.EBString}, End={_kingFridayEBGoalEnd?.EBString}");
            Logger.LogWarning($"Final King Friday MER Goal Values: Begin={_kingFridayMERGoalBegin?.MER}, End={_kingFridayMERGoalEnd?.MER}");
            Logger.LogWarning($"Final King Friday JER Goal Values: Begin={_kingFridayJERGoalBegin?.JER}, End={_kingFridayJERGoalEnd?.JER}");

            // Set shared goal values with fallbacks if API returns null
            _SEGoalBegin = sePlayers?.LowerPlayer ?? new MajPlayerRankingDto { IGN = "Default", SEString = "50.000s", Ranking = 2 };
            _SEGoalEnd = sePlayers?.UpperPlayer ?? new MajPlayerRankingDto { IGN = "Default", SEString = "60.000s", Ranking = 0 };
            _EBGoalBegin = ebPlayers?.LowerPlayer ?? new MajPlayerRankingDto { IGN = "Default", EBString = "10.000d%", Ranking = 2 };
            _EBGoalEnd = ebPlayers?.UpperPlayer ?? new MajPlayerRankingDto { IGN = "Default", EBString = "15.000d%", Ranking = 0 };
            _MERGoalBegin = merPlayers?.LowerPlayer ?? new MajPlayerRankingDto { IGN = "Default", MER = 40, Ranking = 2 };
            _MERGoalEnd = merPlayers?.UpperPlayer ?? new MajPlayerRankingDto { IGN = "Default", MER = 45, Ranking = 0 };
            _JERGoalBegin = jerPlayers?.LowerPlayer ?? new MajPlayerRankingDto { IGN = "Default", JER = 80, Ranking = 2 };
            _JERGoalEnd = jerPlayers?.UpperPlayer ?? new MajPlayerRankingDto { IGN = "Default", JER = 90, Ranking = 0 };

            // Removed percentage calculations and 50% default logic

            StateHasChanged();
            await InvokeAsync(StateHasChanged);
        }
    }

    // Removed ParseBigNumberWrapper method

    private async Task UpdateKingSaturday()
    {
        _kingSaturday = await ApiService.GetLatestPlayerAsync("King Saturday!");
        _kingSaturdayStats = await ApiService.GetRankedPlayerAsync("King Saturday!", 1, 30);
        Logger.LogInformation($"Loaded King Saturday with EID: {_kingSaturday?.EID}");

        if (_kingSaturday != null)
        {
            await InvokeAsync(() => DashboardState.SetPlayerLastUpdated("King Saturday!", _kingSaturday.Updated));
            var seThisWeek = await PlayerDataService.CalculateSEThisWeekAsync(_kingSaturday);
            DashboardState.SetPlayerSEThisWeek("King Saturday!", seThisWeek); // Store in state
            var (prestigesToday, prestigesThisWeek) = await PlayerDataService.CalculatePrestigesAsync(_kingSaturday);
            _kingSaturday.PrestigesToday = prestigesToday;
            _kingSaturday.PrestigesThisWeek = prestigesThisWeek;

            // Calculate DaysToNextTitle
            string? projectedDateStringSat = (await ApiService.GetTitleInfoAsync("King Saturday!"))?.ProjectedTitleChange ?? _kingSaturdayStats?.ProjectedTitleChange;
            _kingSaturdayDaysToNextTitle = CalculateDaysToNextTitle(projectedDateStringSat);
            // Removed setting TitleProgressData and TitleProgressLabels

            _kingSaturdayGoals = await ApiService.GetPlayerGoalsAsync("King Saturday!");

            var sePlayers = await ApiService.GetSurroundingSEPlayersAsync("King Saturday!", _kingSaturday.SoulEggs);
            var ebPlayers = await ApiService.GetSurroundingEBPlayersAsync("King Saturday!", _kingSaturday.EarningsBonusPercentage);
            var merPlayers = await ApiService.GetSurroundingMERPlayersAsync("King Saturday!", (decimal)_kingSaturday.MER);
            var jerPlayers = await ApiService.GetSurroundingJERPlayersAsync("King Saturday!", (decimal)_kingSaturday.JER);

            _kingSaturdaySEGoalBegin = sePlayers?.LowerPlayer;
            _kingSaturdaySEGoalEnd = sePlayers?.UpperPlayer;
            _kingSaturdayEBGoalBegin = ebPlayers?.LowerPlayer;
            _kingSaturdayEBGoalEnd = ebPlayers?.UpperPlayer;
            _kingSaturdayMERGoalBegin = merPlayers?.LowerPlayer;
            _kingSaturdayMERGoalEnd = merPlayers?.UpperPlayer;
            _kingSaturdayJERGoalBegin = jerPlayers?.LowerPlayer;
            _kingSaturdayJERGoalEnd = jerPlayers?.UpperPlayer;

            // Removed percentage calculations and 50% default logic

            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task UpdateKingSunday()
    {
        _kingSunday = await ApiService.GetLatestPlayerAsync("King Sunday!");
        _kingSundayStats = await ApiService.GetRankedPlayerAsync("King Sunday!", 1, 30);
        Logger.LogInformation($"Loaded King Sunday with EID: {_kingSunday?.EID}");

        if (_kingSunday != null)
        {
            await InvokeAsync(() => DashboardState.SetPlayerLastUpdated("King Sunday!", _kingSunday.Updated));
            var seThisWeek = await PlayerDataService.CalculateSEThisWeekAsync(_kingSunday);
            DashboardState.SetPlayerSEThisWeek("King Sunday!", seThisWeek); // Store in state
            var (prestigesToday, prestigesThisWeek) = await PlayerDataService.CalculatePrestigesAsync(_kingSunday);
            _kingSunday.PrestigesToday = prestigesToday;
            _kingSunday.PrestigesThisWeek = prestigesThisWeek;

            // Calculate DaysToNextTitle
            string? projectedDateStringSun = (await ApiService.GetTitleInfoAsync("King Sunday!"))?.ProjectedTitleChange ?? _kingSundayStats?.ProjectedTitleChange;
            _kingSundayDaysToNextTitle = CalculateDaysToNextTitle(projectedDateStringSun);
            // Removed setting TitleProgressData and TitleProgressLabels

            _kingSundayGoals = await ApiService.GetPlayerGoalsAsync("King Sunday!");

            var sePlayers = await ApiService.GetSurroundingSEPlayersAsync("King Sunday!", _kingSunday.SoulEggs);
            var ebPlayers = await ApiService.GetSurroundingEBPlayersAsync("King Sunday!", _kingSunday.EarningsBonusPercentage);
            var merPlayers = await ApiService.GetSurroundingMERPlayersAsync("King Sunday!", (decimal)_kingSunday.MER);
            var jerPlayers = await ApiService.GetSurroundingJERPlayersAsync("King Sunday!", (decimal)_kingSunday.JER);

            _kingSundaySEGoalBegin = sePlayers?.LowerPlayer;
            _kingSundaySEGoalEnd = sePlayers?.UpperPlayer;
            _kingSundayEBGoalBegin = ebPlayers?.LowerPlayer;
            _kingSundayEBGoalEnd = ebPlayers?.UpperPlayer;
            _kingSundayMERGoalBegin = merPlayers?.LowerPlayer;
            _kingSundayMERGoalEnd = merPlayers?.UpperPlayer;
            _kingSundayJERGoalBegin = jerPlayers?.LowerPlayer;
            _kingSundayJERGoalEnd = jerPlayers?.UpperPlayer;

            // Removed percentage calculations and 50% default logic

            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task UpdateKingMonday()
    {
        _kingMonday = await ApiService.GetLatestPlayerAsync("King Monday!");
        _kingMondayStats = await ApiService.GetRankedPlayerAsync("King Monday!", 1, 30);
        Logger.LogInformation($"Loaded King Monday with EID: {_kingMonday?.EID}");

        if (_kingMonday != null)
        {
            await InvokeAsync(() => DashboardState.SetPlayerLastUpdated("King Monday!", _kingMonday.Updated));
            var seThisWeek = await PlayerDataService.CalculateSEThisWeekAsync(_kingMonday);
            DashboardState.SetPlayerSEThisWeek("King Monday!", seThisWeek); // Store in state
            var (prestigesToday, prestigesThisWeek) = await PlayerDataService.CalculatePrestigesAsync(_kingMonday);
            _kingMonday.PrestigesToday = prestigesToday;
            _kingMonday.PrestigesThisWeek = prestigesThisWeek;

            // Calculate DaysToNextTitle
            string? projectedDateStringMon = (await ApiService.GetTitleInfoAsync("King Monday!"))?.ProjectedTitleChange ?? _kingMondayStats?.ProjectedTitleChange;
            _kingMondayDaysToNextTitle = CalculateDaysToNextTitle(projectedDateStringMon);
            // Removed setting TitleProgressData and TitleProgressLabels

            _kingMondayGoals = await ApiService.GetPlayerGoalsAsync("King Monday!");

            var sePlayers = await ApiService.GetSurroundingSEPlayersAsync("King Monday!", _kingMonday.SoulEggs);
            var ebPlayers = await ApiService.GetSurroundingEBPlayersAsync("King Monday!", _kingMonday.EarningsBonusPercentage);
            var merPlayers = await ApiService.GetSurroundingMERPlayersAsync("King Monday!", (decimal)_kingMonday.MER);
            var jerPlayers = await ApiService.GetSurroundingJERPlayersAsync("King Monday!", (decimal)_kingMonday.JER);

            _kingMondaySEGoalBegin = sePlayers?.LowerPlayer;
            _kingMondaySEGoalEnd = sePlayers?.UpperPlayer;
            _kingMondayEBGoalBegin = ebPlayers?.LowerPlayer;
            _kingMondayEBGoalEnd = ebPlayers?.UpperPlayer;
            _kingMondayMERGoalBegin = merPlayers?.LowerPlayer;
            _kingMondayMERGoalEnd = merPlayers?.UpperPlayer;
            _kingMondayJERGoalBegin = jerPlayers?.LowerPlayer;
            _kingMondayJERGoalEnd = jerPlayers?.UpperPlayer;

            // Removed percentage calculations and 50% default logic

            await InvokeAsync(StateHasChanged);
        }
    }

    // Removed LoadKingFridaySEHistory method

    private string GetTimeSinceLastUpdate()
    {
        if (_lastUpdated == default)
            return "Never";
        var timeSince = DateTime.Now - _lastUpdated;
        if (timeSince.TotalSeconds < 60)
            return $"{(int)timeSince.TotalSeconds} seconds ago";
        if (timeSince.TotalMinutes < 60)
            return $"{(int)timeSince.TotalMinutes} minutes, {timeSince.Seconds:D} seconds ago";
        if (timeSince.TotalHours < 24)
            return $"{(int)timeSince.TotalHours} hours, {timeSince.Minutes:D} minutes ago";
        return $"{timeSince.Days:D} days, {timeSince.Hours:D} hours ago";
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

    // Removed InitializeTestData method
    // Removed UpdateMultiSeriesChart method
    // Removed OnDaysSliderChanged method

    private string GetLocalTimeString(DateTime utcTime)
    {
        DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.Local);
        if (localTime.Date == DateTime.Today)
            return $"Today {localTime:HH:mm}";
        if (localTime.Date == DateTime.Today.AddDays(-1))
            return $"Yesterday {localTime:HH:mm}";
        return localTime.ToString("MM/dd HH:mm");
    }

    private MudBlazor.Color CalculateProgressColor(int? actual, int goal)
    {
        if (actual == null || goal <= 0)
            return MudBlazor.Color.Default;
        double percentage = (double)actual.Value / goal * 100;
        if (percentage >= 100)
            return MudBlazor.Color.Success;
        if (percentage >= 75)
            return MudBlazor.Color.Info;
        if (percentage >= 50)
            return MudBlazor.Color.Warning;
        if (percentage >= 25)
            return MudBlazor.Color.Error;
        return MudBlazor.Color.Dark;
    }

    // Removed GetProgressColorStyle method

    private int CalculateDaysToNextTitle(string? projectedDateString)
    {
        if (string.IsNullOrEmpty(projectedDateString))
            return 0;

        if (DateTime.TryParse(projectedDateString, out DateTime projectedDate))
        {
            var daysUntil = (projectedDate - DateTime.Now).Days;
            return Math.Max(0, daysUntil); // Ensure it's not negative
        }
        Logger.LogWarning($"Could not parse projected title change date: {projectedDateString}");
        return 0;
    }


    private void NavigateToPlayerDetail(string? eid)
    {
        if (!string.IsNullOrEmpty(eid))
        {
            Logger.LogInformation($"Navigating to player detail for EID: {eid}");
            NavigationManager.NavigateTo($"/player/{eid}");
        }
        else
        {
            Logger.LogWarning("Cannot navigate to player detail: EID is null or empty");
        }
    }

    public void Dispose()
    {
        try
        {
            _timer?.Stop();
            _timer?.Dispose();
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
            DashboardState.OnChange -= StateHasChanged; // Unsubscribe

            // Clear references
            _kingFriday = _kingSaturday = _kingSunday = _kingMonday = null;
            _kingFridayStats = _kingSaturdayStats = _kingSundayStats = _kingMondayStats = null;
            _kingFridayGoals = _kingSaturdayGoals = _kingSundayGoals = _kingMondayGoals = null;
            _SEGoalBegin = _SEGoalEnd = _EBGoalBegin = _EBGoalEnd = _MERGoalBegin = _MERGoalEnd = _JERGoalBegin = _JERGoalEnd = null;
            _kingSaturdaySEGoalBegin = _kingSaturdaySEGoalEnd = _kingSaturdayEBGoalBegin = _kingSaturdayEBGoalEnd = _kingSaturdayMERGoalBegin = _kingSaturdayMERGoalEnd = _kingSaturdayJERGoalBegin = _kingSaturdayJERGoalEnd = null;
            _kingSundaySEGoalBegin = _kingSundaySEGoalEnd = _kingSundayEBGoalBegin = _kingSundayEBGoalEnd = _kingSundayMERGoalBegin = _kingSundayMERGoalEnd = _kingSundayJERGoalBegin = _kingSundayJERGoalEnd = null;
            _kingMondaySEGoalBegin = _kingMondaySEGoalEnd = _kingMondayEBGoalBegin = _kingMondayEBGoalEnd = _kingMondayMERGoalBegin = _kingMondayMERGoalEnd = _kingMondayJERGoalBegin = _kingMondayJERGoalEnd = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect(); // Second pass
            Logger.LogInformation("Dashboard component disposed, timers and event handlers cleaned up");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error disposing Dashboard component");
        }
    }
}
