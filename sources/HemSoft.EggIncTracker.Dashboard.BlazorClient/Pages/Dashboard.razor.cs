namespace HemSoft.EggIncTracker.Dashboard.BlazorClient.Pages;

using System.Numerics;
using HemSoft.EggIncTracker.Dashboard.BlazorClient.Services;

using HemSoft.EggIncTracker.Data.Dtos;

using Microsoft.AspNetCore.Components;

using ST = System.Timers;

public class DashboardPlayer
{
    public PlayerDto? Player { get; set; }
    public PlayerStatsDto? Stats { get; set; }
    public PlayerGoalDto? Goals{ get; set; }
    public int DaysToNextTitle { get; set; }
    public MajPlayerRankingDto? SEGoalBegin { get; set; }
    public MajPlayerRankingDto? SEGoalEnd { get; set; }
    public MajPlayerRankingDto? EBGoalBegin { get; set; }
    public MajPlayerRankingDto? EBGoalEnd { get; set; }
    public MajPlayerRankingDto? MERGoalBegin { get; set; }
    public MajPlayerRankingDto? MERGoalEnd { get; set; }
    public MajPlayerRankingDto? JERGoalBegin { get; set; }
    public MajPlayerRankingDto? JERGoalEnd { get; set; }
    public BigInteger? SEThisWeek { get; set; }
}

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
    private PlayerDataService PlayerDataService { get; set; } = default!;

    private ST.Timer? _timer;
    private ST.Timer? _updateTimer;
    private DateTime _lastUpdated;
    private string _timeSinceLastUpdate = "Never";
    private bool _isRefreshing;

    private DashboardPlayer _kingFridayPlayer = new DashboardPlayer();
    private DashboardPlayer _kingSaturdayPlayer = new DashboardPlayer();
    private DashboardPlayer _kingSundayPlayer = new DashboardPlayer();
    private DashboardPlayer _kingMondayPlayer = new DashboardPlayer();

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
            {
                return;
            }

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
                    UpdateDashboardPlayer(_kingFridayPlayer,"King Friday!"),
                    UpdateDashboardPlayer(_kingSaturdayPlayer, "King Saturday!"),
                    UpdateDashboardPlayer(_kingSundayPlayer, "King Sunday!"),
                    UpdateDashboardPlayer(_kingMondayPlayer, "King Monday!")
                };
                await Task.WhenAll(tasks);

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
        if (_isRefreshing)
        {
            return;
        }

        var isInitialLoad = _lastUpdated == default;
        _isRefreshing = true;
        if (!isInitialLoad)
        {
            StateHasChanged();
        }

        try
        {
            Logger.LogInformation("Starting data refresh");
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));

            try
            {
                var tasks = new List<Task>
                {
                    UpdateDashboardPlayer(_kingFridayPlayer,"King Friday!"),
                    UpdateDashboardPlayer(_kingSaturdayPlayer, "King Saturday!"),
                    UpdateDashboardPlayer(_kingSundayPlayer, "King Sunday!"),
                    UpdateDashboardPlayer(_kingMondayPlayer, "King Monday!")
                };
                await Task.WhenAll(tasks).WaitAsync(cts.Token);
                Logger.LogInformation("All data loaded successfully");
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning("Data refresh timed out or was canceled");
            }

            _lastUpdated = DateTime.Now;
            _timeSinceLastUpdate = GetTimeSinceLastUpdate();
            await InvokeAsync(() => DashboardState.SetLastUpdated(DateTime.Now)); // Use InvokeAsync
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

    private async Task UpdateDashboardPlayer(DashboardPlayer dashboardPlayer, string playerName)
    {
        dashboardPlayer.Player = await ApiService.GetLatestPlayerAsync(playerName);
        dashboardPlayer.Stats = await ApiService.GetRankedPlayerAsync(playerName, 1, 30);
        Logger.LogInformation($"Loaded {playerName} with EID: {_kingFridayPlayer.Player?.EID}");

        if (dashboardPlayer.Player != null)
        {
            await InvokeAsync(() => DashboardState.SetPlayerLastUpdated(playerName, dashboardPlayer.Player.Updated));
            dashboardPlayer.SEThisWeek = await PlayerDataService.CalculateSEThisWeekAsync(dashboardPlayer.Player);
            var (prestigesToday, prestigesThisWeek) = await PlayerDataService.CalculatePrestigesAsync(dashboardPlayer.Player);
            dashboardPlayer.Player.PrestigesToday = prestigesToday;
            dashboardPlayer.Player.PrestigesThisWeek = prestigesThisWeek;

            // Calculate DaysToNextTitle
            var projectedDateString = (await ApiService.GetTitleInfoAsync(playerName))?.ProjectedTitleChange ?? dashboardPlayer.Stats?.ProjectedTitleChange;
            dashboardPlayer.DaysToNextTitle = CalculateDaysToNextTitle(projectedDateString);
            dashboardPlayer.Goals = await ApiService.GetPlayerGoalsAsync(playerName);

            var sePlayers = await ApiService.GetSurroundingSEPlayersAsync(playerName, dashboardPlayer.Player.SoulEggs);
            var ebPlayers = await ApiService.GetSurroundingEBPlayersAsync(playerName, dashboardPlayer.Player.EarningsBonusPercentage);
            var merPlayers = await ApiService.GetSurroundingMERPlayersAsync(playerName, (decimal)dashboardPlayer.Player.MER);
            var jerPlayers = await ApiService.GetSurroundingJERPlayersAsync(playerName, (decimal)dashboardPlayer.Player.JER);

            // Always ensure we have valid values for King Friday's progress bars
            dashboardPlayer.SEGoalBegin = sePlayers?.LowerPlayer == null ? new MajPlayerRankingDto { IGN = "Lower SE Player", SEString = "50.000s", Ranking = 2, SENumber = 50.000m } : sePlayers.LowerPlayer;
            dashboardPlayer.SEGoalEnd = sePlayers?.UpperPlayer == null ? new MajPlayerRankingDto { IGN = "Upper SE Player", SEString = "60.000s", Ranking = 0, SENumber = 60.000m } : sePlayers.UpperPlayer;
            dashboardPlayer.EBGoalBegin = ebPlayers?.LowerPlayer == null ? new MajPlayerRankingDto { IGN = "Lower EB Player", EBString = "10.000d%", Ranking = 2 } : ebPlayers.LowerPlayer;
            dashboardPlayer.EBGoalEnd = ebPlayers?.UpperPlayer == null ? new MajPlayerRankingDto { IGN = "Upper EB Player", EBString = "15.000d%", Ranking = 0 } : ebPlayers.UpperPlayer;

            if (merPlayers?.LowerPlayer == null || merPlayers?.UpperPlayer == null)
            {
                dashboardPlayer.MERGoalBegin = new MajPlayerRankingDto { IGN = "Lower MER Player", MER = 40, Ranking = 2 };
                dashboardPlayer.MERGoalEnd = new MajPlayerRankingDto { IGN = "Upper MER Player", MER = 45, Ranking = 0 };
            }
            else
            {
                dashboardPlayer.MERGoalBegin = merPlayers.LowerPlayer;
                dashboardPlayer.MERGoalEnd = merPlayers.UpperPlayer;
            }

            if (jerPlayers?.LowerPlayer == null || jerPlayers?.UpperPlayer == null)
            {
                dashboardPlayer.JERGoalBegin = new MajPlayerRankingDto { IGN = "Lower JER Player", JER = 80, Ranking = 2 };
                dashboardPlayer.JERGoalEnd = new MajPlayerRankingDto { IGN = "Upper JER Player", JER = 90, Ranking = 0 };
            }
            else
            {
                dashboardPlayer.JERGoalBegin = jerPlayers.LowerPlayer;
                dashboardPlayer.JERGoalEnd = jerPlayers.UpperPlayer;
            }

            StateHasChanged();
            await InvokeAsync(StateHasChanged);
        }
    }

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
            _kingFridayPlayer = _kingSaturdayPlayer = _kingSundayPlayer = _kingMondayPlayer = null;

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
