// Updated namespace for Blazor Server project
namespace HemSoft.EggIncTracker.Dashboard.BlazorServer.Components.Pages;

using System.Numerics;
using HemSoft.EggIncTracker.Dashboard.BlazorServer.Services;
using HemSoft.EggIncTracker.Data.Dtos;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

public class DashboardPlayer
{
    public PlayerDto? Player { get; set; }
    public PlayerStatsDto? Stats { get; set; }
    public GoalDto? Goals { get; set; }
    public int DaysToNextTitle { get; set; }
    public MajPlayerRankingDto? SEGoalBegin { get; set; }
    public MajPlayerRankingDto? SEGoalEnd { get; set; }
    public List<MajPlayerRankingDto> NextLowerSEPlayers { get; set; } = new List<MajPlayerRankingDto>();
    public List<MajPlayerRankingDto> NextUpperSEPlayers { get; set; } = new List<MajPlayerRankingDto>();
    public MajPlayerRankingDto? EBGoalBegin { get; set; }
    public MajPlayerRankingDto? EBGoalEnd { get; set; }
    public List<MajPlayerRankingDto> NextLowerEBPlayers { get; set; } = new List<MajPlayerRankingDto>();
    public List<MajPlayerRankingDto> NextUpperEBPlayers { get; set; } = new List<MajPlayerRankingDto>();
    public MajPlayerRankingDto? MERGoalBegin { get; set; }
    public MajPlayerRankingDto? MERGoalEnd { get; set; }
    public List<MajPlayerRankingDto> NextLowerMERPlayers { get; set; } = new List<MajPlayerRankingDto>();
    public List<MajPlayerRankingDto> NextUpperMERPlayers { get; set; } = new List<MajPlayerRankingDto>();
    public MajPlayerRankingDto? JERGoalBegin { get; set; }
    public MajPlayerRankingDto? JERGoalEnd { get; set; }
    public List<MajPlayerRankingDto> NextLowerJERPlayers { get; set; } = new List<MajPlayerRankingDto>();
    public List<MajPlayerRankingDto> NextUpperJERPlayers { get; set; } = new List<MajPlayerRankingDto>();
    public BigInteger? SEThisWeek { get; set; }
}

// Ensure the partial class matches the file name and namespace
public partial class Dashboard : IDisposable, IAsyncDisposable
{
    [Inject]
    private ILogger<Dashboard> Logger { get; set; } = default!;

    [Inject]
    private DashboardState DashboardState { get; set; } = default!;

    [Inject]
    private NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    private PlayerCardService PlayerCardService { get; set; } = default!;

    private DateTime _lastUpdated;
    private string _timeSinceLastUpdate = "Never";
    private const bool isLoading = false;

    private DashboardPlayer _kingFridayPlayer = new();
    private DashboardPlayer _kingSaturdayPlayer = new();
    private DashboardPlayer _kingSundayPlayer = new();
    private DashboardPlayer _kingMondayPlayer = new();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await Task.Delay(300);
            await InitialDataLoad();
            PlayerCardService.SetupAutoRefreshTimer();
            PlayerCardService.OnPlayerDataRefreshed += OnPlayerDataRefreshed;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during initialization");
            PlayerCardService.SetupAutoRefreshTimer(); // Still try to set up timer
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            Dispose();
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during async disposal of Dashboard component");
        }
    }

    private async void OnPlayerDataRefreshed(string playerName, DashboardPlayer updatedPlayer)
    {
        try
        {
            // Update the appropriate player object based on the player name
            switch (playerName)
            {
                case "King Friday!":
                    _kingFridayPlayer = updatedPlayer;
                    break;
                case "King Saturday!":
                    _kingSaturdayPlayer = updatedPlayer;
                    break;
                case "King Sunday!":
                    _kingSundayPlayer = updatedPlayer;
                    break;
                case "King Monday!":
                    _kingMondayPlayer = updatedPlayer;
                    break;
            }

            _lastUpdated = DateTime.Now;
            _timeSinceLastUpdate = PlayerCardService.GetTimeSinceLastUpdate();

            // Update UI
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error handling player data refresh for {PlayerName}", playerName);
        }
    }

    private async Task InitialDataLoad()
    {
        try
        {
            // Load each player using PlayerCardService's InitialDataLoad method
            _kingFridayPlayer = await PlayerCardService.InitialDataLoad("King Friday!");
            _kingSaturdayPlayer = await PlayerCardService.InitialDataLoad("King Saturday!");
            _kingSundayPlayer = await PlayerCardService.InitialDataLoad("King Sunday!");
            _kingMondayPlayer = await PlayerCardService.InitialDataLoad("King Monday!");

            _lastUpdated = DateTime.Now;
            _timeSinceLastUpdate = PlayerCardService.GetTimeSinceLastUpdate();
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error during initial data load");
        }
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

    public void Dispose()
    {
        try
        {
            // Unsubscribe from PlayerCardService events
            if (PlayerCardService != null)
            {
                PlayerCardService.OnPlayerDataRefreshed -= OnPlayerDataRefreshed;
            }

            // Clear references to help GC
            _kingFridayPlayer = null!;
            _kingSaturdayPlayer = null!;
            _kingSundayPlayer = null!;
            _kingMondayPlayer = null!;

            Logger.LogInformation("Dashboard component disposed");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error disposing Dashboard component");
        }
    }
}
