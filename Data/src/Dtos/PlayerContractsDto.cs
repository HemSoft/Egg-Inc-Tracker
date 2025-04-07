namespace HemSoft.EggIncTracker.Data.Dtos;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class PlayerContractDto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string EID { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string KevId { get; set; } = null!;

    [Required]
    public int ContractScore { get; set; }

    public int? ContractScoreChange { get; set; }

    [Required]
    public string CoopId { get; set; }

    [Required]
    public DateTime TimeAccepted { get; set; }

    [Required]
    public bool Accepted { get; set; }

    [Required]
    public int CoopContributionFinalized { get; set; }

    [Required]
    public int NumberOfGoalsAchieved { get; set; }

    [Required]
    public int BoostsUsed { get; set; }

    [Required]
    public bool PointsReplay { get; set; }

    [Required]
    public byte Grade { get; set; }

    [Required]
    public double ContributionRatio { get; set; }

    [Required]
    public DateTime CompletionTime { get; set; }

    [Required]
    public byte ChickenRunsSent { get; set; }

    [Required]
    public byte TokensSent { get; set; }

    [Required]
    public byte TokensReceived { get; set; }

    [Required]
    public double TokenValueSent { get; set; }

    [Required]
    public int TokenValueReceived { get; set; }

    [Required]
    public int BoostTokenAllotment { get; set; }

    [Required]
    public double TeamworkScore { get; set; }

    public double? BuffTimeValue { get; set; }

    public bool? CountedInSeason { get; set; }

    [MaxLength(50)]
    public string? SeasonId { get; set; }

    public DateTime? EvaluationStartTime { get; set; }

    public byte? CoopStatus { get; set; }


    public static List<PlayerContractDto> ApiToPlayerContracts(string playerId, string playerName, string apiResponse)
    {
        List<PlayerContractDto> contracts = new();
        PlayerContractDto? playerContract = null;

        var root = JsonConvert.DeserializeObject<JsonArchiveRoot>(apiResponse);
        foreach (var ev in root.ArchiveList)
        {
            if (!string.IsNullOrEmpty(ev.CoopIdentifier))
            {
                playerContract = new PlayerContractDto
                {
                    EID = playerId,
                    KevId = ev.Contract.Identifier,
                    ContractScore = ev.Evaluation.Cxp,
                    ContractScoreChange = ev.Evaluation.CxpChange,
                    CoopId = ev.CoopIdentifier,
                    TimeAccepted = ConvertUnixTimestampToCST((long)ev.TimeAccepted),
                    Accepted = ev.Accepted,
                    CoopContributionFinalized = ev.CoopContributionFinalized ? 1 : 0,
                    NumberOfGoalsAchieved = ev.NumGoalsAchieved,
                    BoostsUsed = ev.BoostsUsed,
                    PointsReplay = ev.PointsReplay,
                    Grade = (byte)ev.Grade,
                    ContributionRatio = ev.Evaluation.ContributionRatio,
                    CompletionTime = ConvertUnixTimestampToCST((long)ev.TimeAccepted + (long)ev.Evaluation.CompletionTime),
                    ChickenRunsSent = (byte)ev.Evaluation.ChickenRunsSent,
                    TokensSent = (byte)ev.Evaluation.GiftTokensSent,
                    TokensReceived = (byte)ev.Evaluation.GiftTokensReceived,
                    TokenValueSent = ev.Evaluation.GiftTokenValueSent,
                    TokenValueReceived = (int)ev.Evaluation.GiftTokenValueReceived,
                    BoostTokenAllotment = ev.Evaluation.BoostTokenAllotment,
                    TeamworkScore = ev.Evaluation.TeamworkScore,
                    BuffTimeValue = ev.Evaluation.BuffTimeValue,
                    CountedInSeason = ev.Evaluation.CountedInSeason,
                    SeasonId = ev.Evaluation.SeasonId,
                    EvaluationStartTime = ConvertUnixTimestampToCST((long)ev.Evaluation.EvaluationStartTime),
                    CoopStatus = (byte)ev.Evaluation.Status
                };
                contracts.Add(playerContract);
            }
        }

        return contracts;
    }

    public static DateTime ConvertUnixTimestampToCST(long unixTimestamp)
    {
        // Unix timestamp is seconds past epoch (UTC)
        DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime utcDateTime = epoch.AddSeconds(unixTimestamp);

        // Convert to CST (Central Standard Time)
        TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById("Central Standard Time");
        DateTime cstDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, cstZone);

        return cstDateTime;
    }
}

