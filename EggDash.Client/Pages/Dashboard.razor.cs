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

    [Inject]
    private PlayerDataService PlayerDataService { get; set; } = default!; // Inject the new service

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
    private MajPlayerRankingDto? _kingSaturdaySEGoalBegin;
    private MajPlayerRankingDto? _kingSaturdaySEGoalEnd;
    private MajPlayerRankingDto? _kingSaturdayEBGoalBegin;
    private MajPlayerRankingDto? _kingSaturdayEBGoalEnd;
    private MajPlayerRankingDto? _kingSaturdayMERGoalBegin;
    private MajPlayerRankingDto? _kingSaturdayMERGoalEnd;
    private MajPlayerRankingDto? _kingSaturdayJERGoalBegin;
    private MajPlayerRankingDto? _kingSaturdayJERGoalEnd;
    private string _kingSaturdaySEThisWeek = string.Empty;

    // Progress bar percentages for King Saturday
    private double _kingSaturdaySEGoalPercentage = 0;
    private double _kingSaturdayEBGoalPercentage = 0;
    private double _kingSaturdayMERGoalPercentage = 0;
    private double _kingSaturdayJERGoalPercentage = 0;

    private PlayerDto? _kingSunday;
    private PlayerStatsDto? _kingSundayStats;
    private PlayerGoalDto? _kingSundayGoals;
    private MajPlayerRankingDto? _kingSundaySEGoalBegin;
    private MajPlayerRankingDto? _kingSundaySEGoalEnd;
    private MajPlayerRankingDto? _kingSundayEBGoalBegin;
    private MajPlayerRankingDto? _kingSundayEBGoalEnd;
    private MajPlayerRankingDto? _kingSundayMERGoalBegin;
    private MajPlayerRankingDto? _kingSundayMERGoalEnd;
    private MajPlayerRankingDto? _kingSundayJERGoalBegin;
    private MajPlayerRankingDto? _kingSundayJERGoalEnd;
    private string _kingSundaySEThisWeek = string.Empty;

    // Progress bar percentages for King Sunday
    private double _kingSundaySEGoalPercentage = 0;
    private double _kingSundayEBGoalPercentage = 0;
    private double _kingSundayMERGoalPercentage = 0;
    private double _kingSundayJERGoalPercentage = 0;

    private PlayerDto? _kingMonday;
    private PlayerStatsDto? _kingMondayStats;
    private PlayerGoalDto? _kingMondayGoals;
    private MajPlayerRankingDto? _kingMondaySEGoalBegin;
    private MajPlayerRankingDto? _kingMondaySEGoalEnd;
    private MajPlayerRankingDto? _kingMondayEBGoalBegin;
    private MajPlayerRankingDto? _kingMondayEBGoalEnd;
    private MajPlayerRankingDto? _kingMondayMERGoalBegin;
    private MajPlayerRankingDto? _kingMondayMERGoalEnd;
    private MajPlayerRankingDto? _kingMondayJERGoalBegin;
    private MajPlayerRankingDto? _kingMondayJERGoalEnd;
    private string _kingMondaySEThisWeek = string.Empty;

    // Progress bar percentages for King Monday
    private double _kingMondaySEGoalPercentage = 0;
    private double _kingMondayEBGoalPercentage = 0;
    private double _kingMondayMERGoalPercentage = 0;
    private double _kingMondayJERGoalPercentage = 0;

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
            await InvokeAsync(() => DashboardState.SetPlayerLastUpdated("King Friday!", _kingFriday.Updated));

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

            // Calculate SE This Week using the service
            _kingFridaySEThisWeek = await PlayerDataService.CalculateSEThisWeekAsync(_kingFriday);

            // Calculate Prestiges Today/Week using the service
            var (prestigesToday, prestigesThisWeek) = await PlayerDataService.CalculatePrestigesAsync(_kingFriday);
            _kingFriday.PrestigesToday = prestigesToday;
            _kingFriday.PrestigesThisWeek = prestigesThisWeek;

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

            // Calculate progress bar percentages using the service
            var sePercentage = PlayerDataService.CalculateProgressPercentage(_kingFridayStats?.SE, _SEGoalEnd?.SEString, _SEGoalBegin?.SEString);
            var ebPercentage = PlayerDataService.CalculateProgressPercentage(_kingFridayStats?.EB, _EBGoalEnd?.EBString, _EBGoalBegin?.EBString);
            var merPercentage = PlayerDataService.CalculateProgressPercentage(_kingFridayStats?.MER.ToString(), _MERGoalEnd?.MER.ToString(), _MERGoalBegin?.MER.ToString());
            var jerPercentage = PlayerDataService.CalculateProgressPercentage(_kingFridayStats?.JER.ToString(), _JERGoalEnd?.JER.ToString(), _JERGoalBegin?.JER.ToString());

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

            // Calculate Prestiges Today/Week using the service
            var (prestigesTodaySat, prestigesThisWeekSat) = await PlayerDataService.CalculatePrestigesAsync(_kingSaturday);
            _kingSaturday.PrestigesToday = prestigesTodaySat;
            _kingSaturday.PrestigesThisWeek = prestigesThisWeekSat;

            // Calculate SE This Week using the service
            _kingSaturdaySEThisWeek = await PlayerDataService.CalculateSEThisWeekAsync(_kingSaturday);

            // Update goal data using ApiService directly
            var sePlayers = await ApiService.GetSurroundingSEPlayersAsync("King Saturday!", _kingSaturday.SoulEggs);
            var ebPlayers = await ApiService.GetSurroundingEBPlayersAsync("King Saturday!", _kingSaturday.EarningsBonusPercentage);
            var merPlayers = await ApiService.GetSurroundingMERPlayersAsync("King Saturday!", (decimal)_kingSaturday.MER);
            var jerPlayers = await ApiService.GetSurroundingJERPlayersAsync("King Saturday!", (decimal)_kingSaturday.JER);

            // Set goal data for progress bars
            if (sePlayers != null)
            {
                _kingSaturdaySEGoalBegin = sePlayers.LowerPlayer;
                _kingSaturdaySEGoalEnd = sePlayers.UpperPlayer;
            }

            if (ebPlayers != null)
            {
                _kingSaturdayEBGoalBegin = ebPlayers.LowerPlayer;
                _kingSaturdayEBGoalEnd = ebPlayers.UpperPlayer;
            }

            if (merPlayers != null)
            {
                _kingSaturdayMERGoalBegin = merPlayers.LowerPlayer;
                _kingSaturdayMERGoalEnd = merPlayers.UpperPlayer;
            }

            if (jerPlayers != null)
            {
                _kingSaturdayJERGoalBegin = jerPlayers.LowerPlayer;
                _kingSaturdayJERGoalEnd = jerPlayers.UpperPlayer;
            }

            // Calculate progress bar percentages using the service
            var sePercentage = PlayerDataService.CalculateProgressPercentage(_kingSaturdayStats?.SE, _kingSaturdaySEGoalEnd?.SEString, _kingSaturdaySEGoalBegin?.SEString);
            var ebPercentage = PlayerDataService.CalculateProgressPercentage(_kingSaturdayStats?.EB, _kingSaturdayEBGoalEnd?.EBString, _kingSaturdayEBGoalBegin?.EBString);
            var merPercentage = PlayerDataService.CalculateProgressPercentage(_kingSaturdayStats?.MER.ToString(), _kingSaturdayMERGoalEnd?.MER.ToString(), _kingSaturdayMERGoalBegin?.MER.ToString());
            var jerPercentage = PlayerDataService.CalculateProgressPercentage(_kingSaturdayStats?.JER.ToString(), _kingSaturdayJERGoalEnd?.JER.ToString(), _kingSaturdayJERGoalBegin?.JER.ToString());

            // Ensure we have valid percentages (not zero)
            _kingSaturdaySEGoalPercentage = sePercentage > 0 ? sePercentage : 50; // Default to 50% if calculation fails
            _kingSaturdayEBGoalPercentage = ebPercentage > 0 ? ebPercentage : 50;
            _kingSaturdayMERGoalPercentage = merPercentage > 0 ? merPercentage : 50;
            _kingSaturdayJERGoalPercentage = jerPercentage > 0 ? jerPercentage : 50;

            Logger.LogInformation($"King Saturday progress bar percentages: SE={_kingSaturdaySEGoalPercentage:F1}%, EB={_kingSaturdayEBGoalPercentage:F1}%, MER={_kingSaturdayMERGoalPercentage:F1}%, JER={_kingSaturdayJERGoalPercentage:F1}%");

            // Update the player's last update time in DashboardState
            await InvokeAsync(() => DashboardState.SetPlayerLastUpdated("King Saturday!", _kingSaturday.Updated));

            // Force UI update after calculating progress bar percentages
            await InvokeAsync(StateHasChanged);
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

            // Calculate Prestiges Today/Week using the service
            var (prestigesTodaySun, prestigesThisWeekSun) = await PlayerDataService.CalculatePrestigesAsync(_kingSunday);
            _kingSunday.PrestigesToday = prestigesTodaySun;
            _kingSunday.PrestigesThisWeek = prestigesThisWeekSun;

            // Calculate SE This Week using the service
            _kingSundaySEThisWeek = await PlayerDataService.CalculateSEThisWeekAsync(_kingSunday);

            // Update goal data using ApiService directly
            var sePlayers = await ApiService.GetSurroundingSEPlayersAsync("King Sunday!", _kingSunday.SoulEggs);
            var ebPlayers = await ApiService.GetSurroundingEBPlayersAsync("King Sunday!", _kingSunday.EarningsBonusPercentage);
            var merPlayers = await ApiService.GetSurroundingMERPlayersAsync("King Sunday!", (decimal)_kingSunday.MER);
            var jerPlayers = await ApiService.GetSurroundingJERPlayersAsync("King Sunday!", (decimal)_kingSunday.JER);

            // Set goal data for progress bars
            if (sePlayers != null)
            {
                _kingSundaySEGoalBegin = sePlayers.LowerPlayer;
                _kingSundaySEGoalEnd = sePlayers.UpperPlayer;
            }

            if (ebPlayers != null)
            {
                _kingSundayEBGoalBegin = ebPlayers.LowerPlayer;
                _kingSundayEBGoalEnd = ebPlayers.UpperPlayer;
            }

            if (merPlayers != null)
            {
                _kingSundayMERGoalBegin = merPlayers.LowerPlayer;
                _kingSundayMERGoalEnd = merPlayers.UpperPlayer;
            }

            if (jerPlayers != null)
            {
                _kingSundayJERGoalBegin = jerPlayers.LowerPlayer;
                _kingSundayJERGoalEnd = jerPlayers.UpperPlayer;
            }

            // Calculate progress bar percentages using the service
            var sePercentage = PlayerDataService.CalculateProgressPercentage(_kingSundayStats?.SE, _kingSundaySEGoalEnd?.SEString, _kingSundaySEGoalBegin?.SEString);
            var ebPercentage = PlayerDataService.CalculateProgressPercentage(_kingSundayStats?.EB, _kingSundayEBGoalEnd?.EBString, _kingSundayEBGoalBegin?.EBString);
            var merPercentage = PlayerDataService.CalculateProgressPercentage(_kingSundayStats?.MER.ToString(), _kingSundayMERGoalEnd?.MER.ToString(), _kingSundayMERGoalBegin?.MER.ToString());
            var jerPercentage = PlayerDataService.CalculateProgressPercentage(_kingSundayStats?.JER.ToString(), _kingSundayJERGoalEnd?.JER.ToString(), _kingSundayJERGoalBegin?.JER.ToString());

            // Ensure we have valid percentages (not zero)
            _kingSundaySEGoalPercentage = sePercentage > 0 ? sePercentage : 50; // Default to 50% if calculation fails
            _kingSundayEBGoalPercentage = ebPercentage > 0 ? ebPercentage : 50;
            _kingSundayMERGoalPercentage = merPercentage > 0 ? merPercentage : 50;
            _kingSundayJERGoalPercentage = jerPercentage > 0 ? jerPercentage : 50;

            Logger.LogInformation($"King Sunday progress bar percentages: SE={_kingSundaySEGoalPercentage:F1}%, EB={_kingSundayEBGoalPercentage:F1}%, MER={_kingSundayMERGoalPercentage:F1}%, JER={_kingSundayJERGoalPercentage:F1}%");

            // Update the player's last update time in DashboardState
            await InvokeAsync(() => DashboardState.SetPlayerLastUpdated("King Sunday!", _kingSunday.Updated));

            // Force UI update after calculating progress bar percentages
            await InvokeAsync(StateHasChanged);
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

            // Calculate Prestiges Today/Week using the service
            var (prestigesTodayMon, prestigesThisWeekMon) = await PlayerDataService.CalculatePrestigesAsync(_kingMonday);
            _kingMonday.PrestigesToday = prestigesTodayMon;
            _kingMonday.PrestigesThisWeek = prestigesThisWeekMon;

            // Calculate SE This Week using the service
            _kingMondaySEThisWeek = await PlayerDataService.CalculateSEThisWeekAsync(_kingMonday);

            // Update goal data using ApiService directly
            var sePlayers = await ApiService.GetSurroundingSEPlayersAsync("King Monday!", _kingMonday.SoulEggs);
            var ebPlayers = await ApiService.GetSurroundingEBPlayersAsync("King Monday!", _kingMonday.EarningsBonusPercentage);
            var merPlayers = await ApiService.GetSurroundingMERPlayersAsync("King Monday!", (decimal)_kingMonday.MER);
            var jerPlayers = await ApiService.GetSurroundingJERPlayersAsync("King Monday!", (decimal)_kingMonday.JER);

            // Set goal data for progress bars
            if (sePlayers != null)
            {
                _kingMondaySEGoalBegin = sePlayers.LowerPlayer;
                _kingMondaySEGoalEnd = sePlayers.UpperPlayer;
            }

            if (ebPlayers != null)
            {
                _kingMondayEBGoalBegin = ebPlayers.LowerPlayer;
                _kingMondayEBGoalEnd = ebPlayers.UpperPlayer;
            }

            if (merPlayers != null)
            {
                _kingMondayMERGoalBegin = merPlayers.LowerPlayer;
                _kingMondayMERGoalEnd = merPlayers.UpperPlayer;
            }

            if (jerPlayers != null)
            {
                _kingMondayJERGoalBegin = jerPlayers.LowerPlayer;
                _kingMondayJERGoalEnd = jerPlayers.UpperPlayer;
            }

            // Calculate progress bar percentages using the service
            var sePercentage = PlayerDataService.CalculateProgressPercentage(_kingMondayStats?.SE, _kingMondaySEGoalEnd?.SEString, _kingMondaySEGoalBegin?.SEString);
            var ebPercentage = PlayerDataService.CalculateProgressPercentage(_kingMondayStats?.EB, _kingMondayEBGoalEnd?.EBString, _kingMondayEBGoalBegin?.EBString);
            var merPercentage = PlayerDataService.CalculateProgressPercentage(_kingMondayStats?.MER.ToString(), _kingMondayMERGoalEnd?.MER.ToString(), _kingMondayMERGoalBegin?.MER.ToString());
            var jerPercentage = PlayerDataService.CalculateProgressPercentage(_kingMondayStats?.JER.ToString(), _kingMondayJERGoalEnd?.JER.ToString(), _kingMondayJERGoalBegin?.JER.ToString());

            // Ensure we have valid percentages (not zero)
            _kingMondaySEGoalPercentage = sePercentage > 0 ? sePercentage : 50; // Default to 50% if calculation fails
            _kingMondayEBGoalPercentage = ebPercentage > 0 ? ebPercentage : 50;
            _kingMondayMERGoalPercentage = merPercentage > 0 ? merPercentage : 50;
            _kingMondayJERGoalPercentage = jerPercentage > 0 ? jerPercentage : 50;

            Logger.LogInformation($"King Monday progress bar percentages: SE={_kingMondaySEGoalPercentage:F1}%, EB={_kingMondayEBGoalPercentage:F1}%, MER={_kingMondayMERGoalPercentage:F1}%, JER={_kingMondayJERGoalPercentage:F1}%");

            // Update the player's last update time in DashboardState
            await InvokeAsync(() => DashboardState.SetPlayerLastUpdated("King Monday!", _kingMonday.Updated));

            // Force UI update after calculating progress bar percentages
            await InvokeAsync(StateHasChanged);
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

                // Convert SE values to numeric values for the chart using the service's parser
                _kingFridaySEHistoryData = historicalData
                    .Select(p => PlayerDataService.CalculateProgressPercentage(p.SoulEggs, null, null)) // Re-use parser logic via CalculateProgressPercentage
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

    // ParseSoulEggs removed, logic is now in PlayerDataService.ParseBigNumber

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

    // CalculateProgressPercentage removed, now in PlayerDataService
    // TryParseAnyNumber removed, now in PlayerDataService
    // TryParseScientificNotation removed, now in PlayerDataService

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
    // Keep method signature, call service for implementation
    private string GetProgressColorStyle(int? actual, int goal)
    {
        return PlayerDataService.GetProgressColorStyle(actual, goal);
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

                // Process Soul Eggs data using the service's parser
                var soulEggsValues = groupedByDay
                    .Select(p => PlayerDataService.CalculateProgressPercentage(p.SoulEggs, null, null)) // Re-use parser logic
                    .ToList();

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

            // Clear King Friday goal references
            _SEGoalBegin = null;
            _SEGoalEnd = null;
            _EBGoalBegin = null;
            _EBGoalEnd = null;
            _MERGoalBegin = null;
            _MERGoalEnd = null;
            _JERGoalBegin = null;
            _JERGoalEnd = null;

            // Clear King Saturday goal references
            _kingSaturdaySEGoalBegin = null;
            _kingSaturdaySEGoalEnd = null;
            _kingSaturdayEBGoalBegin = null;
            _kingSaturdayEBGoalEnd = null;
            _kingSaturdayMERGoalBegin = null;
            _kingSaturdayMERGoalEnd = null;
            _kingSaturdayJERGoalBegin = null;
            _kingSaturdayJERGoalEnd = null;

            // Clear King Sunday goal references
            _kingSundaySEGoalBegin = null;
            _kingSundaySEGoalEnd = null;
            _kingSundayEBGoalBegin = null;
            _kingSundayEBGoalEnd = null;
            _kingSundayMERGoalBegin = null;
            _kingSundayMERGoalEnd = null;
            _kingSundayJERGoalBegin = null;
            _kingSundayJERGoalEnd = null;

            // Clear King Monday goal references
            _kingMondaySEGoalBegin = null;
            _kingMondaySEGoalEnd = null;
            _kingMondayEBGoalBegin = null;
            _kingMondayEBGoalEnd = null;
            _kingMondayMERGoalBegin = null;
            _kingMondayMERGoalEnd = null;
            _kingMondayJERGoalBegin = null;
            _kingMondayJERGoalEnd = null;

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
