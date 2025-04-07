namespace HemSoft.EggIncTracker.Data.Dtos;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

public class ContractDto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required] [MaxLength(50)] public string Name { get; set; } = null!;

    [Required] [MaxLength(50)] public string KevId { get; set; } = null!;

    [Required] public int PlayerCount { get; set; }

    [Required] public DateTime PublishDate { get; set; }

    [Required] public string Description { get; set; } = null!;

    [MaxLength(50)] public string? SeasonId { get; set; }

    [Required] public float MinutesPerToken { get; set; }

    [Required] public DateTime StartTime { get; set; }

    [Required] public DateTime EndTime { get; set; }

    public int? LengthSeconds { get; set; }

    public bool HasProphecyEggReward { get; set; }
    public bool HasArtifactCaseReward { get; set; }


    public static List<ContractDto>ApiToContracts(string playerId, string playerName, string apiResponse)
    {
        List<ContractDto> contracts = new();
        ContractDto? contractDto = null;

        var root = JsonConvert.DeserializeObject<JsonContractsRoot>(apiResponse);
        foreach (var contract in root.Contracts.ContractsList)
        {
            contractDto = new ContractDto
            {
                Name = contract.Name,
                KevId = contract.Identifier,
                PlayerCount = contract.MaxCoopSize,
                PublishDate = Utils.ConvertUnixTimestampToCST(contract.StartTime),
                Description = contract.Description,
                SeasonId = contract.SeasonId,
                MinutesPerToken = contract.MinutesPerToken,
                StartTime = Utils.ConvertUnixTimestampToCST(contract.StartTime),
                EndTime = Utils.ConvertUnixTimestampToCST(contract.ExpirationTime),
                LengthSeconds = contract.LengthSeconds
            };

            // Check Rewards:
            var grade = contract.GradeSpecsList.LastOrDefault();
            if (grade != null)
            {
                foreach (var goal in grade.GoalsList)
                {
                    if ((RewardType)goal.RewardType == RewardType.EggOfProphecy)
                    {
                        contractDto.HasProphecyEggReward = true;
                    }

                    if ((RewardType)goal.RewardType == RewardType.ArtifactCase)
                    {
                        contractDto.HasArtifactCaseReward = true;
                    }
                }
            }

            contracts.Add(contractDto);
        }

        return contracts;
    }

    public class JsonContractsRoot
    {
        [JsonProperty("contracts")] public JsonContracts Contracts { get; set; } = null!;
    }

    public class JsonContracts
    {
        [JsonProperty("contractsList")] public List<JsonContract> ContractsList { get; set; } = new();
    }

    public class JsonContract
    {
        [JsonProperty("identifier")] public string Identifier { get; set; } = null!;

        [JsonProperty("name")] public string Name { get; set; } = null!;

        [JsonProperty("description")] public string Description { get; set; } = null!;

        [JsonProperty("egg")] public int Egg { get; set; }

        [JsonProperty("customEggId")] public string? CustomEggId { get; set; }

        [JsonProperty("goalsList")] public List<JsonGoal> GoalsList { get; set; } = new();

        [JsonProperty("goalSetsList")] public List<JsonGoalSet> GoalSetsList { get; set; } = new();

        [JsonProperty("gradeSpecsList")] public List<JsonGradeSpec> GradeSpecsList { get; set; } = new();

        [JsonProperty("seasonId")] public string SeasonId { get; set; } = null!;

        [JsonProperty("coopAllowed")] public bool CoopAllowed { get; set; }

        [JsonProperty("maxCoopSize")] public int MaxCoopSize { get; set; }

        [JsonProperty("maxBoosts")] public int MaxBoosts { get; set; }

        [JsonProperty("minutesPerToken")] public float MinutesPerToken { get; set; }

        [JsonProperty("chickenRunCooldownMinutes")]
        public int ChickenRunCooldownMinutes { get; set; }

        [JsonProperty("startTime")] public long StartTime { get; set; }

        [JsonProperty("expirationTime")] public long ExpirationTime { get; set; }

        [JsonProperty("lengthSeconds")] public int LengthSeconds { get; set; }

        [JsonProperty("maxSoulEggs")] public int MaxSoulEggs { get; set; }

        [JsonProperty("minClientVersion")] public int MinClientVersion { get; set; }

        [JsonProperty("leggacy")] public bool Leggacy { get; set; }

        [JsonProperty("ccOnly")] public bool CcOnly { get; set; }

        [JsonProperty("defaultShellIdsList")] public List<string> DefaultShellIdsList { get; set; } = new();

        [JsonProperty("debug")] public bool Debug { get; set; }
    }

    public class JsonGoal
    {
        [JsonProperty("type")] public int Type { get; set; }

        [JsonProperty("targetAmount")] public BigInteger TargetAmount { get; set; }

        [JsonProperty("rewardType")] public int RewardType { get; set; }

        [JsonProperty("rewardSubType")] public string RewardSubType { get; set; } = null!;

        [JsonProperty("rewardAmount")] public int RewardAmount { get; set; }

        [JsonProperty("targetSoulEggs")] public long TargetSoulEggs { get; set; }
    }

    public class JsonGoalSet
    {
        [JsonProperty("goalsList")] public List<JsonGoal> GoalsList { get; set; } = new();
    }

    public class JsonGradeSpec
    {
        [JsonProperty("grade")] public int Grade { get; set; }

        [JsonProperty("goalsList")] public List<JsonGoal> GoalsList { get; set; } = new();

        [JsonProperty("modifiersList")] public List<JsonModifier> ModifiersList { get; set; } = new();

        [JsonProperty("lengthSeconds")] public int LengthSeconds { get; set; }
    }

    public class JsonModifier
    {
        [JsonProperty("dimension")] public int Dimension { get; set; }

        [JsonProperty("value")] public double Value { get; set; }
    }
}