public class JsonArchiveRoot
{
    [JsonProperty("archiveList")]
    public List<JsonArchiveEntry> ArchiveList { get; set; } = new();
}

public class JsonArchiveEntry
{
    [JsonProperty("contract")]
    public JsonArchiveContract Contract { get; set; } = null!;

    [JsonProperty("coopIdentifier")]
    public string CoopIdentifier { get; set; } = null!;

    [JsonProperty("accepted")]
    public bool Accepted { get; set; }

    [JsonProperty("timeAccepted")]
    public double TimeAccepted { get; set; }

    [JsonProperty("cancelled")]
    public bool Cancelled { get; set; }

    [JsonProperty("coopSharedEndTime")]
    public double CoopSharedEndTime { get; set; }

    [JsonProperty("coopSimulationEndTime")]
    public double CoopSimulationEndTime { get; set; }

    [JsonProperty("coopGracePeriodEndTime")]
    public double CoopGracePeriodEndTime { get; set; }

    [JsonProperty("coopContributionFinalized")]
    public bool CoopContributionFinalized { get; set; }

    [JsonProperty("coopLastUploadedContribution")]
    public double CoopLastUploadedContribution { get; set; }

    [JsonProperty("coopUserId")]
    public string CoopUserId { get; set; } = null!;

    [JsonProperty("coopShareFarm")]
    public bool CoopShareFarm { get; set; }

    [JsonProperty("lastAmountWhenRewardGiven")]
    public double LastAmountWhenRewardGiven { get; set; }

    [JsonProperty("numGoalsAchieved")]
    public int NumGoalsAchieved { get; set; }

    //[JsonProperty("maxFarmSizeReached")]
    //public int MaxFarmSizeReached { get; set; }

    [JsonProperty("boostsUsed")]
    public int BoostsUsed { get; set; }

    [JsonProperty("pointsReplay")]
    public bool PointsReplay { get; set; }

    [JsonProperty("grade")]
    public int Grade { get; set; }

    [JsonProperty("evaluation")]
    public JsonArchiveEvaluation Evaluation { get; set; } = null!;

    [JsonProperty("reportedUuidsList")]
    public List<string> ReportedUuidsList { get; set; } = new();
}

public class JsonArchiveContract
{
    [JsonProperty("identifier")]
    public string Identifier { get; set; } = null!;

    [JsonProperty("name")]
    public string Name { get; set; } = null!;

    [JsonProperty("description")]
    public string Description { get; set; } = null!;

    [JsonProperty("egg")]
    public int Egg { get; set; }

    [JsonProperty("customEggId")]
    public string? CustomEggId { get; set; }

    //[JsonProperty("goalsList")]
    //public List<JsonArchiveGoal> GoalsList { get; set; } = new();

    //[JsonProperty("goalSetsList")]
    //public List<JsonArchiveGoalSet> GoalSetsList { get; set; } = new();

    [JsonProperty("gradeSpecsList")]
    public List<JsonArchiveGradeSpec> GradeSpecsList { get; set; } = new();

    [JsonProperty("seasonId")]
    public string SeasonId { get; set; } = null!;

    [JsonProperty("coopAllowed")]
    public bool CoopAllowed { get; set; }

    [JsonProperty("maxCoopSize")]
    public int MaxCoopSize { get; set; }

    [JsonProperty("maxBoosts")]
    public int MaxBoosts { get; set; }

    [JsonProperty("minutesPerToken")]
    public float MinutesPerToken { get; set; }

    [JsonProperty("chickenRunCooldownMinutes")]
    public int ChickenRunCooldownMinutes { get; set; }

    [JsonProperty("startTime")]
    public long StartTime { get; set; }

    [JsonProperty("expirationTime")]
    public double ExpirationTime { get; set; }

    [JsonProperty("lengthSeconds")]
    public int LengthSeconds { get; set; }

    [JsonProperty("maxSoulEggs")]
    public int MaxSoulEggs { get; set; }

    [JsonProperty("minClientVersion")]
    public int MinClientVersion { get; set; }

    [JsonProperty("leggacy")]
    public bool Leggacy { get; set; }

