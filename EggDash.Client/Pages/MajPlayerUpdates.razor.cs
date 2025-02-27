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
    private EggIncContext DbContext { get; set; } = default!;

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
            // Get total count for pagination
            var totalRecords = await DbContext.MajPlayerRankings.CountAsync();
            totalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            // Get page of player updates
            playerUpdates = MajPlayerRankingManager.GetMajPlayerRankings();
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

