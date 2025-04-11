using HemSoft.EggIncTracker.Data.Dtos;
using HemSoft.EggIncTracker.Domain;

namespace HemSoft.EggIncTracker.Dashboard.BlazorClient.Services
{
    public interface IApiService
    {
        Task<PlayerDto?> GetLatestPlayerAsync(string playerName);
        Task<PlayerDto?> GetPlayerByEIDAsync(string eid);
        Task<PlayerStatsDto?> GetPlayerStatsAsync(string playerName, int recordLimit = 1, int sampleDaysBack = 30);
        Task<List<PlayerDto>?> GetPlayerHistoryAsync(string playerName, DateTime? from = null, DateTime? to = null, int? limit = null);
        Task<PlayerStatsDto?> GetRankedPlayerAsync(string playerName, int recordLimit = 1, int sampleDaysBack = 30);
        Task<TitleInfoDto?> GetTitleInfoAsync(string playerName);
        Task<PlayerGoalDto?> GetPlayerGoalsAsync(string playerName);

        // MajPlayerRankings endpoints
        Task<SurroundingPlayersDto?> GetSurroundingSEPlayersAsync(string playerName, string soulEggs);
        Task<SurroundingPlayersDto?> GetSurroundingEBPlayersAsync(string playerName, string earningsBonus);
        Task<SurroundingPlayersDto?> GetSurroundingMERPlayersAsync(string playerName, decimal mer);
        Task<SurroundingPlayersDto?> GetSurroundingJERPlayersAsync(string playerName, decimal jer);
        Task<List<MajPlayerRankingDto>?> GetLatestMajPlayerRankingsAsync(int limit = 30);
        Task<SaveRankingResponseDto?> SaveMajPlayerRankingAsync(MajPlayerRankingDto majPlayerRanking);

        // Events endpoints
        Task<List<CurrentEventDto>?> GetActiveEventsAsync();
        Task<List<EventDto>?> GetEventsAsync(bool activeOnly = false, string? eventType = null);
    }
}