    [JsonProperty("ccOnly")]
    public bool CcOnly { get; set; }

    [JsonProperty("defaultShellIdsList")]
    public List<string> DefaultShellIdsList { get; set; } = new();

    [JsonProperty("debug")]
    public bool Debug { get; set; }
}

//public class JsonArchiveGoal
//{
//    [JsonProperty("type")]
//    public int Type { get; set; }

//    //[JsonProperty("targetAmount")]
//    //public long TargetAmount { get; set; }

//    [JsonProperty("rewardType")]
//    public int RewardType { get; set; }

//    [JsonProperty("rewardSubType")]
//    public string RewardSubType { get; set; } = null!;

//    [JsonProperty("rewardAmount")]
//    public int RewardAmount { get; set; }

//    [JsonProperty("targetSoulEggs")]
//    public long TargetSoulEggs { get; set; }
//}

//public class JsonArchiveGoalSet
//{
//    [JsonProperty("goalsList")]
//    public List<JsonArchiveGoal> GoalsList { get; set; } = new();
//}

public class JsonArchiveGradeSpec
{
    [JsonProperty("grade")]
    public int Grade { get; set; }

    //[JsonProperty("goalsList")]
    //public List<JsonArchiveGoal> GoalsList { get; set; } = new();

    [JsonProperty("modifiersList")]
    public List<JsonArchiveModifier> ModifiersList { get; set; } = new();

    [JsonProperty("lengthSeconds")]
    public int LengthSeconds { get; set; }
}

public class JsonArchiveModifier
{
    [JsonProperty("dimension")]
    public int Dimension { get; set; }

    [JsonProperty("value")]
    public double Value { get; set; }
}

public class JsonArchiveEvaluation
{
    [JsonProperty("contractIdentifier")]
    public string ContractIdentifier { get; set; } = null!;

    [JsonProperty("coopIdentifier")]
    public string CoopIdentifier { get; set; } = null!;

    [JsonProperty("cxp")]
    public int Cxp { get; set; }

    [JsonProperty("cxpChange")]
    public int CxpChange { get; set; }

    [JsonProperty("oldLeague")]
    public int OldLeague { get; set; }

    [JsonProperty("grade")]
    public int Grade { get; set; }

    [JsonProperty("contributionRatio")]
    public double ContributionRatio { get; set; }

    [JsonProperty("completionPercent")]
    public double CompletionPercent { get; set; }

    [JsonProperty("originalLength")]
    public int OriginalLength { get; set; }

    [JsonProperty("coopSize")]
    public int CoopSize { get; set; }

    [JsonProperty("solo")]
    public bool Solo { get; set; }

    [JsonProperty("soulPower")]
    public double SoulPower { get; set; }

    [JsonProperty("lastContributionTime")]
    public double LastContributionTime { get; set; }

    [JsonProperty("completionTime")]
    public double CompletionTime { get; set; }

    [JsonProperty("chickenRunsSent")]
    public int ChickenRunsSent { get; set; }

    [JsonProperty("giftTokensSent")]
    public int GiftTokensSent { get; set; }

    [JsonProperty("giftTokensReceived")]
    public int GiftTokensReceived { get; set; }

    [JsonProperty("giftTokenValueSent")]
    public double GiftTokenValueSent { get; set; }

    [JsonProperty("giftTokenValueReceived")]
    public double GiftTokenValueReceived { get; set; }

    [JsonProperty("boostTokenAllotment")]
    public int BoostTokenAllotment { get; set; }

    [JsonProperty("buffTimeValue")]
    public double BuffTimeValue { get; set; }

    [JsonProperty("teamworkScore")]
    public double TeamworkScore { get; set; }

    [JsonProperty("countedInSeason")]
    public bool CountedInSeason { get; set; }

    [JsonProperty("seasonId")]
    public string SeasonId { get; set; } = null!;

    [JsonProperty("timeCheats")]
    public int TimeCheats { get; set; }

    [JsonProperty("issuesList")]
    public List<string> IssuesList { get; set; } = new();

    [JsonProperty("notesList")]
    public List<string> NotesList { get; set; } = new();

    [JsonProperty("version")]
    public string Version { get; set; } = null!;

    [JsonProperty("evaluationStartTime")]
    public double EvaluationStartTime { get; set; }

    [JsonProperty("status")]
    public int Status { get; set; }
}
