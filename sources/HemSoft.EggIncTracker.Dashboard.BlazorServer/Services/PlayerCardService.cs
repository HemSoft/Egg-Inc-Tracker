namespace HemSoft.EggIncTracker.Dashboard.BlazorServer.Services;

using System.Timers;
using HemSoft.EggIncTracker.Dashboard.BlazorServer.Components.Pages;
using HemSoft.EggIncTracker.Data.Dtos;
using HemSoft.EggIncTracker.Domain;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

public class PlayerCardService
{
    // Event to notify subscribers when data is refreshed
    public event Action<string, DashboardPlayer>? OnPlayerDataRefreshed;

    // Fields for auto-refresh functionality
    private Timer? _refreshTimer;
    private Timer? _updateTimer;
    private DateTime _lastUpdated;
    private string _timeSinceLastUpdate = "Never";
    private bool _isRefreshing;

    // Inject services
    private readonly PlayerDataService _playerDataService;
    private readonly ILogger<PlayerCardService> _logger;
    private readonly DashboardState _dashboardState;

    public PlayerCardService(
        PlayerDataService playerDataService,
        ILogger<PlayerCardService> logger,
        DashboardState dashboardState)
    {
        _playerDataService = playerDataService;
        _logger = logger;
        _dashboardState = dashboardState;
    }

    /// <summary>
    /// Loads a player's dashboard data by name
    /// </summary>
    /// <param name="playerName">The name of the player to load</param>
    /// <param name="cancellationToken">Optional cancellation token</param>
    /// <returns>A DashboardPlayer object with all the player's data</returns>
    public async Task<DashboardPlayer> GetDashboardPlayer(string playerName, CancellationToken cancellationToken = default)
    {
        var dashboardPlayer = new DashboardPlayer();

        try
        {
            // Get player data with timeout
            dashboardPlayer.Player = await PlayerManager.GetLatestPlayerByNameAsync(playerName, _logger);
            dashboardPlayer.Stats = await PlayerManager.GetRankedPlayersAsync(playerName, 1, 30, _logger);

            if (dashboardPlayer.Player != null)
            {
                _dashboardState.SetPlayerLastUpdated(playerName, dashboardPlayer.Player.Updated);
                dashboardPlayer.SEThisWeek = await _playerDataService.CalculateSEThisWeekAsync(dashboardPlayer.Player);
                (dashboardPlayer.Player.PrestigesToday, dashboardPlayer.Player.PrestigesThisWeek) = await _playerDataService.CalculatePrestigesAsync(dashboardPlayer.Player);

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
                    _logger.LogWarning(ex, "Error calculating title information, using defaults");
                }

                // Get Goals with timeout
                dashboardPlayer.Goals = await GoalManager.GetPlayerGoalsAsync(playerName, _logger);

                try
                {
                    // Get extended surrounding SE players
                    var extendedSEPlayers = await MajPlayerRankingManager.GetExtendedSurroundingSEPlayersAsync(playerName, dashboardPlayer.Player.SoulEggs, 3, _logger);
                    dashboardPlayer.SEGoalBegin = extendedSEPlayers?.LowerPlayer ?? new MajPlayerRankingDto { IGN = "Lower SE", SEString = "0s" };
                    dashboardPlayer.SEGoalEnd = extendedSEPlayers?.UpperPlayer ?? new MajPlayerRankingDto { IGN = "Upper SE", SEString = dashboardPlayer.Player.SoulEggs };
                    dashboardPlayer.NextLowerSEPlayers = extendedSEPlayers?.NextLowerPlayers ?? new List<MajPlayerRankingDto>();
                    dashboardPlayer.NextUpperSEPlayers = extendedSEPlayers?.NextUpperPlayers ?? new List<MajPlayerRankingDto>();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error getting surrounding SE players, using defaults");
                    dashboardPlayer.SEGoalBegin = new MajPlayerRankingDto { IGN = "Lower SE", SEString = "0s" };
                    dashboardPlayer.SEGoalEnd = new MajPlayerRankingDto { IGN = "Upper SE", SEString = dashboardPlayer.Player.SoulEggs };
                }

                try
                {
                    // Get extended surrounding EB players
                    var extendedEBPlayers = await MajPlayerRankingManager.GetExtendedSurroundingEBPlayersAsync(playerName, dashboardPlayer.Player.EarningsBonusPercentage, 3, _logger);
                    dashboardPlayer.EBGoalBegin = extendedEBPlayers?.LowerPlayer ?? new MajPlayerRankingDto { IGN = "Lower EB", EBString = "0%" };
                    dashboardPlayer.EBGoalEnd = extendedEBPlayers?.UpperPlayer ?? new MajPlayerRankingDto { IGN = "Upper EB", EBString = dashboardPlayer.Player.EarningsBonusPercentage };
                    dashboardPlayer.NextLowerEBPlayers = extendedEBPlayers?.NextLowerPlayers ?? new List<MajPlayerRankingDto>();
                    dashboardPlayer.NextUpperEBPlayers = extendedEBPlayers?.NextUpperPlayers ?? new List<MajPlayerRankingDto>();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error getting surrounding EB players, using defaults");
                    dashboardPlayer.EBGoalBegin = new MajPlayerRankingDto { IGN = "Lower EB", EBString = "0%" };
                    dashboardPlayer.EBGoalEnd = new MajPlayerRankingDto { IGN = "Upper EB", EBString = dashboardPlayer.Player.EarningsBonusPercentage };
                }

                try
                {
                    decimal merValue = (decimal)dashboardPlayer.Player.MER;
                    // Get extended surrounding MER players
                    var extendedMERPlayers = await MajPlayerRankingManager.GetExtendedSurroundingMERPlayersAsync(playerName, merValue, 3, _logger);
                    dashboardPlayer.MERGoalBegin = extendedMERPlayers?.LowerPlayer ?? new MajPlayerRankingDto { IGN = "Lower MER", MER = 0m };
                    dashboardPlayer.MERGoalEnd = extendedMERPlayers?.UpperPlayer ?? new MajPlayerRankingDto { IGN = "Upper MER", MER = merValue };
                    dashboardPlayer.NextLowerMERPlayers = extendedMERPlayers?.NextLowerPlayers ?? new List<MajPlayerRankingDto>();
                    dashboardPlayer.NextUpperMERPlayers = extendedMERPlayers?.NextUpperPlayers ?? new List<MajPlayerRankingDto>();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error getting surrounding MER players, using defaults");
                    dashboardPlayer.MERGoalBegin = new MajPlayerRankingDto { IGN = "Lower MER", MER = 0m };
                    dashboardPlayer.MERGoalEnd = new MajPlayerRankingDto { IGN = "Upper MER", MER = (decimal)dashboardPlayer.Player.MER };
                }

                try
                {
                    decimal jerValue = (decimal)dashboardPlayer.Player.JER;
                    // Get extended surrounding JER players
                    var extendedJERPlayers = await MajPlayerRankingManager.GetExtendedSurroundingJERPlayersAsync(playerName, jerValue, 3, _logger);
                    dashboardPlayer.JERGoalBegin = extendedJERPlayers?.LowerPlayer ?? new MajPlayerRankingDto { IGN = "Lower JER", JER = 0m };
                    dashboardPlayer.JERGoalEnd = extendedJERPlayers?.UpperPlayer ?? new MajPlayerRankingDto { IGN = "Upper JER", JER = jerValue };
                    dashboardPlayer.NextLowerJERPlayers = extendedJERPlayers?.NextLowerPlayers ?? new List<MajPlayerRankingDto>();
                    dashboardPlayer.NextUpperJERPlayers = extendedJERPlayers?.NextUpperPlayers ?? new List<MajPlayerRankingDto>();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error getting surrounding JER players, using defaults");
                    dashboardPlayer.JERGoalBegin = new MajPlayerRankingDto { IGN = "Lower JER", JER = 0m };
                    dashboardPlayer.JERGoalEnd = new MajPlayerRankingDto { IGN = "Upper JER", JER = (decimal)dashboardPlayer.Player.JER };
                }

                var seValue = dashboardPlayer.SEThisWeek; // Capture value
                _dashboardState.SetPlayerSEThisWeek(playerName, seValue);
            }
            else
            {
                _logger.LogWarning("Failed to load player data for {PlayerName}", playerName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dashboard player data for {PlayerName}", playerName);
        }

        return dashboardPlayer;
    }

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
            _logger.LogWarning("Projected title change date is MinValue, returning 0 days.");
            return 0;
        }

