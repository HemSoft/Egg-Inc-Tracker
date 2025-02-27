namespace EggDash.Client.Pages;

using System.Globalization;

using EggDash.Client.Services;

using HemSoft.EggIncTracker.Data;
using HemSoft.EggIncTracker.Data.Dtos;
using HemSoft.EggIncTracker.Domain;

using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

using MudBlazor;

using ST = System.Timers;

public partial class MajPlayerUpdates
{
    [Inject]
    private PlayerApiClient ApiClient { get; set; } = default!;

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
            playerUpdates = await ApiClient.GetMajPlayerRankingsAsync(pageSize);
            
            // For pagination, you might need to add a method to get the count
            // For now, just use the list length
            totalPages = (int)Math.Ceiling(playerUpdates.Count / (double)pageSize);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading player updates: {ex.Message}");
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }
}

