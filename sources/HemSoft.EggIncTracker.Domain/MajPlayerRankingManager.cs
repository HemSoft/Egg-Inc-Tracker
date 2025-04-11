namespace HemSoft.EggIncTracker.Domain;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using HemSoft.EggIncTracker.Data.Dtos;

using Microsoft.Extensions.Logging;

/// <summary>
/// Interface for API service to avoid direct dependency on EggDash.Client
/// </summary>
public interface IApiService
{
    Task<SurroundingPlayersDto?> GetSurroundingSEPlayersAsync(string playerName, string soulEggs);
    Task<SurroundingPlayersDto?> GetSurroundingEBPlayersAsync(string playerName, string earningsBonus);
    Task<SurroundingPlayersDto?> GetSurroundingMERPlayersAsync(string playerName, decimal mer);
    Task<SurroundingPlayersDto?> GetSurroundingJERPlayersAsync(string playerName, decimal jer);
    Task<List<MajPlayerRankingDto>?> GetLatestMajPlayerRankingsAsync(int limit = 30);
    Task<SaveRankingResponseDto?> SaveMajPlayerRankingAsync(MajPlayerRankingDto majPlayerRanking);
}

/// <summary>
/// DTO for surrounding players response
/// </summary>
public class SurroundingPlayersDto
{
    public MajPlayerRankingDto LowerPlayer { get; set; }
    public MajPlayerRankingDto UpperPlayer { get; set; }
}

/// <summary>
/// DTO for save ranking response
/// </summary>
public class SaveRankingResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public MajPlayerRankingDto PreviousRanking { get; set; }
}

public static class MajPlayerRankingManager
{
    public static async Task<(MajPlayerRankingDto, MajPlayerRankingDto)> GetSurroundingEBPlayers(string playerName, string eb)
    {
        try
        {
            // API client should be passed as a parameter or injected
            var apiClient = ServiceLocator.GetService<IApiService>();
            if (apiClient == null)
            {
                throw new InvalidOperationException("ApiService is not available");
            }

            var surroundingPlayers = await apiClient.GetSurroundingEBPlayersAsync(playerName, eb);
            return (surroundingPlayers?.LowerPlayer, surroundingPlayers?.UpperPlayer);
        }
        catch (Exception ex)
        {
            var logger = ServiceLocator.GetService<ILogger>();
            logger?.LogError(ex, $"Error in GetSurroundingEBPlayers for player {playerName} with EB {eb}");
            throw;
        }
    }

    public static async Task<(MajPlayerRankingDto, MajPlayerRankingDto)> GetSurroundingSEPlayers(string playerName, string se)
    {
        try
        {
            // API client should be passed as a parameter or injected
            var apiClient = ServiceLocator.GetService<IApiService>();
            if (apiClient == null)
            {
                throw new InvalidOperationException("ApiService is not available");
            }

            var surroundingPlayers = await apiClient.GetSurroundingSEPlayersAsync(playerName, se);
            return (surroundingPlayers?.LowerPlayer, surroundingPlayers?.UpperPlayer);
        }
        catch (Exception ex)
        {
            var logger = ServiceLocator.GetService<ILogger>();
            logger?.LogError(ex, $"Error in GetSurroundingSEPlayers for player {playerName} with SE {se}");
            throw;
        }
    }

    public static async Task<(MajPlayerRankingDto, MajPlayerRankingDto)> GetSurroundingMERPlayers(string playerName, decimal mer)
    {
        try
        {
            // API client should be passed as a parameter or injected
            var apiClient = ServiceLocator.GetService<IApiService>();
            if (apiClient == null)
            {
                throw new InvalidOperationException("ApiService is not available");
            }

            var surroundingPlayers = await apiClient.GetSurroundingMERPlayersAsync(playerName, mer);
            return (surroundingPlayers?.LowerPlayer, surroundingPlayers?.UpperPlayer);
        }
        catch (Exception ex)
        {
            var logger = ServiceLocator.GetService<ILogger>();
            logger?.LogError(ex, $"Error in GetSurroundingMERPlayers for player {playerName} with MER {mer}");
            throw;
        }
    }

    public static async Task<(MajPlayerRankingDto, MajPlayerRankingDto)> GetSurroundingJERPlayers(string playerName, decimal jer)
    {
        try
        {
            // API client should be passed as a parameter or injected
            var apiClient = ServiceLocator.GetService<IApiService>();
            if (apiClient == null)
            {
                throw new InvalidOperationException("ApiService is not available");
            }

            var surroundingPlayers = await apiClient.GetSurroundingJERPlayersAsync(playerName, jer);
            return (surroundingPlayers?.LowerPlayer, surroundingPlayers?.UpperPlayer);
        }
        catch (Exception ex)
        {
            var logger = ServiceLocator.GetService<ILogger>();
            logger?.LogError(ex, $"Error in GetSurroundingJERPlayers for player {playerName} with JER {jer}");
            throw;
        }
    }

    public static async Task<List<MajPlayerRankingDto>> GetMajPlayerRankings(int limitTo = 30)
    {
        try
        {
            // API client should be passed as a parameter or injected
            var apiClient = ServiceLocator.GetService<IApiService>();
            if (apiClient == null)
            {
                throw new InvalidOperationException("ApiService is not available");
            }

            var rankings = await apiClient.GetLatestMajPlayerRankingsAsync(limitTo);
            return rankings ?? new List<MajPlayerRankingDto>();
        }
        catch (Exception ex)
        {
            var logger = ServiceLocator.GetService<ILogger>();
            logger?.LogError(ex, $"Error in GetMajPlayerRankings");
            throw;
        }
    }

    public static async Task<(bool, MajPlayerRankingDto)> SaveMajPlayerRanking(MajPlayerRankingDto majPlayerRanking, ILogger? logger)
    {
        try
        {
            // API client should be passed as a parameter or injected
            var apiClient = ServiceLocator.GetService<IApiService>();
            if (apiClient == null)
            {
                throw new InvalidOperationException("ApiService is not available");
            }

            var response = await apiClient.SaveMajPlayerRankingAsync(majPlayerRanking);

            if (response == null)
            {
                logger?.LogWarning("SaveMajPlayerRankingAsync returned null");
                return (false, new MajPlayerRankingDto());
            }

            if (response.Success)
            {
                logger?.LogInformation(response.Message);
                return (true, response.PreviousRanking);
            }
            else
            {
                //logger?.LogInformation(response.Message);
                return (false, null);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, $"Error in SaveMajPlayerRanking for {majPlayerRanking.IGN}");
            throw;
        }
    }
}

// A simple service locator for accessing common services
// Note: This is a quick solution for the refactoring, but dependency injection would be better
public static class ServiceLocator
{
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, object> _services =
        new System.Collections.Concurrent.ConcurrentDictionary<Type, object>();
    private static readonly object _lock = new object();

    public static void RegisterService<T>(T service) where T : class
    {
        // Use thread-safe AddOrUpdate method
        _services.AddOrUpdate(typeof(T), service, (_, _) => service);
    }

    public static T GetService<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var service))
        {
            return (T)service;
        }
        return null;
    }
}
