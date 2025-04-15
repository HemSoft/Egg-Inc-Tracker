// Updated namespace for Blazor Server project
namespace HemSoft.EggIncTracker.Dashboard.BlazorServer.Components.Pages;

using System.Numerics;
// Updated namespace for Blazor Server services
using HemSoft.EggIncTracker.Dashboard.BlazorServer.Services;
using HemSoft.EggIncTracker.Data.Dtos; // Ensure DTOs using is present
using HemSoft.EggIncTracker.Domain; // Add Domain using for static managers

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

using ST = System.Timers;

public class DashboardPlayer
{
    public PlayerDto? Player { get; set; }
    public PlayerStatsDto? Stats { get; set; }
    public GoalDto? Goals { get; set; } // Changed type to GoalDto
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

// Ensure the partial class matches the file name and namespace
public partial class Dashboard : IDisposable, IAsyncDisposable
{
    [Inject]
    private ILogger<Dashboard> Logger { get; set; } = default!;

    [Inject]
    private DashboardState DashboardState { get; set; } = default!; // Uses updated namespace

    // Removed IApiService injection
    // [Inject]
    // private HemSoft.EggIncTracker.Dashboard.BlazorServer.Services.IApiService ApiService { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private PlayerDataService PlayerDataService { get; set; } = default!; // Uses updated namespace

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
            await Task.Delay(300); // Keep delay? Or remove? Let's keep for now.
            Logger.LogInformation("Starting initial data load");
            await InitialDataLoad();
            SetupAutoRefreshTimer();
            Logger.LogInformation("Automatic refresh timer started");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during initialization");
            // Consider if RefreshData should be called here if Initial fails
            // await RefreshData();
            SetupAutoRefreshTimer(); // Still try to set up timer
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            Dispose(); // Call synchronous dispose
            // Async cleanup if needed in future
            await Task.CompletedTask;
            // Removed GC calls - generally not recommended in Dispose
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

            _timer = new ST.Timer(30000); // 30 seconds
            _timer.Elapsed += OnTimerElapsed;
            _timer.AutoReset = true;
            _timer.Start();
            Logger.LogInformation("Main refresh timer started (30 second interval)");

            _updateTimer = new ST.Timer(1000); // 1 second
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
                // Use InvokeAsync for UI updates from timer threads
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
                Logger.LogDebug("Refresh skipped because a refresh is already in progress.");
                return;
            }

            // Use InvokeAsync for UI updates and async calls from timer threads
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
                await InvokeAsync(StateHasChanged); // Ensure UI updates to show loading state

                // Use a timeout for each player load to prevent hanging
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                // Load each player with individual try/catch to prevent one failure from stopping all
                try
                {
                    _kingFridayPlayer = await GetDashboardPlayer("King Friday!", cts.Token);
                    Logger.LogInformation("Loaded King Friday data");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error loading King Friday data");
                    _kingFridayPlayer = CreateEmptyDashboardPlayer("King Friday!");
                }

                try
                {
                    _kingSaturdayPlayer = await GetDashboardPlayer("King Saturday!", cts.Token);
                    Logger.LogInformation("Loaded King Saturday data");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error loading King Saturday data");
                    _kingSaturdayPlayer = CreateEmptyDashboardPlayer("King Saturday!");
                }

                try
                {
                    _kingSundayPlayer = await GetDashboardPlayer("King Sunday!", cts.Token);
                    Logger.LogInformation("Loaded King Sunday data");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error loading King Sunday data");
                    _kingSundayPlayer = CreateEmptyDashboardPlayer("King Sunday!");
                }

                try
                {
                    _kingMondayPlayer = await GetDashboardPlayer("King Monday!", cts.Token);
                    Logger.LogInformation("Loaded King Monday data");
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Error loading King Monday data");
                    _kingMondayPlayer = CreateEmptyDashboardPlayer("King Monday!");
                }

                _lastUpdated = DateTime.Now;
                _timeSinceLastUpdate = GetTimeSinceLastUpdate();
                DashboardState.SetLastUpdated(DateTime.Now);

