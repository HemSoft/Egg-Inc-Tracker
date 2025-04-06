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
            // Get player updates from API
            var rankings = await ApiService.GetLatestMajPlayerRankingsAsync(100); // Increased limit to show more data
            if (rankings != null)
            {
                playerUpdates = rankings;
            }
            else
            {
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
}

