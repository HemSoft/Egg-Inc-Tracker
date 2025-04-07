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
    private bool _isRefreshing = false; // Added flag

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
            if (_isRefreshing) return;
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
                StateHasChanged();
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
        if (_isRefreshing) return;

        bool isInitialLoad = _lastUpdated == default;
        _isRefreshing = true;
        if (!isInitialLoad) StateHasChanged();

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

            Logger.LogInformation($"Progress bar values after refresh: SE={_kingFridaySEGoalPercentage:F1}%, EB={_kingFridayEBGoalPercentage:F1}%, MER={_kingFridayMERGoalPercentage:F1}%, JER={_kingFridayJERGoalPercentage:F1}%");
            Logger.LogInformation("Data refresh completed");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error refreshing data: {Message}", ex.Message);
            if (ex.InnerException != null) Logger.LogError("Inner exception: {Message}", ex.InnerException.Message);
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
            _kingFridaySEThisWeek = await PlayerDataService.CalculateSEThisWeekAsync(_kingFriday);
            var (prestigesToday, prestigesThisWeek) = await PlayerDataService.CalculatePrestigesAsync(_kingFriday);
            _kingFriday.PrestigesToday = prestigesToday;
            _kingFriday.PrestigesThisWeek = prestigesThisWeek;

            var titleInfo = await ApiService.GetTitleInfoAsync("King Friday!");
            if (titleInfo != null)
            {
                _kingFridayTitleProgressData = new double[] { titleInfo.TitleProgress, 100 - titleInfo.TitleProgress };
                _kingFridayTitleProgressLabels = new string[] { FormatTitleChangeLabel(DateTime.Parse(titleInfo.ProjectedTitleChange)), titleInfo.NextTitle };
            }
            else if (_kingFridayStats?.ProjectedTitleChange != null) // Check if stats has the date
            {
                _kingFridayTitleProgressData = new double[] { _kingFriday.TitleProgress, 100 - _kingFriday.TitleProgress };
                _kingFridayTitleProgressLabels = new string[] { FormatTitleChangeLabel(DateTime.Parse(_kingFridayStats.ProjectedTitleChange)), _kingFriday.NextTitle };
            }

            _kingFridayGoals = await ApiService.GetPlayerGoalsAsync("King Friday!");

            var sePlayers = await ApiService.GetSurroundingSEPlayersAsync("King Friday!", _kingFriday.SoulEggs);
            var ebPlayers = await ApiService.GetSurroundingEBPlayersAsync("King Friday!", _kingFriday.EarningsBonusPercentage);
            var merPlayers = await ApiService.GetSurroundingMERPlayersAsync("King Friday!", (decimal)_kingFriday.MER);
            var jerPlayers = await ApiService.GetSurroundingJERPlayersAsync("King Friday!", (decimal)_kingFriday.JER);

            _SEGoalBegin = sePlayers?.LowerPlayer; _SEGoalEnd = sePlayers?.UpperPlayer;
            _EBGoalBegin = ebPlayers?.LowerPlayer; _EBGoalEnd = ebPlayers?.UpperPlayer;
            _MERGoalBegin = merPlayers?.LowerPlayer; _MERGoalEnd = merPlayers?.UpperPlayer;
            _JERGoalBegin = jerPlayers?.LowerPlayer; _JERGoalEnd = jerPlayers?.UpperPlayer;

            _kingFridaySEGoalPercentage = PlayerDataService.CalculateProgressPercentage(_kingFridayStats?.SE, _SEGoalEnd?.SEString, _SEGoalBegin?.SEString);
            _kingFridayEBGoalPercentage = PlayerDataService.CalculateProgressPercentage(_kingFridayStats?.EB, _EBGoalEnd?.EBString, _EBGoalBegin?.EBString);
            _kingFridayMERGoalPercentage = PlayerDataService.CalculateProgressPercentage(_kingFridayStats?.MER.ToString(), _MERGoalEnd?.MER.ToString(), _MERGoalBegin?.MER.ToString());
            _kingFridayJERGoalPercentage = PlayerDataService.CalculateProgressPercentage(_kingFridayStats?.JER.ToString(), _JERGoalEnd?.JER.ToString(), _JERGoalBegin?.JER.ToString());

            // Default to 50% if calculation fails or results in 0 (unless actual value is 0)
            _kingFridaySEGoalPercentage = _kingFridaySEGoalPercentage == 0 && ParseBigNumberWrapper(_kingFridayStats?.SE) != 0 ? 50 : _kingFridaySEGoalPercentage;
            _kingFridayEBGoalPercentage = _kingFridayEBGoalPercentage == 0 && ParseBigNumberWrapper(_kingFridayStats?.EB) != 0 ? 50 : _kingFridayEBGoalPercentage;
            _kingFridayMERGoalPercentage = _kingFridayMERGoalPercentage == 0 && _kingFridayStats?.MER != 0 ? 50 : _kingFridayMERGoalPercentage;
            _kingFridayJERGoalPercentage = _kingFridayJERGoalPercentage == 0 && _kingFridayStats?.JER != 0 ? 50 : _kingFridayJERGoalPercentage;

            await InvokeAsync(StateHasChanged);
        }
    }

     // Wrapper to handle potential nulls for the 50% default logic
    private double ParseBigNumberWrapper(string? value) => string.IsNullOrEmpty(value) ? 0 : PlayerDataService.CalculateProgressPercentage(value, null, null); // Re-use parser via CalculateProgressPercentage


    private async Task UpdateKingSaturday()
    {
        _kingSaturday = await ApiService.GetLatestPlayerAsync("King Saturday!");
        _kingSaturdayStats = await ApiService.GetRankedPlayerAsync("King Saturday!", 1, 30);
        Logger.LogInformation($"Loaded King Saturday with EID: {_kingSaturday?.EID}");

        if (_kingSaturday != null)
        {
            await InvokeAsync(() => DashboardState.SetPlayerLastUpdated("King Saturday!", _kingSaturday.Updated));
            _kingSaturdaySEThisWeek = await PlayerDataService.CalculateSEThisWeekAsync(_kingSaturday);
            var (prestigesToday, prestigesThisWeek) = await PlayerDataService.CalculatePrestigesAsync(_kingSaturday);
            _kingSaturday.PrestigesToday = prestigesToday;
            _kingSaturday.PrestigesThisWeek = prestigesThisWeek;

            var titleInfo = await ApiService.GetTitleInfoAsync("King Saturday!");
             if (titleInfo != null)
            {
                _kingSaturdayTitleProgressData = new double[] { titleInfo.TitleProgress, 100 - titleInfo.TitleProgress };
                _kingSaturdayTitleProgressLabels = new string[] { FormatTitleChangeLabel(DateTime.Parse(titleInfo.ProjectedTitleChange)), titleInfo.NextTitle };
            }
             else if (_kingSaturdayStats?.ProjectedTitleChange != null)
            {
                _kingSaturdayTitleProgressData = new double[] { _kingSaturday.TitleProgress, 100 - _kingSaturday.TitleProgress };
                _kingSaturdayTitleProgressLabels = new string[] { FormatTitleChangeLabel(DateTime.Parse(_kingSaturdayStats.ProjectedTitleChange)), _kingSaturday.NextTitle };
            }

            _kingSaturdayGoals = await ApiService.GetPlayerGoalsAsync("King Saturday!");

            var sePlayers = await ApiService.GetSurroundingSEPlayersAsync("King Saturday!", _kingSaturday.SoulEggs);
            var ebPlayers = await ApiService.GetSurroundingEBPlayersAsync("King Saturday!", _kingSaturday.EarningsBonusPercentage);
            var merPlayers = await ApiService.GetSurroundingMERPlayersAsync("King Saturday!", (decimal)_kingSaturday.MER);
            var jerPlayers = await ApiService.GetSurroundingJERPlayersAsync("King Saturday!", (decimal)_kingSaturday.JER);

             _kingSaturdaySEGoalBegin = sePlayers?.LowerPlayer; _kingSaturdaySEGoalEnd = sePlayers?.UpperPlayer;
             _kingSaturdayEBGoalBegin = ebPlayers?.LowerPlayer; _kingSaturdayEBGoalEnd = ebPlayers?.UpperPlayer;
             _kingSaturdayMERGoalBegin = merPlayers?.LowerPlayer; _kingSaturdayMERGoalEnd = merPlayers?.UpperPlayer;
             _kingSaturdayJERGoalBegin = jerPlayers?.LowerPlayer; _kingSaturdayJERGoalEnd = jerPlayers?.UpperPlayer;

            _kingSaturdaySEGoalPercentage = PlayerDataService.CalculateProgressPercentage(_kingSaturdayStats?.SE, _kingSaturdaySEGoalEnd?.SEString, _kingSaturdaySEGoalBegin?.SEString);
            _kingSaturdayEBGoalPercentage = PlayerDataService.CalculateProgressPercentage(_kingSaturdayStats?.EB, _kingSaturdayEBGoalEnd?.EBString, _kingSaturdayEBGoalBegin?.EBString);
            _kingSaturdayMERGoalPercentage = PlayerDataService.CalculateProgressPercentage(_kingSaturdayStats?.MER.ToString(), _kingSaturdayMERGoalEnd?.MER.ToString(), _kingSaturdayMERGoalBegin?.MER.ToString());
            _kingSaturdayJERGoalPercentage = PlayerDataService.CalculateProgressPercentage(_kingSaturdayStats?.JER.ToString(), _kingSaturdayJERGoalEnd?.JER.ToString(), _kingSaturdayJERGoalBegin?.JER.ToString());

            _kingSaturdaySEGoalPercentage = _kingSaturdaySEGoalPercentage == 0 && ParseBigNumberWrapper(_kingSaturdayStats?.SE) != 0 ? 50 : _kingSaturdaySEGoalPercentage;
            _kingSaturdayEBGoalPercentage = _kingSaturdayEBGoalPercentage == 0 && ParseBigNumberWrapper(_kingSaturdayStats?.EB) != 0 ? 50 : _kingSaturdayEBGoalPercentage;
            _kingSaturdayMERGoalPercentage = _kingSaturdayMERGoalPercentage == 0 && _kingSaturdayStats?.MER != 0 ? 50 : _kingSaturdayMERGoalPercentage;
            _kingSaturdayJERGoalPercentage = _kingSaturdayJERGoalPercentage == 0 && _kingSaturdayStats?.JER != 0 ? 50 : _kingSaturdayJERGoalPercentage;

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
             _kingSundaySEThisWeek = await PlayerDataService.CalculateSEThisWeekAsync(_kingSunday);
            var (prestigesToday, prestigesThisWeek) = await PlayerDataService.CalculatePrestigesAsync(_kingSunday);
            _kingSunday.PrestigesToday = prestigesToday;
            _kingSunday.PrestigesThisWeek = prestigesThisWeek;

            var titleInfo = await ApiService.GetTitleInfoAsync("King Sunday!");
             if (titleInfo != null)
            {
                _kingSundayTitleProgressData = new double[] { titleInfo.TitleProgress, 100 - titleInfo.TitleProgress };
                _kingSundayTitleProgressLabels = new string[] { FormatTitleChangeLabel(DateTime.Parse(titleInfo.ProjectedTitleChange)), titleInfo.NextTitle };
            }
             else if (_kingSundayStats?.ProjectedTitleChange != null)
            {
                _kingSundayTitleProgressData = new double[] { _kingSunday.TitleProgress, 100 - _kingSunday.TitleProgress };
                _kingSundayTitleProgressLabels = new string[] { FormatTitleChangeLabel(DateTime.Parse(_kingSundayStats.ProjectedTitleChange)), _kingSunday.NextTitle };
            }

            _kingSundayGoals = await ApiService.GetPlayerGoalsAsync("King Sunday!");

            var sePlayers = await ApiService.GetSurroundingSEPlayersAsync("King Sunday!", _kingSunday.SoulEggs);
            var ebPlayers = await ApiService.GetSurroundingEBPlayersAsync("King Sunday!", _kingSunday.EarningsBonusPercentage);
            var merPlayers = await ApiService.GetSurroundingMERPlayersAsync("King Sunday!", (decimal)_kingSunday.MER);
            var jerPlayers = await ApiService.GetSurroundingJERPlayersAsync("King Sunday!", (decimal)_kingSunday.JER);

             _kingSundaySEGoalBegin = sePlayers?.LowerPlayer; _kingSundaySEGoalEnd = sePlayers?.UpperPlayer;
             _kingSundayEBGoalBegin = ebPlayers?.LowerPlayer; _kingSundayEBGoalEnd = ebPlayers?.UpperPlayer;
             _kingSundayMERGoalBegin = merPlayers?.LowerPlayer; _kingSundayMERGoalEnd = merPlayers?.UpperPlayer;
             _kingSundayJERGoalBegin = jerPlayers?.LowerPlayer; _kingSundayJERGoalEnd = jerPlayers?.UpperPlayer;

            _kingSundaySEGoalPercentage = PlayerDataService.CalculateProgressPercentage(_kingSundayStats?.SE, _kingSundaySEGoalEnd?.SEString, _kingSundaySEGoalBegin?.SEString);
            _kingSundayEBGoalPercentage = PlayerDataService.CalculateProgressPercentage(_kingSundayStats?.EB, _kingSundayEBGoalEnd?.EBString, _kingSundayEBGoalBegin?.EBString);
            _kingSundayMERGoalPercentage = PlayerDataService.CalculateProgressPercentage(_kingSundayStats?.MER.ToString(), _kingSundayMERGoalEnd?.MER.ToString(), _kingSundayMERGoalBegin?.MER.ToString());
            _kingSundayJERGoalPercentage = PlayerDataService.CalculateProgressPercentage(_kingSundayStats?.JER.ToString(), _kingSundayJERGoalEnd?.JER.ToString(), _kingSundayJERGoalBegin?.JER.ToString());

             _kingSundaySEGoalPercentage = _kingSundaySEGoalPercentage == 0 && ParseBigNumberWrapper(_kingSundayStats?.SE) != 0 ? 50 : _kingSundaySEGoalPercentage;
            _kingSundayEBGoalPercentage = _kingSundayEBGoalPercentage == 0 && ParseBigNumberWrapper(_kingSundayStats?.EB) != 0 ? 50 : _kingSundayEBGoalPercentage;
            _kingSundayMERGoalPercentage = _kingSundayMERGoalPercentage == 0 && _kingSundayStats?.MER != 0 ? 50 : _kingSundayMERGoalPercentage;
            _kingSundayJERGoalPercentage = _kingSundayJERGoalPercentage == 0 && _kingSundayStats?.JER != 0 ? 50 : _kingSundayJERGoalPercentage;

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
            _kingMondaySEThisWeek = await PlayerDataService.CalculateSEThisWeekAsync(_kingMonday);
            var (prestigesToday, prestigesThisWeek) = await PlayerDataService.CalculatePrestigesAsync(_kingMonday);
            _kingMonday.PrestigesToday = prestigesToday;
            _kingMonday.PrestigesThisWeek = prestigesThisWeek;

            var titleInfo = await ApiService.GetTitleInfoAsync("King Monday!");
             if (titleInfo != null)
            {
                _kingMondayTitleProgressData = new double[] { titleInfo.TitleProgress, 100 - titleInfo.TitleProgress };
                _kingMondayTitleProgressLabels = new string[] { FormatTitleChangeLabel(DateTime.Parse(titleInfo.ProjectedTitleChange)), titleInfo.NextTitle };
            }
             else if (_kingMondayStats?.ProjectedTitleChange != null)
            {
                _kingMondayTitleProgressData = new double[] { _kingMonday.TitleProgress, 100 - _kingMonday.TitleProgress };
                _kingMondayTitleProgressLabels = new string[] { FormatTitleChangeLabel(DateTime.Parse(_kingMondayStats.ProjectedTitleChange)), _kingMonday.NextTitle };
            }

            _kingMondayGoals = await ApiService.GetPlayerGoalsAsync("King Monday!");

            var sePlayers = await ApiService.GetSurroundingSEPlayersAsync("King Monday!", _kingMonday.SoulEggs);
            var ebPlayers = await ApiService.GetSurroundingEBPlayersAsync("King Monday!", _kingMonday.EarningsBonusPercentage);
            var merPlayers = await ApiService.GetSurroundingMERPlayersAsync("King Monday!", (decimal)_kingMonday.MER);
            var jerPlayers = await ApiService.GetSurroundingJERPlayersAsync("King Monday!", (decimal)_kingMonday.JER);

             _kingMondaySEGoalBegin = sePlayers?.LowerPlayer; _kingMondaySEGoalEnd = sePlayers?.UpperPlayer;
             _kingMondayEBGoalBegin = ebPlayers?.LowerPlayer; _kingMondayEBGoalEnd = ebPlayers?.UpperPlayer;
             _kingMondayMERGoalBegin = merPlayers?.LowerPlayer; _kingMondayMERGoalEnd = merPlayers?.UpperPlayer;
             _kingMondayJERGoalBegin = jerPlayers?.LowerPlayer; _kingMondayJERGoalEnd = jerPlayers?.UpperPlayer;

            _kingMondaySEGoalPercentage = PlayerDataService.CalculateProgressPercentage(_kingMondayStats?.SE, _kingMondaySEGoalEnd?.SEString, _kingMondaySEGoalBegin?.SEString);
            _kingMondayEBGoalPercentage = PlayerDataService.CalculateProgressPercentage(_kingMondayStats?.EB, _kingMondayEBGoalEnd?.EBString, _kingMondayEBGoalBegin?.EBString);
            _kingMondayMERGoalPercentage = PlayerDataService.CalculateProgressPercentage(_kingMondayStats?.MER.ToString(), _kingMondayMERGoalEnd?.MER.ToString(), _kingMondayMERGoalBegin?.MER.ToString());
            _kingMondayJERGoalPercentage = PlayerDataService.CalculateProgressPercentage(_kingMondayStats?.JER.ToString(), _kingMondayJERGoalEnd?.JER.ToString(), _kingMondayJERGoalBegin?.JER.ToString());

             _kingMondaySEGoalPercentage = _kingMondaySEGoalPercentage == 0 && ParseBigNumberWrapper(_kingMondayStats?.SE) != 0 ? 50 : _kingMondaySEGoalPercentage;
            _kingMondayEBGoalPercentage = _kingMondayEBGoalPercentage == 0 && ParseBigNumberWrapper(_kingMondayStats?.EB) != 0 ? 50 : _kingMondayEBGoalPercentage;
            _kingMondayMERGoalPercentage = _kingMondayMERGoalPercentage == 0 && _kingMondayStats?.MER != 0 ? 50 : _kingMondayMERGoalPercentage;
            _kingMondayJERGoalPercentage = _kingMondayJERGoalPercentage == 0 && _kingMondayStats?.JER != 0 ? 50 : _kingMondayJERGoalPercentage;

            await InvokeAsync(StateHasChanged);
        }
    }

    // Removed LoadKingFridaySEHistory method

    private string GetTimeSinceLastUpdate()
    {
        if (_lastUpdated == default) return "Never";
        var timeSince = DateTime.Now - _lastUpdated;
        if (timeSince.TotalSeconds < 60) return $"{(int)timeSince.TotalSeconds} seconds ago";
        if (timeSince.TotalMinutes < 60) return $"{(int)timeSince.TotalMinutes} minutes, {timeSince.Seconds:D} seconds ago";
        if (timeSince.TotalHours < 24) return $"{(int)timeSince.TotalHours} hours, {timeSince.Minutes:D} minutes ago";
        return $"{timeSince.Days:D} days, {timeSince.Hours:D} hours ago";
    }

    private string FormatTitleChangeLabel(DateTime projectedDate)
    {
        var timeSpan = projectedDate - DateTime.Now;
        if (timeSpan.Days < 0) return "Title change overdue";
        if (timeSpan.Days == 0) return "Title change today";
        if (timeSpan.Days == 1) return "Next title tomorrow";
        return $"Next title in {timeSpan.Days} days";
    }

    // CalculateProgressPercentage removed, now in PlayerDataService
    // TryParseAnyNumber removed, now in PlayerDataService
    // TryParseScientificNotation removed, now in PlayerDataService

    // Removed InitializeTestData method
    // Removed UpdateMultiSeriesChart method
    // Removed OnDaysSliderChanged method

    private string GetLocalTimeString(DateTime utcTime)
    {
        DateTime localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.Local);
        if (localTime.Date == DateTime.Today) return $"Today {localTime:HH:mm}";
        if (localTime.Date == DateTime.Today.AddDays(-1)) return $"Yesterday {localTime:HH:mm}";
        return localTime.ToString("MM/dd HH:mm");
    }

    private MudBlazor.Color CalculateProgressColor(int? actual, int goal)
    {
        if (actual == null || goal <= 0) return MudBlazor.Color.Default;
        double percentage = (double)actual.Value / goal * 100;
        if (percentage >= 100) return MudBlazor.Color.Success;
        if (percentage >= 75) return MudBlazor.Color.Info;
        if (percentage >= 50) return MudBlazor.Color.Warning;
        if (percentage >= 25) return MudBlazor.Color.Error;
        return MudBlazor.Color.Dark;
    }

    private string GetProgressColorStyle(int? actual, int goal)
    {
        return PlayerDataService.GetProgressColorStyle(actual, goal);
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