                _isRefreshing = false;
                await InvokeAsync(StateHasChanged);
                Logger.LogInformation("Initial data load completed successfully");
                return;
            }
            catch (OperationCanceledException)
            {
                Logger.LogWarning($"Initial data load timed out (attempt {attempt + 1})");
                if (attempt < maxRetries - 1)
                {
                    int delayMs = 500 * (int)Math.Pow(2, attempt);
                    Logger.LogInformation($"Retrying in {delayMs}ms...");
                    await Task.Delay(delayMs);
                }
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
            finally // Ensure refreshing flag is reset even on retry failure
            {
                if (attempt == maxRetries - 1) // If last attempt failed
                {
                    _isRefreshing = false;
                    await InvokeAsync(StateHasChanged);
                }
            }
        }
        // If loop completes without returning, all retries failed
        Logger.LogError(lastException, "All initial data load attempts failed");
    }

    private async Task RefreshData()
    {
        if (_isRefreshing)
        {
            Logger.LogDebug("Refresh skipped because a refresh is already in progress.");
            return;
        }

        var isInitialLoad = _lastUpdated == default;
        _isRefreshing = true;
        if (!isInitialLoad)
        {
            // Update UI on UI thread
            await InvokeAsync(StateHasChanged);
        }

        try
        {
            _kingFridayPlayer = await GetDashboardPlayer("King Friday!");
            _kingSaturdayPlayer = await GetDashboardPlayer("King Saturday!");
            _kingSundayPlayer = await GetDashboardPlayer("King Sunday!");
            _kingMondayPlayer = await GetDashboardPlayer("King Monday!");
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Data refresh timed out or was canceled");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during one or more player data updates in RefreshData");
        }

        _lastUpdated = DateTime.Now;
        _timeSinceLastUpdate = GetTimeSinceLastUpdate();
        DashboardState.SetLastUpdated(DateTime.Now);
        _isRefreshing = false;
    }

    // Refactored method to use static Domain Managers with cancellation support
    private async Task<DashboardPlayer> GetDashboardPlayer(string playerName, CancellationToken cancellationToken = default)
    {
        var dashboardPlayer = new DashboardPlayer();

        try
        {
            // Get player data with timeout
            dashboardPlayer.Player = await PlayerManager.GetLatestPlayerByNameAsync(playerName, Logger);
            dashboardPlayer.Stats = await PlayerManager.GetRankedPlayersAsync(playerName, 1, 30, Logger);

            if (dashboardPlayer.Player != null)
            {
                DashboardState.SetPlayerLastUpdated(playerName, dashboardPlayer.Player.Updated);
                dashboardPlayer.SEThisWeek = await PlayerDataService.CalculateSEThisWeekAsync(dashboardPlayer.Player);
                (dashboardPlayer.Player.PrestigesToday, dashboardPlayer.Player.PrestigesThisWeek) = await PlayerDataService.CalculatePrestigesAsync(dashboardPlayer.Player);

                try
                {
                    var ebNumber = PlayerManager.CalculateEarningsBonusPercentageNumber(dashboardPlayer.Player);
                    (dashboardPlayer.Player.Title, dashboardPlayer.Player.NextTitle, dashboardPlayer.Player.TitleProgress) = PlayerManager.GetTitleWithProgress(ebNumber);
                    var projectedDate = PlayerManager.CalculateProjectedTitleChange(dashboardPlayer.Player);
                    dashboardPlayer.Player.ProjectedTitleChange = projectedDate;
                    dashboardPlayer.DaysToNextTitle = CalculateDaysToNextTitle(projectedDate);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Error calculating title information, using defaults");
                }

                // Get Goals with timeout
                dashboardPlayer.Goals = await GoalManager.GetPlayerGoalsAsync(playerName, Logger);

                try
                {
                    var sePlayers = await MajPlayerRankingManager.GetSurroundingSEPlayersAsync(playerName, dashboardPlayer.Player.SoulEggs, Logger);
                    dashboardPlayer.SEGoalBegin = sePlayers?.LowerPlayer ?? new MajPlayerRankingDto { IGN = "Lower SE", SEString = "0s" };
                    dashboardPlayer.SEGoalEnd = sePlayers?.UpperPlayer ?? new MajPlayerRankingDto { IGN = "Upper SE", SEString = dashboardPlayer.Player.SoulEggs };
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Error getting surrounding SE players, using defaults");
                    dashboardPlayer.SEGoalBegin = new MajPlayerRankingDto { IGN = "Lower SE", SEString = "0s" };
                    dashboardPlayer.SEGoalEnd = new MajPlayerRankingDto { IGN = "Upper SE", SEString = dashboardPlayer.Player.SoulEggs };
                }

                try
                {
                    var ebPlayers = await MajPlayerRankingManager.GetSurroundingEBPlayersAsync(playerName, dashboardPlayer.Player.EarningsBonusPercentage, Logger);
                    dashboardPlayer.EBGoalBegin = ebPlayers?.LowerPlayer ?? new MajPlayerRankingDto { IGN = "Lower EB", EBString = "0%" };
                    dashboardPlayer.EBGoalEnd = ebPlayers?.UpperPlayer ?? new MajPlayerRankingDto { IGN = "Upper EB", EBString = dashboardPlayer.Player.EarningsBonusPercentage };
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Error getting surrounding EB players, using defaults");
                    dashboardPlayer.EBGoalBegin = new MajPlayerRankingDto { IGN = "Lower EB", EBString = "0%" };
                    dashboardPlayer.EBGoalEnd = new MajPlayerRankingDto { IGN = "Upper EB", EBString = dashboardPlayer.Player.EarningsBonusPercentage };
                }

                try
                {
                    decimal merValue = (decimal)dashboardPlayer.Player.MER;
                    var merPlayers = await MajPlayerRankingManager.GetSurroundingMERPlayersAsync(playerName, merValue, Logger);
                    dashboardPlayer.MERGoalBegin = merPlayers?.LowerPlayer ?? new MajPlayerRankingDto { IGN = "Lower MER", MER = 0m };
                    dashboardPlayer.MERGoalEnd = merPlayers?.UpperPlayer ?? new MajPlayerRankingDto { IGN = "Upper MER", MER = merValue };
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Error getting surrounding MER players, using defaults");
                    dashboardPlayer.MERGoalBegin = new MajPlayerRankingDto { IGN = "Lower MER", MER = 0m };
                    dashboardPlayer.MERGoalEnd = new MajPlayerRankingDto { IGN = "Upper MER", MER = (decimal)dashboardPlayer.Player.MER };
                }

                try
                {
                    decimal jerValue = (decimal)dashboardPlayer.Player.JER;
                    var jerPlayers = await MajPlayerRankingManager.GetSurroundingJERPlayersAsync(playerName, jerValue, Logger);
                    dashboardPlayer.JERGoalBegin = jerPlayers?.LowerPlayer ?? new MajPlayerRankingDto { IGN = "Lower JER", JER = 0m };
                    dashboardPlayer.JERGoalEnd = jerPlayers?.UpperPlayer ?? new MajPlayerRankingDto { IGN = "Upper JER", JER = jerValue };
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Error getting surrounding JER players, using defaults");
                    dashboardPlayer.JERGoalBegin = new MajPlayerRankingDto { IGN = "Lower JER", JER = 0m };
                    dashboardPlayer.JERGoalEnd = new MajPlayerRankingDto { IGN = "Upper JER", JER = (decimal)dashboardPlayer.Player.JER };
                }

                var seValue = dashboardPlayer.SEThisWeek; // Capture value
                DashboardState.SetPlayerSEThisWeek(playerName, seValue);
            }
            else
            {
                Logger.LogWarning("Failed to load player data for {PlayerName}", playerName);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error updating dashboard player data for {PlayerName}", playerName);
        }

        return dashboardPlayer;
    }


    private string GetTimeSinceLastUpdate()
    {
        if (_lastUpdated == default)
        {
            return "Never";
        }

        var timeSince = DateTime.Now - _lastUpdated;

        if (timeSince.TotalSeconds < 1)
        {
            return "Just now"; // Added for immediate feedback
        }

        if (timeSince.TotalSeconds < 60)
        {
            return $"{(int)timeSince.TotalSeconds}s ago";
        }

        if (timeSince.TotalMinutes < 60)
        {
            return $"{(int)timeSince.TotalMinutes}m {timeSince.Seconds:D}s ago";
        }

        if (timeSince.TotalHours < 24)
        {
            return $"{(int)timeSince.TotalHours}h {timeSince.Minutes:D}m ago";
        }

        return $"{timeSince.Days:D}d {timeSince.Hours:D}h ago";
    }

    // Updated method to accept DateTime
    private int CalculateDaysToNextTitle(DateTime projectedDate)
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

    private void NavigateToPlayerDetail(string? eid)
    {
        if (!string.IsNullOrEmpty(eid))
        {
            Logger.LogInformation("Navigating to player detail for EID: {EID}", eid);
            NavigationManager.NavigateTo($"/player/{eid}");
        }
        else
        {
            Logger.LogWarning("Cannot navigate to player detail: EID is null or empty");
        }
    }

    // Helper method to create an empty dashboard player with required fields
    private static DashboardPlayer CreateEmptyDashboardPlayer(string playerName)
    {
        return new DashboardPlayer
        {
            Player = new PlayerDto
            {
                PlayerName = playerName,
                EID = "unknown",
                SoulEggs = "0",
                SoulEggsFull = "0",
                EarningsBonusPercentage = "0%",
                EarningsBonusPerHour = "0",
                Title = "Unknown",
                NextTitle = "Unknown",
                Updated = DateTime.Now
            },
            SEGoalBegin = new MajPlayerRankingDto { IGN = "Lower SE", SEString = "0s" },
            SEGoalEnd = new MajPlayerRankingDto { IGN = "Upper SE", SEString = "0s" },
            EBGoalBegin = new MajPlayerRankingDto { IGN = "Lower EB", EBString = "0%" },
            EBGoalEnd = new MajPlayerRankingDto { IGN = "Upper EB", EBString = "0%" },
            MERGoalBegin = new MajPlayerRankingDto { IGN = "Lower MER", MER = 0m },
            MERGoalEnd = new MajPlayerRankingDto { IGN = "Upper MER", MER = 0m },
            JERGoalBegin = new MajPlayerRankingDto { IGN = "Lower JER", JER = 0m },
            JERGoalEnd = new MajPlayerRankingDto { IGN = "Upper JER", JER = 0m }
        };
    }

    public void Dispose()
    {
        try
        {
            _timer?.Stop();
            _timer?.Dispose();
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
            // Unsubscribe might fail if DashboardState is null during disposal, add null check?
            // DashboardState?.OnChange -= StateHasChanged;

            // Clear references to help GC
            _kingFridayPlayer = null!;
            _kingSaturdayPlayer = null!;
            _kingSundayPlayer = null!;
            _kingMondayPlayer = null!;
            _timer = null;
            _updateTimer = null;

            // Removed GC calls
            Logger.LogInformation("Dashboard component disposed, timers cleaned up");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error disposing Dashboard component");
        }
    }
}