        var daysUntil = (projectedDate - DateTime.Now).Days;
        return Math.Max(0, daysUntil); // Ensure it's not negative
    }

    /// <summary>
    /// Sets up timers for auto-refreshing data
    /// </summary>
    public void SetupAutoRefreshTimer()
    {
        try
        {
            _refreshTimer?.Dispose(); // Dispose existing timer if any
            _updateTimer?.Dispose();

            _refreshTimer = new Timer(1800000); // 30 minutes
            _refreshTimer.Elapsed += OnRefreshTimerElapsed;
            _refreshTimer.AutoReset = true;
            _refreshTimer.Start();
            _logger.LogInformation("Main refresh timer started (30 minute interval)");

            _updateTimer = new Timer(1000); // 1 second
            _updateTimer.Elapsed += OnUpdateTimerElapsed;
            _updateTimer.AutoReset = true;
            _updateTimer.Start();
            _logger.LogInformation("Update timer started (1 second interval)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting up timers");
        }
    }

    /// <summary>
    /// Updates the time since last update display
    /// </summary>
    private async void OnUpdateTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            if (_lastUpdated != default)
            {
                // Notify subscribers
                OnPlayerDataRefreshed?.Invoke("", new DashboardPlayer()); // Just to trigger UI updates
            }
        }
        catch (ObjectDisposedException) { _updateTimer?.Stop(); _logger.LogInformation("Update timer stopped due to component disposal"); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating time display"); }
    }

    /// <summary>
    /// Refreshes player data on timer elapsed
    /// </summary>
    private async void OnRefreshTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            if (_isRefreshing)
            {
                _logger.LogDebug("Refresh skipped because a refresh is already in progress.");
                return;
            }

            // Refresh data for all players
            await RefreshPlayerData("King Friday!");
            await RefreshPlayerData("King Saturday!");
            await RefreshPlayerData("King Sunday!");
            await RefreshPlayerData("King Monday!");
        }
        catch (ObjectDisposedException) { _refreshTimer?.Stop(); _logger.LogInformation("Refresh timer stopped due to component disposal"); }
        catch (Exception ex) { _logger.LogError(ex, "Timer refresh error"); }
    }

    /// <summary>
    /// Refreshes data for a specific player
    /// </summary>
    public async Task RefreshPlayerData(string playerName)
    {
        if (_isRefreshing)
        {
            _logger.LogDebug("Refresh skipped because a refresh is already in progress.");
            return;
        }

        var isInitialLoad = _lastUpdated == default;
        _isRefreshing = true;

        try
        {
            var updatedPlayer = await GetDashboardPlayer(playerName);
            _lastUpdated = DateTime.Now;
            _timeSinceLastUpdate = GetTimeSinceLastUpdate();
            _dashboardState.SetLastUpdated(DateTime.Now);

            // Notify subscribers of the updated data
            OnPlayerDataRefreshed?.Invoke(playerName, updatedPlayer);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Data refresh timed out or was canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during player data update in RefreshPlayerData");
        }
        finally
        {
            _isRefreshing = false;
        }
    }

    /// <summary>
    /// Performs initial data load with retry logic
    /// </summary>
    public async Task<DashboardPlayer> InitialDataLoad(string playerName, int maxRetries = 3)
    {
        Exception? lastException = null;
        DashboardPlayer result = new DashboardPlayer();

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Initial data load attempt {Attempt} of {MaxRetries} for {PlayerName}", attempt + 1, maxRetries, playerName);
                _isRefreshing = true;

                // Use a timeout to prevent hanging
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                // Load player data
                result = await GetDashboardPlayer(playerName, cts.Token);

                _lastUpdated = DateTime.Now;
                _timeSinceLastUpdate = GetTimeSinceLastUpdate();
                _dashboardState.SetLastUpdated(DateTime.Now);

                _isRefreshing = false;
                _logger.LogInformation("Initial data load completed successfully for {PlayerName}", playerName);
                return result;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Initial data load timed out for {PlayerName} (attempt {Attempt})", playerName, attempt + 1);
                if (attempt < maxRetries - 1)
                {
                    int delayMs = 500 * (int)Math.Pow(2, attempt);
                    _logger.LogInformation("Retrying in {DelayMs}ms...", delayMs);
                    await Task.Delay(delayMs);
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
                _logger.LogError(ex, "Error during initial data load for {PlayerName} (attempt {Attempt})", playerName, attempt + 1);
                if (attempt < maxRetries - 1)
                {
                    int delayMs = 500 * (int)Math.Pow(2, attempt);
                    _logger.LogInformation("Retrying in {DelayMs}ms...", delayMs);
                    await Task.Delay(delayMs);
                }
            }
            finally
            {
                if (attempt == maxRetries - 1) // If last attempt failed
                {
                    _isRefreshing = false;
                }
            }
        }

        // If loop completes without returning, all retries failed
        _logger.LogError(lastException, "All initial data load attempts failed for {PlayerName}", playerName);
        return CreateEmptyDashboardPlayer(playerName);
    }

    /// <summary>
    /// Creates an empty DashboardPlayer with default values
    /// </summary>
    public DashboardPlayer CreateEmptyDashboardPlayer(string playerName)
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
            NextLowerSEPlayers = new List<MajPlayerRankingDto>(),
            NextUpperSEPlayers = new List<MajPlayerRankingDto>(),
            EBGoalBegin = new MajPlayerRankingDto { IGN = "Lower EB", EBString = "0%" },
            EBGoalEnd = new MajPlayerRankingDto { IGN = "Upper EB", EBString = "0%" },
            NextLowerEBPlayers = new List<MajPlayerRankingDto>(),
            NextUpperEBPlayers = new List<MajPlayerRankingDto>(),
            MERGoalBegin = new MajPlayerRankingDto { IGN = "Lower MER", MER = 0m },
            MERGoalEnd = new MajPlayerRankingDto { IGN = "Upper MER", MER = 0m },
            NextLowerMERPlayers = new List<MajPlayerRankingDto>(),
            NextUpperMERPlayers = new List<MajPlayerRankingDto>(),
            JERGoalBegin = new MajPlayerRankingDto { IGN = "Lower JER", JER = 0m },
            JERGoalEnd = new MajPlayerRankingDto { IGN = "Upper JER", JER = 0m },
            NextLowerJERPlayers = new List<MajPlayerRankingDto>(),
            NextUpperJERPlayers = new List<MajPlayerRankingDto>()
        };
    }

    /// <summary>
    /// Gets a formatted string representing the time since last update
    /// </summary>
    public string GetTimeSinceLastUpdate()
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

    public void Dispose()
    {
        _refreshTimer?.Stop();
        _refreshTimer?.Dispose();
        _updateTimer?.Stop();
        _updateTimer?.Dispose();
    }
}
