using HemSoft.EggIncTracker.Domain;

namespace EggDash.Client.Pages;

using System.Globalization;

using EggDash.Client.Services;

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
    private EggDash.Client.Services.ApiService ApiService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    private const int NameCutOff = 12;

    private ST.Timer? _timer;
    private ST.Timer? _updateTimer; // Timer for updating the "Last updated" text
    private DateTime _lastUpdated;
    private string _timeSinceLastUpdate = "Never";

    private double[] _kingFridayTitleProgressData = new double[] { 0, 100 };
    private string[] _kingFridayTitleProgressLabels = new string[] { "", "" };

    private double[] _kingSaturdayTitleProgressData = new double[] { 0, 100 };
    private string[] _kingSaturdayTitleProgressLabels = new string[] { "", "" };

    private double[] _kingSundayTitleProgressData = new double[] { 0, 100 };
    private string[] _kingSundayTitleProgressLabels = new string[] { "", "" };

    private double[] _kingMondayTitleProgressData = new double[] { 0, 100 };
    private string[] _kingMondayTitleProgressLabels = new string[] { "", "" };

    private PlayerDto? _kingFriday;
    private PlayerStatsDto? _kingFridayStats;
    private PlayerGoalDto? _kingFridayGoals;
    private MajPlayerRankingDto? _SEGoalBegin;
    private MajPlayerRankingDto? _SEGoalEnd;
    private MajPlayerRankingDto? _EBGoalBegin;
    private MajPlayerRankingDto? _EBGoalEnd;
    private MajPlayerRankingDto? _MERGoalBegin;
    private MajPlayerRankingDto? _MERGoalEnd;
    private MajPlayerRankingDto? _JERGoalBegin;
    private MajPlayerRankingDto? _JERGoalEnd;
    private string _kingFridaySEThisWeek = string.Empty;

    // Progress bar percentages
    private double _kingFridaySEGoalPercentage = 0;
    private double _kingFridayEBGoalPercentage = 0;
    private double _kingFridayMERGoalPercentage = 0;
    private double _kingFridayJERGoalPercentage = 0;

    private PlayerDto? _kingSaturday;
    private PlayerStatsDto? _kingSaturdayStats;
    private PlayerGoalDto? _kingSaturdayGoals;

    private PlayerDto? _kingSunday;
    private PlayerStatsDto? _kingSundayStats;
    private PlayerGoalDto? _kingSundayGoals;

    private PlayerDto? _kingMonday;
    private PlayerStatsDto? _kingMondayStats;
    private PlayerGoalDto? _kingMondayGoals;

    private readonly ChartOptions _options = new()
    {
        ChartPalette = new[] { "#4CAF50", "#666666" },
        // Disable all interactive features to prevent JS interop issues
        ShowLegend = false,
        ShowToolTips = false,
        ShowLabels = false
    };

    private List<ChartSeries> _kingFridaySEHistorySeries = new List<ChartSeries>(Array.Empty<ChartSeries>());
    private string[] _kingFridaySEHistoryLabels = Array.Empty<string>();
    private double[] _kingFridaySEHistoryData = Array.Empty<double>();

    // New properties for the multi-series line chart
    private List<ChartSeries> _kingFridayMultiSeriesData = new();
    private string[] _kingFridayMultiSeriesLabels = Array.Empty<string>();
    private int _selectedDaysToDisplay = 7;
    private Dictionary<string, List<double>> _metricHistoryData = new();

    // Chart options with a custom palette for the Soul Eggs chart
    private readonly ChartOptions _multiSeriesChartOptions = new()
    {
        ChartPalette = new[] { "#4CAF50" }, // Green color for Soul Eggs
        InterpolationOption = InterpolationOption.Straight, // Use straight lines between points
        YAxisFormat = "0.##", // Format for Y-axis values
        XAxisLines = true, // Show X-axis grid lines
        YAxisLines = true, // Show Y-axis grid lines
        YAxisTicks = 1, // Increase number of ticks on Y-axis to show more granular values
        // Disable all interactive features to prevent JS interop issues
        ShowLabels = false,
        ShowLegend = false,
        ShowLegendLabels = false,
        ShowToolTips = false
    };

    protected override async Task OnInitializedAsync()
    {
        try
        {
            // First attempt to load data with a small delay to ensure all services are initialized
            await Task.Delay(300); // Short delay before initial load

            // Initial data load - use a longer timeout for the first load
            Logger.LogInformation("Starting initial data load");
            await InitialDataLoad();

            // Set up timer for automatic refreshes (30 seconds)
            SetupAutoRefreshTimer();
            Logger.LogInformation("Automatic refresh timer started");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during initialization");
            // Even if initialization fails, try to load some data
            await RefreshData();

            // Still try to set up the timer even if initial load fails
            SetupAutoRefreshTimer();
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            // Dispose of the component
            Dispose();

            // Wait for any pending JS interop calls to complete
            await Task.Delay(100);

            // Force garbage collection again after the delay
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
            // Dispose of any existing timers
            if (_timer != null)
            {
                _timer.Elapsed -= OnTimerElapsed;
                _timer.Stop();
                _timer.Dispose();
                _timer = null;
            }

            if (_updateTimer != null)
            {
                _updateTimer.Elapsed -= OnUpdateTimerElapsed;
                _updateTimer.Stop();
                _updateTimer.Dispose();
                _updateTimer = null;
            }

            // Create a new timer that refreshes every 30 seconds
            _timer = new ST.Timer(30000);
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Start();
            Logger.LogInformation("Main refresh timer started (30 second interval)");

            // Create a timer to update the "Last updated" text every second
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
            // Only update if we have a valid last updated time and we're not in the middle of a refresh
            if (_lastUpdated != default)
            {
                try
                {
                    await InvokeAsync(() =>
                    {
                        try
                        {
                            // Update the time since last update
                            _timeSinceLastUpdate = GetTimeSinceLastUpdate();
                            StateHasChanged();
                        }
                        catch (Exception innerEx)
                        {
                            // Log but don't rethrow to prevent timer from stopping
                            Logger.LogError(innerEx, "Error in UI update");
                        }
                    });
                }
                catch (ObjectDisposedException)
                {
                    // Component might have been disposed, stop the timer
                    _updateTimer?.Stop();
                    Logger.LogInformation("Update timer stopped due to component disposal");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating time display");
            // Don't rethrow to prevent timer from stopping
        }
    }

    private async void OnTimerElapsed(object? sender, ST.ElapsedEventArgs e)
    {
        try
        {
            // Skip if already refreshing
            if (_isRefreshing)
            {
                Logger.LogInformation("Skipping timer refresh as a refresh is already in progress");
                return;
            }

            try
            {
                // Ensure we're on the UI thread
                await InvokeAsync(async () =>
                {
                    try
                    {
                        // Only refresh if not already refreshing (double-check)
                        if (!_isRefreshing)
                        {
                            await RefreshData();
                        }
                    }
                    catch (Exception innerEx)
                    {
                        Logger.LogError(innerEx, "Error during refresh in timer");
                    }
                });
            }
            catch (ObjectDisposedException)
            {
                // Component might have been disposed, stop the timer
                _timer?.Stop();
                Logger.LogInformation("Refresh timer stopped due to component disposal");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Timer refresh error");
            // Don't rethrow to prevent timer from stopping
        }
    }

    private async Task InitialDataLoad()
    {
        // Special method for initial data load with multiple retries
        const int maxRetries = 3;
        Exception? lastException = null;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                Logger.LogInformation($"Initial data load attempt {attempt + 1} of {maxRetries}");
                _isRefreshing = true;
                StateHasChanged();

                // Load data for all kings in parallel to speed up initial load
                var tasks = new List<Task>
                {
                    UpdateKingFriday(),
                    UpdateKingSaturday(),
                    UpdateKingSunday(),
                    UpdateKingMonday(),
                    LoadKingFridaySEHistory()
                };

                await Task.WhenAll(tasks);

                // Update chart after all data is loaded
                await UpdateMultiSeriesChart();

                _lastUpdated = DateTime.Now;
                _timeSinceLastUpdate = GetTimeSinceLastUpdate();
                DashboardState.SetLastUpdated(DateTime.Now);

                // Ensure UI is updated after initial data load
                _isRefreshing = false;
                StateHasChanged();

                Logger.LogInformation("Initial data load completed successfully");
                return; // Success - exit the retry loop
            }
            catch (Exception ex)
            {
                lastException = ex;
                Logger.LogError(ex, $"Error during initial data load (attempt {attempt + 1})");

                if (attempt < maxRetries - 1)
                {
                    // Wait before retry with exponential backoff
                    int delayMs = 500 * (int)Math.Pow(2, attempt);
                    Logger.LogInformation($"Retrying in {delayMs}ms...");
                    await Task.Delay(delayMs);
                }
            }
        }

        // If we get here, all retries failed
        Logger.LogError(lastException, "All initial data load attempts failed");
        _isRefreshing = false;
        StateHasChanged();
    }

    private bool _isRefreshing = false;

    private async Task RefreshData()
    {
        if (_isRefreshing)
        {
            Logger.LogInformation("Refresh already in progress, skipping");
            return;
        }

        // Only show the full-screen loading indicator on initial load
        // For subsequent refreshes, we'll use a more subtle indicator
        bool isInitialLoad = _lastUpdated == default;
        _isRefreshing = true;

        // Only trigger a full UI update if this is not the initial load
        if (!isInitialLoad)
        {
            StateHasChanged(); // Update UI to show the subtle loading indicator
        }

        try
        {
            Logger.LogInformation("Starting data refresh");

            // Use a CancellationTokenSource with timeout to prevent hanging tasks
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(20)); // 20 second timeout

            try
            {
                // Create tasks for parallel execution to speed up the refresh
                var tasks = new List<Task>
                {
                    UpdateKingFriday(),
                    UpdateKingSaturday(),
                    UpdateKingSunday(),
                    UpdateKingMonday(),
                    LoadKingFridaySEHistory()
                };

                // Create a task that will complete when the timeout occurs
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(20), cts.Token);

                // Wait for either all tasks to complete or timeout to occur
                var allTasks = Task.WhenAll(tasks);
                var completedTask = await Task.WhenAny(allTasks, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    // Timeout occurred
                    Logger.LogWarning("Data refresh timed out after 20 seconds");
                    // Cancel the remaining tasks
                    cts.Cancel();
                }
                else
                {
                    // All tasks completed successfully
                    Logger.LogInformation("All data loaded successfully");
                    // Update chart after all data is loaded
                    await UpdateMultiSeriesChart();
                }
            }
            catch (TaskCanceledException)
            {
                Logger.LogWarning("Data refresh timed out after 20 seconds");
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Data refresh was canceled");
            }

            // Update timestamps regardless of timeout
            _lastUpdated = DateTime.Now;
            _timeSinceLastUpdate = GetTimeSinceLastUpdate();
            // Update the shared state instead of local _lastUpdated
            await InvokeAsync(() => DashboardState.SetLastUpdated(DateTime.Now));

            // Log progress bar values for debugging
            Logger.LogInformation($"Progress bar values after refresh: SE={_kingFridaySEGoalPercentage:F1}%, EB={_kingFridayEBGoalPercentage:F1}%, MER={_kingFridayMERGoalPercentage:F1}%, JER={_kingFridayJERGoalPercentage:F1}%");

            Logger.LogInformation("Data refresh completed");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error refreshing data: {Message}", ex.Message);
            // Log inner exception if available for more detailed diagnostics
            if (ex.InnerException != null)
            {
                Logger.LogError("Inner exception: {Message}", ex.InnerException.Message);
            }
        }
        finally
        {
            _isRefreshing = false;
            // Always update the UI after refresh completes
            StateHasChanged();
        }
    }

    private async Task UpdateKingFriday()
    {
        // Get the current time in local time instead of UTC to match the user's day
        var localNow = DateTime.Now;
        var localToday = new DateTime(localNow.Year, localNow.Month, localNow.Day, 0, 0, 0, DateTimeKind.Local);
        var todayUtcStart = localToday.ToUniversalTime(); // Convert to UTC for API calls

        // Calculate the start of the week (Monday) in local time, then convert to UTC
        // Get the days since last Monday (DayOfWeek.Monday = 1)
        int daysSinceMonday = ((int)localToday.DayOfWeek - 1 + 7) % 7; // Ensure positive result
        var startOfWeekLocal = localToday.AddDays(-daysSinceMonday);
        var startOfWeekUtc = startOfWeekLocal.ToUniversalTime();

        // Log for debugging purposes
        Logger.LogInformation($"Today (Local): {localToday}, Today (UTC): {todayUtcStart}");
        Logger.LogInformation($"Start of Week (Monday) (Local): {startOfWeekLocal}, (UTC): {startOfWeekUtc}");
        Logger.LogInformation($"Days since Monday: {daysSinceMonday}");

        _kingFriday = await ApiService.GetLatestPlayerAsync("King Friday!");
        // Replace direct call to PlayerManager with API service call
        _kingFridayStats = await ApiService.GetRankedPlayerAsync("King Friday!", 1, 30);

        Logger.LogInformation($"Loaded King Friday with EID: {_kingFriday?.EID}");

        if (_kingFriday != null)
        {
            // Update the player's last update time in DashboardState
            await InvokeAsync(() => DashboardState.SetPlayerLastUpdated(_kingFriday.Updated));

            // Get player history for today
            var playerHistoryToday = await ApiService.GetPlayerHistoryAsync(
                "King Friday!",
                todayUtcStart,
                DateTime.UtcNow);

            // Get player history for this week
            var playerHistoryWeek = await ApiService.GetPlayerHistoryAsync(
                "King Friday!",
                startOfWeekUtc,
                DateTime.UtcNow);

            // If weekly data is empty or null but we have daily data, use daily data for weekly calculation
            if ((playerHistoryWeek == null || !playerHistoryWeek.Any()) && playerHistoryToday != null && playerHistoryToday.Any())
            {
                Logger.LogInformation("No weekly history found, using daily history for weekly calculation");
                playerHistoryWeek = playerHistoryToday;
            }

            var firstToday = playerHistoryToday?.OrderBy(x => x.Updated).FirstOrDefault();
            var firstThisWeek = playerHistoryWeek?.OrderBy(x => x.Updated).FirstOrDefault();

            // Log the data we're working with
            Logger.LogInformation($"Player history today count: {playerHistoryToday?.Count() ?? 0}");
            Logger.LogInformation($"Player history week count: {playerHistoryWeek?.Count() ?? 0}");
            Logger.LogInformation($"First today: {firstToday?.Updated}, SE: {firstToday?.SoulEggs}");
            Logger.LogInformation($"First this week: {firstThisWeek?.Updated}, SE: {firstThisWeek?.SoulEggs}");
            Logger.LogInformation($"Current SE: {_kingFriday.SoulEggs}, Full: {_kingFriday.SoulEggsFull}");

            // Calculate SE gain this week
            if (playerHistoryWeek != null && playerHistoryWeek.Any())
            {
                var latestSE = _kingFriday.SoulEggsFull;
                var earliestSE = firstThisWeek?.SoulEggsFull ?? latestSE;

                Logger.LogInformation($"Using latest SE: {latestSE}");
                Logger.LogInformation($"Using earliest SE: {earliestSE}");

                try
                {
                    // Use BigNumberCalculator to calculate the difference
                    _kingFridaySEThisWeek = HemSoft.EggIncTracker.Domain.BigNumberCalculator.CalculateDifference(earliestSE, latestSE);
                    Logger.LogInformation($"Calculated SE gain this week: {_kingFridaySEThisWeek}");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error calculating SE gain this week");
                    _kingFridaySEThisWeek = "0";
                }
            }
            else
            {
                Logger.LogWarning("No player history for this week, setting SE gain to 0");
                _kingFridaySEThisWeek = "0";
            }

            // Log the data we're working with
            Logger.LogInformation($"Current prestiges: {_kingFriday.Prestiges}");
            Logger.LogInformation($"First today prestiges: {firstToday?.Prestiges}, timestamp: {firstToday?.Updated}");
            Logger.LogInformation($"First this week prestiges: {firstThisWeek?.Prestiges}, timestamp: {firstThisWeek?.Updated}");

            _kingFriday.PrestigesToday = _kingFriday.Prestiges - (firstToday?.Prestiges ?? _kingFriday.Prestiges);
            _kingFriday.PrestigesThisWeek = _kingFriday.Prestiges - (firstThisWeek?.Prestiges ?? _kingFriday.Prestiges);

            // Log calculated values
            Logger.LogInformation($"Calculated prestiges today: {_kingFriday.PrestigesToday}");
            Logger.LogInformation($"Calculated prestiges this week: {_kingFriday.PrestigesThisWeek}");

            // Ensure PrestigesThisWeek is at least equal to PrestigesToday and handle null values
            if (_kingFriday.PrestigesToday.HasValue)
            {
                // If PrestigesThisWeek is null or less than PrestigesToday, set it to PrestigesToday
                if (!_kingFriday.PrestigesThisWeek.HasValue || _kingFriday.PrestigesThisWeek.Value < _kingFriday.PrestigesToday.Value)
                {
                    _kingFriday.PrestigesThisWeek = _kingFriday.PrestigesToday;
                    Logger.LogInformation($"Applied fix for weekly prestiges: updated to {_kingFriday.PrestigesThisWeek}");
                }
            }

            // Get title information from the API
            var titleInfo = await ApiService.GetTitleInfoAsync("King Friday!");
            if (titleInfo != null)
            {
                _kingFridayTitleProgressData = new double[] { titleInfo.TitleProgress, 100 - titleInfo.TitleProgress };
                _kingFridayTitleProgressLabels = new string[]
                {
                    FormatTitleChangeLabel(DateTime.Parse(titleInfo.ProjectedTitleChange)),
                    titleInfo.NextTitle
                };
            }
            else
            {
                // Fallback if API call fails
                _kingFridayTitleProgressData = new double[] { _kingFriday.TitleProgress, 100 - _kingFriday.TitleProgress };
                _kingFridayTitleProgressLabels = new string[]
                {
                    FormatTitleChangeLabel(DateTime.Parse(_kingFridayStats?.ProjectedTitleChange)),
                    _kingFriday.NextTitle
                };
            }

            // Get player goals
            _kingFridayGoals = await ApiService.GetPlayerGoalsAsync("King Friday!");
            Logger.LogInformation($"Retrieved goals for King Friday: DailyPrestigeGoal={_kingFridayGoals?.DailyPrestigeGoal}");

            // Update goal data using ApiService directly instead of MajPlayerRankingManager
            var sePlayers = await ApiService.GetSurroundingSEPlayersAsync("King Friday!", _kingFriday.SoulEggs);
            var ebPlayers = await ApiService.GetSurroundingEBPlayersAsync("King Friday!", _kingFriday.EarningsBonusPercentage);
            var merPlayers = await ApiService.GetSurroundingMERPlayersAsync("King Friday!", (decimal)_kingFriday.MER);
            var jerPlayers = await ApiService.GetSurroundingJERPlayersAsync("King Friday!", (decimal)_kingFriday.JER);

            // Set the goal data from the API responses
            _SEGoalBegin = sePlayers?.LowerPlayer;
            _SEGoalEnd = sePlayers?.UpperPlayer;
            _EBGoalBegin = ebPlayers?.LowerPlayer;
            _EBGoalEnd = ebPlayers?.UpperPlayer;
            _MERGoalBegin = merPlayers?.LowerPlayer;
            _MERGoalEnd = merPlayers?.UpperPlayer;
            _JERGoalBegin = jerPlayers?.LowerPlayer;
            _JERGoalEnd = jerPlayers?.UpperPlayer;

            // Calculate progress bar percentages
            var sePercentage = CalculateProgressPercentage(_kingFridayStats?.SE, _SEGoalEnd?.SEString, _SEGoalBegin?.SEString);
            var ebPercentage = CalculateProgressPercentage(_kingFridayStats?.EB, _EBGoalEnd?.EBString, _EBGoalBegin?.EBString);
            var merPercentage = CalculateProgressPercentage(_kingFridayStats?.MER.ToString(), _MERGoalEnd?.MER.ToString(), _MERGoalBegin?.MER.ToString());
            var jerPercentage = CalculateProgressPercentage(_kingFridayStats?.JER.ToString(), _JERGoalEnd?.JER.ToString(), _JERGoalBegin?.JER.ToString());

            // Ensure we have valid percentages (not zero)
            _kingFridaySEGoalPercentage = sePercentage > 0 ? sePercentage : 50; // Default to 50% if calculation fails
            _kingFridayEBGoalPercentage = ebPercentage > 0 ? ebPercentage : 50;
            _kingFridayMERGoalPercentage = merPercentage > 0 ? merPercentage : 50;
            _kingFridayJERGoalPercentage = jerPercentage > 0 ? jerPercentage : 50;

            Logger.LogInformation($"Progress bar percentages calculated: SE={_kingFridaySEGoalPercentage:F1}%, EB={_kingFridayEBGoalPercentage:F1}%, MER={_kingFridayMERGoalPercentage:F1}%, JER={_kingFridayJERGoalPercentage:F1}%");

            // Force UI update after calculating progress bar percentages
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task UpdateKingSaturday()
    {
        // Get the current time in local time instead of UTC to match the user's day
        var localNow = DateTime.Now;
        var localToday = new DateTime(localNow.Year, localNow.Month, localNow.Day, 0, 0, 0, DateTimeKind.Local);
        var todayUtcStart = localToday.ToUniversalTime(); // Convert to UTC for API calls

        // Calculate the start of the week (Monday) in local time, then convert to UTC
        int daysSinceMonday = ((int)localToday.DayOfWeek - 1 + 7) % 7; // Ensure positive result
        var startOfWeekLocal = localToday.AddDays(-daysSinceMonday);
        var startOfWeekUtc = startOfWeekLocal.ToUniversalTime();

        _kingSaturday = await ApiService.GetLatestPlayerAsync("King Saturday!");
        // Replace direct call to PlayerManager with API service call
        _kingSaturdayStats = await ApiService.GetRankedPlayerAsync("King Saturday!", 1, 30);

        Logger.LogInformation($"Loaded King Saturday with EID: {_kingSaturday?.EID}");

        if (_kingSaturday != null)
        {
            // Get title information from the API
            var titleInfo = await ApiService.GetTitleInfoAsync("King Saturday!");
            if (titleInfo != null)
            {
                _kingSaturdayTitleProgressData = new double[] { titleInfo.TitleProgress, 100 - titleInfo.TitleProgress };
                _kingSaturdayTitleProgressLabels = new string[]
                {
                    FormatTitleChangeLabel(DateTime.Parse(titleInfo.ProjectedTitleChange)),
                    titleInfo.NextTitle
                };
            }
            else
            {
                // Fallback if API call fails
                _kingSaturdayTitleProgressData = new double[] { _kingSaturday.TitleProgress, 100 - _kingSaturday.TitleProgress };
                _kingSaturdayTitleProgressLabels = new string[]
                {
                    FormatTitleChangeLabel(DateTime.Parse(_kingSaturdayStats?.ProjectedTitleChange)),
                    _kingSaturday.NextTitle
                };
            }

            // Get player goals
            _kingSaturdayGoals = await ApiService.GetPlayerGoalsAsync("King Saturday!");
            Logger.LogInformation($"Retrieved goals for King Saturday: DailyPrestigeGoal={_kingSaturdayGoals?.DailyPrestigeGoal}");

            // Get player history for today and this week
            var playerHistoryToday = await ApiService.GetPlayerHistoryAsync(
                "King Saturday!",
                todayUtcStart,
                DateTime.UtcNow);

            var playerHistoryWeek = await ApiService.GetPlayerHistoryAsync(
                "King Saturday!",
                startOfWeekUtc,
                DateTime.UtcNow);

            // If weekly data is empty or null but we have daily data, use daily data for weekly calculation
            if ((playerHistoryWeek == null || !playerHistoryWeek.Any()) && playerHistoryToday != null && playerHistoryToday.Any())
            {
                playerHistoryWeek = playerHistoryToday;
            }

            var firstToday = playerHistoryToday?.OrderBy(x => x.Updated).FirstOrDefault();
            var firstThisWeek = playerHistoryWeek?.OrderBy(x => x.Updated).FirstOrDefault();

            // Calculate prestige counts
            if (_kingSaturday.Prestiges.HasValue)
            {
                _kingSaturday.PrestigesToday = _kingSaturday.Prestiges - (firstToday?.Prestiges ?? _kingSaturday.Prestiges);
                _kingSaturday.PrestigesThisWeek = _kingSaturday.Prestiges - (firstThisWeek?.Prestiges ?? _kingSaturday.Prestiges);

                // Ensure PrestigesThisWeek is at least equal to PrestigesToday
                if (_kingSaturday.PrestigesToday.HasValue &&
                    (!_kingSaturday.PrestigesThisWeek.HasValue || _kingSaturday.PrestigesThisWeek.Value < _kingSaturday.PrestigesToday.Value))
                {
                    _kingSaturday.PrestigesThisWeek = _kingSaturday.PrestigesToday;
                }
            }
        }
    }

    private async Task UpdateKingSunday()
    {
        // Get the current time in local time instead of UTC to match the user's day
        var localNow = DateTime.Now;
        var localToday = new DateTime(localNow.Year, localNow.Month, localNow.Day, 0, 0, 0, DateTimeKind.Local);
        var todayUtcStart = localToday.ToUniversalTime(); // Convert to UTC for API calls

        // Calculate the start of the week (Monday) in local time, then convert to UTC
        int daysSinceMonday = ((int)localToday.DayOfWeek - 1 + 7) % 7; // Ensure positive result
        var startOfWeekLocal = localToday.AddDays(-daysSinceMonday);
        var startOfWeekUtc = startOfWeekLocal.ToUniversalTime();

        _kingSunday = await ApiService.GetLatestPlayerAsync("King Sunday!");
        // Replace direct call to PlayerManager with API service call
        _kingSundayStats = await ApiService.GetRankedPlayerAsync("King Sunday!", 1, 30);

        Logger.LogInformation($"Loaded King Sunday with EID: {_kingSunday?.EID}");

        if (_kingSunday != null)
        {
            // Get title information from the API
            var titleInfo = await ApiService.GetTitleInfoAsync("King Sunday!");
            if (titleInfo != null)
            {
                _kingSundayTitleProgressData = new double[] { titleInfo.TitleProgress, 100 - titleInfo.TitleProgress };
                _kingSundayTitleProgressLabels = new string[]
                {
                    FormatTitleChangeLabel(DateTime.Parse(titleInfo.ProjectedTitleChange)),
                    titleInfo.NextTitle
                };
            }
            else
            {
                // Fallback if API call fails
                _kingSundayTitleProgressData = new double[] { _kingSunday.TitleProgress, 100 - _kingSunday.TitleProgress };
                _kingSundayTitleProgressLabels = new string[]
                {
                    FormatTitleChangeLabel(DateTime.Parse(_kingSundayStats?.ProjectedTitleChange)),
                    _kingSunday.NextTitle
                };
            }

            // Get player goals
            _kingSundayGoals = await ApiService.GetPlayerGoalsAsync("King Sunday!");
            Logger.LogInformation($"Retrieved goals for King Sunday: DailyPrestigeGoal={_kingSundayGoals?.DailyPrestigeGoal}");

            // Get player history for today and this week
            var playerHistoryToday = await ApiService.GetPlayerHistoryAsync(
                "King Sunday!",
                todayUtcStart,
                DateTime.UtcNow);

            var playerHistoryWeek = await ApiService.GetPlayerHistoryAsync(
                "King Sunday!",
                startOfWeekUtc,
                DateTime.UtcNow);

            // If weekly data is empty or null but we have daily data, use daily data for weekly calculation
            if ((playerHistoryWeek == null || !playerHistoryWeek.Any()) && playerHistoryToday != null && playerHistoryToday.Any())
            {
                playerHistoryWeek = playerHistoryToday;
            }

            var firstToday = playerHistoryToday?.OrderBy(x => x.Updated).FirstOrDefault();
            var firstThisWeek = playerHistoryWeek?.OrderBy(x => x.Updated).FirstOrDefault();

            // Calculate prestige counts
            if (_kingSunday.Prestiges.HasValue)
            {
                _kingSunday.PrestigesToday = _kingSunday.Prestiges - (firstToday?.Prestiges ?? _kingSunday.Prestiges);
                _kingSunday.PrestigesThisWeek = _kingSunday.Prestiges - (firstThisWeek?.Prestiges ?? _kingSunday.Prestiges);

                // Ensure PrestigesThisWeek is at least equal to PrestigesToday
                if (_kingSunday.PrestigesToday.HasValue &&
                    (!_kingSunday.PrestigesThisWeek.HasValue || _kingSunday.PrestigesThisWeek.Value < _kingSunday.PrestigesToday.Value))
                {
                    _kingSunday.PrestigesThisWeek = _kingSunday.PrestigesToday;
                }
            }
        }
    }

    private async Task UpdateKingMonday()
    {
        // Get the current time in local time instead of UTC to match the user's day
        var localNow = DateTime.Now;
        var localToday = new DateTime(localNow.Year, localNow.Month, localNow.Day, 0, 0, 0, DateTimeKind.Local);
        var todayUtcStart = localToday.ToUniversalTime(); // Convert to UTC for API calls

        // Calculate the start of the week (Monday) in local time, then convert to UTC
        int daysSinceMonday = ((int)localToday.DayOfWeek - 1 + 7) % 7; // Ensure positive result
        var startOfWeekLocal = localToday.AddDays(-daysSinceMonday);
        var startOfWeekUtc = startOfWeekLocal.ToUniversalTime();

        _kingMonday = await ApiService.GetLatestPlayerAsync("King Monday!");
        // Replace direct call to PlayerManager with API service call
        _kingMondayStats = await ApiService.GetRankedPlayerAsync("King Monday!", 1, 30);

        Logger.LogInformation($"Loaded King Monday with EID: {_kingMonday?.EID}");

        if (_kingMonday != null)
        {
            // Get title information from the API
            var titleInfo = await ApiService.GetTitleInfoAsync("King Monday!");
            if (titleInfo != null)
            {
                _kingMondayTitleProgressData = new double[] { titleInfo.TitleProgress, 100 - titleInfo.TitleProgress };
                _kingMondayTitleProgressLabels = new string[]
                {
                    FormatTitleChangeLabel(DateTime.Parse(titleInfo.ProjectedTitleChange)),
                    titleInfo.NextTitle
                };
            }
            else
            {
                // Fallback if API call fails
                _kingMondayTitleProgressData = new double[] { _kingMonday.TitleProgress, 100 - _kingMonday.TitleProgress };
                _kingMondayTitleProgressLabels = new string[]
                {
                    FormatTitleChangeLabel(DateTime.Parse(_kingMondayStats?.ProjectedTitleChange)),
                    _kingMonday.NextTitle
                };
            }

            // Get player goals
            _kingMondayGoals = await ApiService.GetPlayerGoalsAsync("King Monday!");
            Logger.LogInformation($"Retrieved goals for King Monday: DailyPrestigeGoal={_kingMondayGoals?.DailyPrestigeGoal}");

            // Get player history for today and this week
            var playerHistoryToday = await ApiService.GetPlayerHistoryAsync(
                "King Monday!",
                todayUtcStart,
                DateTime.UtcNow);

            var playerHistoryWeek = await ApiService.GetPlayerHistoryAsync(
                "King Monday!",
                startOfWeekUtc,
                DateTime.UtcNow);

            // If weekly data is empty or null but we have daily data, use daily data for weekly calculation
            if ((playerHistoryWeek == null || !playerHistoryWeek.Any()) && playerHistoryToday != null && playerHistoryToday.Any())
            {
                playerHistoryWeek = playerHistoryToday;
            }

            var firstToday = playerHistoryToday?.OrderBy(x => x.Updated).FirstOrDefault();
            var firstThisWeek = playerHistoryWeek?.OrderBy(x => x.Updated).FirstOrDefault();

            // Calculate prestige counts
            if (_kingMonday.Prestiges.HasValue)
            {
                _kingMonday.PrestigesToday = _kingMonday.Prestiges - (firstToday?.Prestiges ?? _kingMonday.Prestiges);
                _kingMonday.PrestigesThisWeek = _kingMonday.Prestiges - (firstThisWeek?.Prestiges ?? _kingMonday.Prestiges);

                // Ensure PrestigesThisWeek is at least equal to PrestigesToday
                if (_kingMonday.PrestigesToday.HasValue &&
                    (!_kingMonday.PrestigesThisWeek.HasValue || _kingMonday.PrestigesThisWeek.Value < _kingMonday.PrestigesToday.Value))
                {
                    _kingMonday.PrestigesThisWeek = _kingMonday.PrestigesToday;
                }
            }
        }
    }

    private async Task LoadKingFridaySEHistory()
    {
        try
        {
            // Calculate date range (14 days ago until now)
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-14);

            // Get historical player data for King Friday
            var historicalData = await ApiService.GetPlayerHistoryAsync(
                "King Friday!",
                startDate,
                endDate);

            if (historicalData?.Any() == true)
            {
                // Format the data for the chart
                _kingFridaySEHistoryLabels = historicalData
                    .Select(p => p.Updated.ToString("MM/dd"))
                    .ToArray();

                // Convert SE values to numeric values for the chart
                _kingFridaySEHistoryData = historicalData
                    .Select(p => ParseSoulEggs(p.SoulEggs))
                    .ToArray();

                // Create the chart series
                _kingFridaySEHistorySeries =
                [
                    new ChartSeries
                    {
                        Name = "Soul Eggs",
                        Data = _kingFridaySEHistoryData
                    }
                ];
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading King Friday SE history data");
        }
    }

    private double ParseSoulEggs(string? soulEggs)
    {
        if (string.IsNullOrEmpty(soulEggs))
            return 0;

        // Remove any suffix like 'Q' for quintillion, etc.
        var cleanedValue = soulEggs;
        var suffixes = new[] { 'Q', 'q', 's', 'S', 'T', 't', 'B', 'b', 'M', 'm' };
        foreach (var suffix in suffixes)
        {
            cleanedValue = cleanedValue.TrimEnd(suffix);
        }

        // Try to parse using the existing number parsing method
        if (TryParseAnyNumber(cleanedValue, out decimal result))
        {
            return (double)result;
        }

        // Fallback to simpler parsing
        if (double.TryParse(cleanedValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double simpleResult))
        {
            return simpleResult;
        }

        return 0;
    }

    private string GetTimeSinceLastUpdate()
    {
        // If _lastUpdated is default, return "Never"
        if (_lastUpdated == default)
        {
            return "Never";
        }

        var timeSince = DateTime.Now - _lastUpdated;

        if (timeSince.TotalSeconds < 60)
        {
            return $"{(int)timeSince.TotalSeconds} seconds ago";
        }

        if (timeSince.TotalMinutes < 60)
        {
            return $"{(int)timeSince.TotalMinutes} minutes, {timeSince.Seconds:D} seconds ago";
        }

        if (timeSince.TotalHours < 24)
        {
            return $"{(int)timeSince.TotalHours} hours, {timeSince.Minutes:D} minutes ago";
        }

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
            if (string.IsNullOrEmpty(input))
                return false;

            // Split number and exponent
            string[] parts = input.Split(new[] { 'e' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
                return false;

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

    /// <summary>
    /// Calculate the color based on the progress percentage
    /// </summary>
    /// <param name="actual">The actual value</param>
    /// <param name="goal">The goal value</param>
    /// <returns>A MudBlazor Color enum value</returns>
    private MudBlazor.Color CalculateProgressColor(int? actual, int goal)
    {
        if (actual == null || goal <= 0)
            return MudBlazor.Color.Default;

        double percentage = (double)actual.Value / goal * 100;

        if (percentage >= 100)
            return MudBlazor.Color.Success; // Green for meeting or exceeding goal
        else if (percentage >= 75)
            return MudBlazor.Color.Info;    // Blue for 75-99%
        else if (percentage >= 50)
            return MudBlazor.Color.Warning; // Yellow/Orange for 50-74%
        else if (percentage >= 25)
            return MudBlazor.Color.Error;   // Red for 25-49%
        else
            return MudBlazor.Color.Dark;    // Dark red for < 25%
    }

    /// <summary>
    /// Get the CSS color string based on the progress percentage
    /// </summary>
    /// <param name="actual">The actual value</param>
    /// <param name="goal">The goal value</param>
    /// <returns>A CSS color string</returns>
    private string GetProgressColorStyle(int? actual, int goal)
    {
        if (actual == null || goal <= 0)
            return "color: white";

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

    // Generate test data for the chart - only used as a fallback when real data isn't available
    private void InitializeTestData()
    {
        Logger.LogInformation("Using test data for Soul Eggs chart");

        // Create mock data for the last 7 days
        var today = DateTime.Now;
        var dates = Enumerable.Range(0, 7)
            .Select(i => today.AddDays(-i))
            .Reverse()
            .ToArray();

        _kingFridayMultiSeriesLabels = dates.Select(d => d.ToString("MM/dd")).ToArray();

        // Initialize the metrics dictionary with random test data for Soul Eggs only
        _metricHistoryData = new Dictionary<string, List<double>>
        {
            // Soul Eggs (increasing trend)
            ["Soul Eggs"] = new List<double> { 100, 110, 125, 145, 160, 185, 210 }
        };

        // Create chart series directly
        _kingFridayMultiSeriesData = new List<ChartSeries>
        {
            new ChartSeries
            {
                Name = "Soul Eggs",
                Data = _metricHistoryData["Soul Eggs"].ToArray()
            }
        };

        // Update labels
        _kingFridayMultiSeriesLabels = dates.Select(d => d.ToString("MM/dd")).ToArray();
    }

    // Update the chart based on the selected number of days
    private async Task UpdateMultiSeriesChart()
    {
        try
        {
            // Calculate date range (30 days ago until now)
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-30); // Get data for up to 30 days

            // Get historical player data for King Friday directly from the API
            var historicalData = await ApiService.GetPlayerHistoryAsync(
                "King Friday!",
                startDate,
                endDate);

            if (historicalData?.Any() == true)
            {
                // Process actual historical data
                // First, ensure the data is sorted by date
                var sortedData = historicalData
                    .OrderBy(p => p.Updated)
                    .ToList();

                // Group data by day and take only the last entry for each day
                var groupedByDay = sortedData
                    .GroupBy(p => p.Updated.Date)
                    .Select(g => g.OrderByDescending(p => p.Updated).First())
                    .OrderBy(p => p.Updated)
                    .ToList();

                Logger.LogInformation("Reduced data points from {OriginalCount} to {GroupedCount} (one per day)",
                    sortedData.Count, groupedByDay.Count);

                // Extract the dates for the x-axis
                _kingFridayMultiSeriesLabels = groupedByDay
                    .Select(p => p.Updated.ToString("MM/dd"))
                    .ToArray();

                // Process Soul Eggs data
                var soulEggsValues = new List<double>();

                foreach (var dataPoint in groupedByDay)
                {
                    soulEggsValues.Add(ParseSoulEggs(dataPoint.SoulEggs));
                }

                // Update the metrics dictionary with Soul Eggs data only
                _metricHistoryData = new Dictionary<string, List<double>>
                {
                    ["Soul Eggs"] = soulEggsValues
                };

                Logger.LogInformation("Successfully loaded Soul Eggs data with {Count} data points", groupedByDay.Count);
            }
            else
            {
                Logger.LogWarning("No historical data found for King Friday");
                // No real data available, initialize with test data
                InitializeTestData();
                return;
            }

            // Format the chart data based on the selected number of days
            // Limit the labels and data to the selected number of days
            var daysToShow = Math.Min(_selectedDaysToDisplay, _kingFridayMultiSeriesLabels.Length);
            var labels = _kingFridayMultiSeriesLabels.TakeLast(daysToShow).ToArray();

            // Create single series for Soul Eggs
            _kingFridayMultiSeriesData = new List<ChartSeries>();

            var soulEggData = _metricHistoryData["Soul Eggs"].TakeLast(daysToShow).ToArray();
            _kingFridayMultiSeriesData.Add(new ChartSeries
            {
                Name = "Soul Eggs",
                Data = soulEggData
            });

            // Update the labels
            _kingFridayMultiSeriesLabels = labels;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading King Friday Soul Eggs data");
            // If there's an error, fall back to test data
            InitializeTestData();
        }
    }

    // Handle the slider value change
    private async void OnDaysSliderChanged(int value)
    {
        _selectedDaysToDisplay = value;
        await UpdateMultiSeriesChart();
        StateHasChanged();
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

    // Manual refresh removed as per user request

    public void Dispose()
    {
        // Clean up the timers when the component is disposed
        try
        {
            // Dispose main refresh timer
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Elapsed -= OnTimerElapsed; // Remove the event handler
                _timer.Dispose();
                _timer = null;
            }

            // Dispose update timer
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer.Elapsed -= OnUpdateTimerElapsed; // Remove the event handler
                _updateTimer.Dispose();
                _updateTimer = null;
            }

            // Remove any event handlers from shared state
            DashboardState.OnChange -= StateHasChanged;

            // Clear any references that might be holding DotNetObjectReference instances
            _kingFridayTitleProgressData = new double[] { 0, 100 };
            _kingFridayTitleProgressLabels = new string[] { "", "" };
            _kingSaturdayTitleProgressData = new double[] { 0, 100 };
            _kingSaturdayTitleProgressLabels = new string[] { "", "" };
            _kingSundayTitleProgressData = new double[] { 0, 100 };
            _kingSundayTitleProgressLabels = new string[] { "", "" };
            _kingMondayTitleProgressData = new double[] { 0, 100 };
            _kingMondayTitleProgressLabels = new string[] { "", "" };
            _kingFridaySEHistorySeries = new List<ChartSeries>();
            _kingFridaySEHistoryLabels = Array.Empty<string>();
            _kingFridaySEHistoryData = Array.Empty<double>();
            _kingFridayMultiSeriesData = new List<ChartSeries>();
            _kingFridayMultiSeriesLabels = Array.Empty<string>();
            _metricHistoryData = new Dictionary<string, List<double>>();

            // Clear other references
            _kingFriday = null;
            _kingFridayStats = null;
            _kingFridayGoals = null;
            _kingSaturday = null;
            _kingSaturdayStats = null;
            _kingSaturdayGoals = null;
            _kingSunday = null;
            _kingSundayStats = null;
            _kingSundayGoals = null;
            _kingMonday = null;
            _kingMondayStats = null;
            _kingMondayGoals = null;
            _SEGoalBegin = null;
            _SEGoalEnd = null;
            _EBGoalBegin = null;
            _EBGoalEnd = null;
            _MERGoalBegin = null;
            _MERGoalEnd = null;
            _JERGoalBegin = null;
            _JERGoalEnd = null;

            // Force garbage collection to clean up any lingering references
            GC.Collect();
            GC.WaitForPendingFinalizers();

            // Second garbage collection pass to ensure all references are cleaned up
            GC.Collect();
            GC.WaitForPendingFinalizers();

            Logger.LogInformation("Dashboard component disposed, timers and event handlers cleaned up");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error disposing Dashboard component");
        }
    }
}
