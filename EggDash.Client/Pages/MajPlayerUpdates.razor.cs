namespace EggDash.Client.Pages;

using System.Globalization;

using EggDash.Client.Services;

using HemSoft.EggIncTracker.Data.Dtos;
using HemSoft.EggIncTracker.Domain;

using Microsoft.AspNetCore.Components;

using MudBlazor;

using ST = System.Timers;

public partial class MajPlayerUpdates
{
    [Inject]
    private ApiService ApiService { get; set; } = default!;

    [Inject]
    private ILogger<MajPlayerUpdates> Logger { get; set; } = default!;

    private List<MajPlayerRankingDto> playerUpdates = new();
    private bool isLoading = true;
    private string searchString = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        await LoadPlayerUpdates();
    }

    private async Task LoadPlayerUpdates()
    {
        isLoading = true;
        StateHasChanged();

        try
        {
            Logger.LogInformation("Fetching player updates from API...");
            // Get player updates from API
            var rankings = await ApiService.GetLatestMajPlayerRankingsAsync(500); // Significantly increased limit to ensure we get all data

            if (rankings != null)
            {
                Logger.LogInformation($"Received {rankings.Count} player updates from API");
                playerUpdates = rankings;

                // Debug: Check if we have King Friday data
                var kingFridayEntries = rankings.Where(r => r.IGN.Contains("King Friday")).ToList();
                Logger.LogInformation($"Found {kingFridayEntries.Count} entries for King Friday");

                foreach (var entry in kingFridayEntries.Take(3))
                {
                    Logger.LogInformation($"King Friday entry: {entry.Updated}, SE: {entry.SEString}, SEGainsWeek: {entry.SEGainsWeek}");
                }
            }
            else
            {
                Logger.LogWarning("API returned null for player updates");
                playerUpdates = new List<MajPlayerRankingDto>();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading player updates");
            playerUpdates = new List<MajPlayerRankingDto>();
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    // Filter function for the MudTable
    private bool FilterFunc(MajPlayerRankingDto player)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return true;

        // Check if either IGN or DiscordName contains the search string (case-insensitive)
        bool matchesIGN = player.IGN?.Contains(searchString, StringComparison.OrdinalIgnoreCase) ?? false;
        bool matchesDiscord = player.DiscordName?.Contains(searchString, StringComparison.OrdinalIgnoreCase) ?? false;

        // Log when we find a match for debugging
        if (matchesIGN || matchesDiscord)
        {
            Logger.LogInformation($"Filter match found: {player.IGN} (Discord: {player.DiscordName})");
        }

        return matchesIGN || matchesDiscord;
    }
}

