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
    private int currentPage = 1;
    private int pageSize = 10;
    private int totalPages = 1;

    protected override async Task OnInitializedAsync()
    {
        await LoadPlayerUpdates();
    }

    private async Task OnPageChanged(int page)
    {
        currentPage = page;
        await LoadPlayerUpdates();
    }

    private async Task LoadPlayerUpdates()
    {
        isLoading = true;
        StateHasChanged();

        try
        {
            // Get player updates from API
            var rankings = await ApiService.GetLatestMajPlayerRankingsAsync(30);
            if (rankings != null)
            {
                playerUpdates = rankings;
                totalPages = (int)Math.Ceiling(playerUpdates.Count / (double)pageSize);
            }
            else
            {
                playerUpdates = new List<MajPlayerRankingDto>();
                totalPages = 1;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading player updates");
            playerUpdates = new List<MajPlayerRankingDto>();
            totalPages = 1;
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }
}

