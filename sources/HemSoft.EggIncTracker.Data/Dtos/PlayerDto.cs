using Exception = System.Exception;

namespace HemSoft.EggIncTracker.Data.Dtos;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

using HemSoft.EggIncTracker.Data;

public class PlayerDto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public required string EID { get; set; }
    public required string PlayerName { get; set; }
    public DateTime Updated { get; set; }
    public int TotalCraftsThatCanBeLegendary { get; set; }
    public float ExpectedLegendaryCrafts { get; set; }
    public float ExpectedLegendaryDropsFromShips { get; set; }
    public float ExpectedLegendaries { get; set; }
    public float PlayerLegendaries { get; set; }
    public float PlayerLegendariesExcludingLunarTotem { get; set; }
    public float LLC { get; set; }
    public int ProphecyEggs { get; set; }
    public required string SoulEggs { get; set; }
    public required string SoulEggsFull { get; set; }
    public required string EarningsBonusPercentage { get; set; }
    public required string EarningsBonusPerHour { get; set; }
    public required string Title { get; set; }
    public required string NextTitle { get; set; }
    public double TitleProgress { get; set; }
    public DateTime ProjectedTitleChange { get; set; }
    public float MER { get; set; }
    public float JER { get; set; }
    public int CraftingLevel { get; set; }
    public int PiggyConsumeValue { get; set; }
    public float ShipLaunchPoints { get; set; }
    public float HoarderScore { get; set; }
    //public float InventoryScore { get; set; }
    public int? Prestiges { get; set; }
    public bool? PiggyFull { get; set; }
    public int? PiggyBreaks { get; set; }
    [NotMapped] 
    public int? PrestigesToday { get; set; }
    [NotMapped]
    public int? PrestigesThisWeek { get; set; }

    public static (PlayerDto, JsonPlayerRoot) ApiToPlayer(string playerId, string playerName, string apiResponse, string apiResponse2)
    {
        var se = Regex.Replace(GetString(apiResponse, @"Soul eggs: ([\d,]+)"), @",", "");
        var player = new PlayerDto
        {
            EID = playerId,
            PlayerName = playerName,
            Updated = DateTime.UtcNow,
            TotalCraftsThatCanBeLegendary = GetNumber(apiResponse, @"legendary:\s+(\d+)"),
            ExpectedLegendaryCrafts = GetFloat(apiResponse, @"Expected legendary crafts:\s+(\d+\.\d+)"),
            ExpectedLegendaryDropsFromShips =
                GetFloat(apiResponse, @"Expected legendary drops from ships:\s+(\d+\.\d+)"),
            ExpectedLegendaries = GetFloat(apiResponse, @"Expected legendaries:\s+(\d+\.\d+)"),
            PlayerLegendaries = GetFloat(apiResponse, @"Your legendaries:\s+(\d+\.\d+)"),
            PlayerLegendariesExcludingLunarTotem =
                GetFloat(apiResponse, @"Your legendaries excluding Lunar Totem:\s+(\d+\.\d+)"),
            LLC = GetFloat(apiResponse, @"Your LLC:\s+(-?\d+\.\d+)"),
            ProphecyEggs = GetNumber(apiResponse, @"Prophecy eggs:\s+(\d+)"),
            SoulEggs = Utils.FormatBigInteger(se),
            SoulEggsFull = se,
            EarningsBonusPercentage = string.Empty,
            EarningsBonusPerHour = string.Empty,
            Title = string.Empty,
            NextTitle = string.Empty,
            JER = GetFloat(apiResponse, @"Your JER is:\s+(\d+\.\d+)"),
            MER = GetFloat(apiResponse, @"Your MER is:\s+(\d+\.\d+)"),
            CraftingLevel = GetNumber(apiResponse, @"Crafting level:\s+(\d+)"),
            ShipLaunchPoints = GetFloat(apiResponse, @"Henliner launch points:\s+(\d+\.\d+)"),
            HoarderScore = GetFloat(apiResponse, @"Hoarder score:\s+(\d+\.\d+)")
        };

        try
        {
            var root = JsonConvert.DeserializeObject<JsonPlayerRoot>(apiResponse2);
            if (root == null)
            {
                return (player, null);
            }

            var t = ConvertUnixTimestampToCST(root.ApproxTime);

            player.ProphecyEggs = root.Game.EggsOfProphecy;
            player.SoulEggsFull = ConvertScientificToFullNumber(root.Game.SoulEggsD);
            player.Prestiges = root.Stats.NumPrestiges;
            player.PiggyFull = root.Stats.PiggyFull;
            player.PiggyBreaks = root.Stats.NumPiggyBreaks;

            return (player, root);
        }
        catch (Exception ex)
        {
            Console.WriteLine("\nException Caught!");
            Console.WriteLine("Message :{0} ", ex.Message);
            Console.Write(ex.StackTrace);
            return (null, null);
        }
    }

    public static string ConvertScientificToFullNumber(double scientificNotation)
    {
        return scientificNotation.ToString("F0");
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

    private static int GetNumber(string apiResponse, string pattern)
    {
        var match = Regex.Match(apiResponse, pattern);
        if (match.Success)
        {
            return int.Parse(match.Groups[1].Value);
        }
        return 0;
    }

    private static BigInteger GetBigNumber(string apiResponse, string pattern)
    {
        var match = Regex.Match(apiResponse, pattern);
        string pureString = string.Empty;
        if (match.Success)
        {
            pureString = match.Groups[1].Value;
            var pureNumber = Regex.Replace(pureString, @",", "");
            if (!string.IsNullOrEmpty(pureNumber))
            {
                return BigInteger.Parse(pureNumber);
            }
        }

        return 0;
    }

    private static float GetFloat(string apiResponse, string pattern)
    {
        var match = Regex.Match(apiResponse, pattern);
        if (match.Success)
        {
            return float.Parse(match.Groups[1].Value);
        }
        return 0;
    }

    private static string GetString(string apiResponse, string pattern)
    {
        var match = Regex.Match(apiResponse, pattern);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        return string.Empty;
    }

    public static bool CompareBigNumber(BigInteger bigNumber, string limit)
    {
        var lim = BigInteger.Parse(limit);
        return bigNumber <= lim;
    }

    public override string ToString()
    {
        return
            $"Id ...........: {Id}\n" +
            $"EID ..........: {EID}\n" +
            $"Name .........: {PlayerName}\n" +
            $"Prophecy Eggs : {ProphecyEggs}\n" +
            $"Soul Eggs ....: {SoulEggs}\n" +
            $"Earnings Bonus: {EarningsBonusPercentage}\n" +
            $"Title ........: {Title}\n" +
            $"LLC ..........: {LLC}\n" +
            $"MER ..........: {MER}\n" +
            $"JER ..........: {JER}\n" +
            $"Crafting Level: {CraftingLevel}\n";
    }
}

public class JsonPlayerRoot
{
    [JsonPropertyName("eiUserId")]
    public string EiUserId { get; set; }

    [JsonPropertyName("gameServicesId")]
    public string GameServicesId { get; set; }

    [JsonPropertyName("pushUserId")]
    public string PushUserId { get; set; }

    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; }

    [JsonPropertyName("userName")]
    public string UserName { get; set; }

    [JsonPropertyName("approxTime")]
    public long ApproxTime { get; set; }

    [JsonPropertyName("version")]
    public int Version { get; set; }

    [JsonPropertyName("forceOfferBackup")]
    public bool ForceOfferBackup { get; set; }

    [JsonPropertyName("forceBackup")]
    public bool ForceBackup { get; set; }

    [JsonPropertyName("settings")]
    public JsonPlayerSettings Settings { get; set; }

    [JsonPropertyName("tutorial")]
    public JsonPlayerTutorial Tutorial { get; set; }

    [JsonPropertyName("stats")]
    public JsonPlayerStats Stats { get; set; }

    [JsonPropertyName("game")]
    public JsonPlayerGame Game { get; set; }

    [JsonPropertyName("artifacts")]
    public JsonPlayerArtifacts Artifacts { get; set; }

    [JsonPropertyName("contracts")]
    public JsonPlayerContracts Contracts { get; set; }

    [JsonPropertyName("artifactsDb")]
    public JsonPlayerArtifactsDb ArtifactsDb { get; set; }

    [JsonPropertyName("shellDb")]
    public JsonPlayerShellDb ShellDb { get; set; }

    [JsonPropertyName("readMailIdsList")]
    public List<string> ReadMailIdsList { get; set; }

    [JsonPropertyName("mailState")]
    public JsonPlayerMailState MailState { get; set; }

    [JsonPropertyName("checksum")]
    public long Checksum { get; set; }
}

public class JsonPlayerSettings
{
    [JsonPropertyName("sfx")]
    public bool Sfx { get; set; }

    [JsonPropertyName("music")]
    public bool Music { get; set; }

    [JsonPropertyName("lowBatteryMode")]
    public bool LowBatteryMode { get; set; }

    [JsonPropertyName("lowPerformanceMode")]
    public bool LowPerformanceMode { get; set; }

    [JsonPropertyName("forceTouchChickenBtn")]
    public bool ForceTouchChickenBtn { get; set; }

    [JsonPropertyName("lastNotificationQueryTime")]
    public double LastNotificationQueryTime { get; set; }

    [JsonPropertyName("notificationsOn")]
    public bool NotificationsOn { get; set; }

    [JsonPropertyName("notifyDailyGift")]
    public bool NotifyDailyGift { get; set; }

    [JsonPropertyName("lowPerformance")]
    public bool LowPerformance { get; set; }

    [JsonPropertyName("autoStopFueling")]
    public bool AutoStopFueling { get; set; }

    [JsonPropertyName("maxEnabled")]
    public bool MaxEnabled { get; set; }

    [JsonPropertyName("hideCcStatus")]
    public bool HideCcStatus { get; set; }

    [JsonPropertyName("contractsWidgetEnabled")]
    public bool ContractsWidgetEnabled { get; set; }

    [JsonPropertyName("artifactSparkle")]
    public bool ArtifactSparkle { get; set; }

    [JsonPropertyName("lastBackupTime")]
    public double LastBackupTime { get; set; }

    [JsonPropertyName("ageRestricted")]
    public bool AgeRestricted { get; set; }

    [JsonPropertyName("dataCollectionConsentQueried")]
    public bool DataCollectionConsentQueried { get; set; }

    [JsonPropertyName("dataCollectionConsentGiven")]
    public bool DataCollectionConsentGiven { get; set; }

    [JsonPropertyName("userAdsEnabled")]
    public bool UserAdsEnabled { get; set; }

    [JsonPropertyName("userCloudEnabled")]
    public bool UserCloudEnabled { get; set; }

    [JsonPropertyName("userAnalyticsEnabled")]
    public bool UserAnalyticsEnabled { get; set; }

    [JsonPropertyName("userPersonalizedAdsEnabled")]
    public bool UserPersonalizedAdsEnabled { get; set; }
}

public class JsonPlayerTutorial
{
    [JsonPropertyName("introShown")]
    public bool IntroShown { get; set; }

    [JsonPropertyName("clickTutorialShown")]
    public bool ClickTutorialShown { get; set; }

    [JsonPropertyName("qNumShown")]
    public bool QNumShown { get; set; }

    [JsonPropertyName("sNumShown")]
    public bool SNumShown { get; set; }

    [JsonPropertyName("tutorialShownList")]
    public List<bool> TutorialShownList { get; set; }
}

public class JsonPlayerStats
{
    [JsonPropertyName("eggTotalsOldList")]
    public List<double> EggTotalsOldList { get; set; }

    [JsonPropertyName("eggTotalsList")]
    public List<double> EggTotalsList { get; set; }

    [JsonPropertyName("boostsUsed")]
    public int BoostsUsed { get; set; }

    [JsonPropertyName("videoDoublerUses")]
    public int VideoDoublerUses { get; set; }

    [JsonPropertyName("droneTakedowns")]
    public int DroneTakedowns { get; set; }

    [JsonPropertyName("droneTakedownsElite")]
    public int DroneTakedownsElite { get; set; }

    [JsonPropertyName("numPrestiges")]
    public int NumPrestiges { get; set; }

    [JsonPropertyName("numPiggyBreaks")]
    public int NumPiggyBreaks { get; set; }

    [JsonPropertyName("iapPacksPurchased")]
    public int IapPacksPurchased { get; set; }

    [JsonPropertyName("piggyFull")]
    public bool PiggyFull { get; set; }

    [JsonPropertyName("piggyFoundFull")]
    public bool PiggyFoundFull { get; set; }

    [JsonPropertyName("timePiggyFilledRealtime")]
    public double TimePiggyFilledRealtime { get; set; }

    [JsonPropertyName("timePiggyFullGametime")]
    public double TimePiggyFullGametime { get; set; }

    [JsonPropertyName("lostPiggyIncrements")]
    public int LostPiggyIncrements { get; set; }
}

public class JsonPlayerGame
{
    [JsonPropertyName("currentFarm")]
    public int CurrentFarm { get; set; }

    [JsonPropertyName("maxEggReached")]
    public int MaxEggReached { get; set; }

    [JsonPropertyName("goldenEggsEarned")]
    public long GoldenEggsEarned { get; set; }

    [JsonPropertyName("goldenEggsSpent")]
    public long GoldenEggsSpent { get; set; }

    [JsonPropertyName("uncliamedGoldenEggs")]
    public int UnclaimedGoldenEggs { get; set; }

    [JsonPropertyName("soulEggsD")]
    public double SoulEggsD { get; set; }

    [JsonPropertyName("unclaimedSoulEggsD")]
    public int UnclaimedSoulEggsD { get; set; }

    [JsonPropertyName("eggsOfProphecy")]
    public int EggsOfProphecy { get; set; }

    [JsonPropertyName("unclaimedEggsOfProphecy")]
    public int UnclaimedEggsOfProphecy { get; set; }

    [JsonPropertyName("shellScriptsEarned")]
    public int ShellScriptsEarned { get; set; }

    [JsonPropertyName("shellScriptsSpent")]
    public int ShellScriptsSpent { get; set; }

    [JsonPropertyName("unclaimedShellScripts")]
    public int UnclaimedShellScripts { get; set; }

    [JsonPropertyName("prestigeCashEarned")]
    public double PrestigeCashEarned { get; set; }

    [JsonPropertyName("prestigeSoulBoostCash")]
    public double PrestigeSoulBoostCash { get; set; }

    [JsonPropertyName("lifetimeCashEarned")]
    public double LifetimeCashEarned { get; set; }

    [JsonPropertyName("piggyBank")]
    public int PiggyBank { get; set; }

    [JsonPropertyName("piggyFullAlertShown")]
    public bool PiggyFullAlertShown { get; set; }

    [JsonPropertyName("permitLevel")]
    public int PermitLevel { get; set; }

    [JsonPropertyName("epicResearchList")]
    public List<JsonPlayerEpicResearch> EpicResearchList { get; set; }

    [JsonPropertyName("hyperloopStation")]
    public bool HyperloopStation { get; set; }

    [JsonPropertyName("nextDailyGiftTime")]
    public double NextDailyGiftTime { get; set; }

    [JsonPropertyName("lastDailyGiftCollectedDay")]
    public int LastDailyGiftCollectedDay { get; set; }

    [JsonPropertyName("numDailyGiftsCollected")]
    public int NumDailyGiftsCollected { get; set; }

    [JsonPropertyName("newsList")]
    public List<JsonPlayerNews> NewsList { get; set; }

    [JsonPropertyName("lastNewsTime")]
    public double LastNewsTime { get; set; }

    [JsonPropertyName("currentMultiplier")]
    public int CurrentMultiplier { get; set; }

    [JsonPropertyName("currentMultiplierExpiration")]
    public double CurrentMultiplierExpiration { get; set; }

    [JsonPropertyName("achievementsList")]
    public List<JsonPlayerAchievement> AchievementsList { get; set; }

    [JsonPropertyName("maxFarmSizeReachedList")]
    public List<long> MaxFarmSizeReachedList { get; set; }

    [JsonPropertyName("eggMedalLevelList")]
    public List<int> EggMedalLevelList { get; set; }

    [JsonPropertyName("boostsList")]
    public List<JsonPlayerBoost> BoostsList { get; set; }
}

public class JsonPlayerEpicResearch
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("level")]
    public int Level { get; set; }
}

public class JsonPlayerNews
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("read")]
    public bool Read { get; set; }
}

public class JsonPlayerAchievement
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("achieved")]
    public bool Achieved { get; set; }
}

public class JsonPlayerBoost
{
    [JsonPropertyName("boostId")]
    public string BoostId { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public class JsonPlayerFarm
{
    [JsonPropertyName("eggType")]
    public int EggType { get; set; }

    [JsonPropertyName("farmType")]
    public int FarmType { get; set; }

    [JsonPropertyName("contractId")]
    public string ContractId { get; set; }

    [JsonPropertyName("cashEarned")]
    public double CashEarned { get; set; }

    [JsonPropertyName("cashSpent")]
    public double CashSpent { get; set; }

    [JsonPropertyName("unclaimedCash")]
    public double UnclaimedCash { get; set; }

    [JsonPropertyName("lastStepTime")]
    public double LastStepTime { get; set; }

    [JsonPropertyName("numChickens")]
    public long NumChickens { get; set; }

    [JsonPropertyName("numChickensUnsettled")]
    public long NumChickensUnsettled { get; set; }

    [JsonPropertyName("numChickensRunning")]
    public long NumChickensRunning { get; set; }

    [JsonPropertyName("eggsLaid")]
    public double EggsLaid { get; set; }

    [JsonPropertyName("eggsShipped")]
    public double EggsShipped { get; set; }

    [JsonPropertyName("eggsPaidFor")]
    public double EggsPaidFor { get; set; }

    [JsonPropertyName("silosOwned")]
    public int SilosOwned { get; set; }

    [JsonPropertyName("habsList")]
    public List<int> HabsList { get; set; }

    [JsonPropertyName("habPopulationList")]
    public List<long> HabPopulationList { get; set; }

    [JsonPropertyName("habPopulationIndoundList")]
    public List<long> HabPopulationInboundList { get; set; }

    [JsonPropertyName("habIncubatorPopuplationList")]
    public List<double> HabIncubatorPopulationList { get; set; }

    [JsonPropertyName("hatcheryPopulation")]
    public int HatcheryPopulation { get; set; }

    [JsonPropertyName("vehiclesList")]
    public List<int> VehiclesList { get; set; }

    [JsonPropertyName("trainLengthList")]
    public List<int> TrainLengthList { get; set; }

    [JsonPropertyName("commonResearchList")]
    public List<JsonPlayerCommonResearch> CommonResearchList { get; set; }

    [JsonPropertyName("activeBoostsList")]
    public List<JsonPlayerActiveBoost> ActiveBoostsList { get; set; }
}

public class JsonPlayerCommonResearch
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("level")]
    public int Level { get; set; }
}

public class JsonPlayerActiveBoost
{
    [JsonPropertyName("boostId")]
    public string BoostId { get; set; }

    [JsonPropertyName("timeRemaining")]
    public double TimeRemaining { get; set; }

    [JsonPropertyName("referenceValue")]
    public int ReferenceValue { get; set; }
}

public class JsonPlayerContracts
{
    [JsonPropertyName("contractIdsSeenList")]
    public List<string> ContractIdsSeenList { get; set; }

    [JsonPropertyName("contractsList")]
    public List<JsonPlayerContract> ContractsList { get; set; }
}

public class JsonPlayerContract
{
    [JsonPropertyName("contract")]
    public JsonPlayerContractDefinition Contract { get; set; }

    [JsonPropertyName("coopIdentifier")]
    public string CoopIdentifier { get; set; }

    [JsonPropertyName("accepted")]
    public bool Accepted { get; set; }

    [JsonPropertyName("timeAccepted")]
    public double TimeAccepted { get; set; }

    [JsonPropertyName("coopSharedEndTime")]
    public double CoopSharedEndTime { get; set; }

    [JsonPropertyName("coopSimulationEndTime")]
    public double CoopSimulationEndTime { get; set; }

    [JsonPropertyName("coopGracePeriodEndTime")]
    public double CoopGracePeriodEndTime { get; set; }

    [JsonPropertyName("coopContributionFinalized")]
    public bool CoopContributionFinalized { get; set; }

    [JsonPropertyName("coopLastUploadedContribution")]
    public double CoopLastUploadedContribution { get; set; }

    [JsonPropertyName("coopUserId")]
    public string CoopUserId { get; set; }

    [JsonPropertyName("coopShareFarm")]
    public bool CoopShareFarm { get; set; }

    [JsonPropertyName("lastAmountWhenRewardGiven")]
    public double LastAmountWhenRewardGiven { get; set; }

    [JsonPropertyName("numGoalsAchieved")]
    public int NumGoalsAchieved { get; set; }

    [JsonPropertyName("maxFarmSizeReached")]
    public long MaxFarmSizeReached { get; set; }

    [JsonPropertyName("boostsUsed")]
    public int BoostsUsed { get; set; }

    [JsonPropertyName("pointsReplay")]
    public bool PointsReplay { get; set; }

    [JsonPropertyName("grade")]
    public int Grade { get; set; }

    [JsonPropertyName("reportedUuidsList")]
    public List<string> ReportedUuidsList { get; set; }
}

public class JsonPlayerContractDefinition
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("egg")]
    public int Egg { get; set; }

    [JsonPropertyName("customEggId")]
    public string CustomEggId { get; set; }

    [JsonPropertyName("goalsList")]
    public List<object> GoalsList { get; set; }

    [JsonPropertyName("goalSetsList")]
    public List<JsonPlayerGoalSet> GoalSetsList { get; set; }

    [JsonPropertyName("gradeSpecsList")]
    public List<JsonPlayerGradeSpec> GradeSpecsList { get; set; }

    [JsonPropertyName("seasonId")]
    public string SeasonId { get; set; }

    [JsonPropertyName("coopAllowed")]
    public bool CoopAllowed { get; set; }

    [JsonPropertyName("maxCoopSize")]
    public int MaxCoopSize { get; set; }

    [JsonPropertyName("maxBoosts")]
    public int MaxBoosts { get; set; }

    [JsonPropertyName("minutesPerToken")]
    public float MinutesPerToken { get; set; }

    [JsonPropertyName("chickenRunCooldownMinutes")]
    public int ChickenRunCooldownMinutes { get; set; }

    [JsonPropertyName("startTime")]
    public long StartTime { get; set; }

    [JsonPropertyName("expirationTime")]
    public double ExpirationTime { get; set; }

    [JsonPropertyName("lengthSeconds")]
    public int LengthSeconds { get; set; }

    [JsonPropertyName("maxSoulEggs")]
    public long MaxSoulEggs { get; set; }

    [JsonPropertyName("minClientVersion")]
    public int MinClientVersion { get; set; }

    [JsonPropertyName("leggacy")]
    public bool Leggacy { get; set; }

    [JsonPropertyName("ccOnly")]
    public bool CcOnly { get; set; }

    [JsonPropertyName("defaultShellIdsList")]
    public List<string> DefaultShellIdsList { get; set; }

    [JsonPropertyName("debug")]
    public bool Debug { get; set; }
}

public class JsonPlayerGoalSet
{
    [JsonPropertyName("goalsList")]
    public List<JsonPlayerGoal> GoalsList { get; set; }
}

public class JsonPlayerGoal
{
    [JsonPropertyName("type")]
    public int Type { get; set; }

    [JsonPropertyName("targetAmount")]
    public double TargetAmount { get; set; }

    [JsonPropertyName("rewardType")]
    public int RewardType { get; set; }

    [JsonPropertyName("rewardSubType")]
    public string RewardSubType { get; set; }

    [JsonPropertyName("rewardAmount")]
    public long RewardAmount { get; set; }

    [JsonPropertyName("targetSoulEggs")]
    public long TargetSoulEggs { get; set; }
}

public class JsonPlayerGradeSpec
{
    [JsonPropertyName("grade")]
    public int Grade { get; set; }

    [JsonPropertyName("goalsList")]
    public List<JsonPlayerGoal> GoalsList { get; set; }

    [JsonPropertyName("modifiersList")]
    public List<JsonPlayerModifier> ModifiersList { get; set; }

    [JsonPropertyName("lengthSeconds")]
    public int LengthSeconds { get; set; }
}

public class JsonPlayerModifier
{
    [JsonPropertyName("dimension")]
    public int Dimension { get; set; }

    [JsonPropertyName("value")]
    public double Value { get; set; }
}

public class JsonPlayerMission
{
    [JsonPropertyName("currentMissionsList")]
    public List<object> CurrentMissionsList { get; set; }

    [JsonPropertyName("missionsList")]
    public List<JsonPlayerMissionInfo> MissionsList { get; set; }
}

public class JsonPlayerMissionInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("completed")]
    public bool Completed { get; set; }

    [JsonPropertyName("referenceValue")]
    public long ReferenceValue { get; set; }
}

public class JsonPlayerArtifacts
{
    [JsonPropertyName("flowPercentageArtifacts")]
    public double FlowPercentageArtifacts { get; set; }

    [JsonPropertyName("fuelingEnabled")]
    public bool FuelingEnabled { get; set; }

    [JsonPropertyName("tankFillingEnabled")]
    public bool TankFillingEnabled { get; set; }

    [JsonPropertyName("tankLevel")]
    public int TankLevel { get; set; }

    [JsonPropertyName("tankFuelsList")]
    public List<double> TankFuelsList { get; set; }

    [JsonPropertyName("tankLimitsList")]
    public List<double> TankLimitsList { get; set; }

    [JsonPropertyName("lastFueledShip")]
    public int LastFueledShip { get; set; }

    [JsonPropertyName("inventoryScore")]
    public double InventoryScore { get; set; }

    [JsonPropertyName("craftingXp")]
    public long CraftingXp { get; set; }

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("introShown")]
    public bool IntroShown { get; set; }

    [JsonPropertyName("infusingEnabledDeprecated")]
    public bool InfusingEnabledDeprecated { get; set; }
}

public class JsonPlayerArtifactsDb
{
    [JsonPropertyName("inventoryItemsList")]
    public List<JsonPlayerInventoryItem> InventoryItemsList { get; set; }

    [JsonPropertyName("itemSequence")]
    public int ItemSequence { get; set; }

    [JsonPropertyName("inventorySlotsList")]
    public List<object> InventorySlotsList { get; set; }

    [JsonPropertyName("activeArtifactsDeprecatedList")]
    public List<object> ActiveArtifactsDeprecatedList { get; set; }

    [JsonPropertyName("activeArtifactSetsList")]
    public List<JsonPlayerArtifactSet> ActiveArtifactSetsList { get; set; }

    [JsonPropertyName("savedArtifactSetsList")]
    public List<JsonPlayerArtifactSet> SavedArtifactSetsList { get; set; }

    [JsonPropertyName("artifactStatusList")]
    public List<JsonPlayerArtifactStatus> ArtifactStatusList { get; set; }

    [JsonPropertyName("fuelingMission")]
    public JsonPlayerFuelingMission FuelingMission { get; set; }

    [JsonPropertyName("missionInfosList")]
    public List<JsonPlayerExtendedMissionInfo> MissionInfosList { get; set; }

    [JsonPropertyName("missionArchiveList")]
    public List<JsonPlayerExtendedMissionInfo> MissionArchiveList { get; set; }
}

public class JsonPlayerInventoryItem
{
    [JsonPropertyName("itemId")]
    public int ItemId { get; set; }

    [JsonPropertyName("artifact")]
    public JsonPlayerArtifactInfo Artifact { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("serverId")]
    public string ServerId { get; set; }
}

public class JsonPlayerArtifactInfo
{
    [JsonPropertyName("spec")]
    public JsonPlayerArtifactSpec Spec { get; set; }

    [JsonPropertyName("stonesList")]
    public List<JsonPlayerArtifactSpec> StonesList { get; set; }
}

public class JsonPlayerArtifactSpec
{
    [JsonPropertyName("name")]
    public int Name { get; set; }

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("rarity")]
    public int Rarity { get; set; }

    [JsonPropertyName("egg")]
    public int Egg { get; set; }
}

public class JsonPlayerArtifactSet
{
    [JsonPropertyName("slotsList")]
    public List<JsonPlayerArtifactSlot> SlotsList { get; set; }

    [JsonPropertyName("uid")]
    public int? Uid { get; set; }
}

public class JsonPlayerArtifactSlot
{
    [JsonPropertyName("occupied")]
    public bool Occupied { get; set; }

    [JsonPropertyName("itemId")]
    public int ItemId { get; set; }
}

public class JsonPlayerArtifactStatus
{
    [JsonPropertyName("spec")]
    public JsonPlayerArtifactStatusSpec Spec { get; set; }

    [JsonPropertyName("discovered")]
    public bool Discovered { get; set; }

    [JsonPropertyName("craftable")]
    public bool Craftable { get; set; }

    [JsonPropertyName("recipeDiscovered")]
    public bool RecipeDiscovered { get; set; }

    [JsonPropertyName("seen")]
    public bool Seen { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public class JsonPlayerArtifactStatusSpec
{
    [JsonPropertyName("name")]
    public int Name { get; set; }

    [JsonPropertyName("level")]
    public int Level { get; set; }
}

public class JsonPlayerFuelingMission
{
    [JsonPropertyName("ship")]
    public int Ship { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("durationType")]
    public int DurationType { get; set; }

    [JsonPropertyName("fuelList")]
    public List<JsonPlayerFuel> FuelList { get; set; }

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("capacity")]
    public int Capacity { get; set; }

    [JsonPropertyName("targetArtifact")]
    public int TargetArtifact { get; set; }
}

public class JsonPlayerFuel
{
    [JsonPropertyName("egg")]
    public int Egg { get; set; }

    [JsonPropertyName("amount")]
    public double Amount { get; set; }
}

public class JsonPlayerExtendedMissionInfo
{
    [JsonPropertyName("ship")]
    public int Ship { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("durationType")]
    public int DurationType { get; set; }

    [JsonPropertyName("fuelList")]
    public List<JsonPlayerFuel> FuelList { get; set; }

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("durationSeconds")]
    public float DurationSeconds { get; set; }

    [JsonPropertyName("capacity")]
    public int Capacity { get; set; }

    [JsonPropertyName("qualityBump")]
    public double QualityBump { get; set; }

    [JsonPropertyName("targetArtifact")]
    public int TargetArtifact { get; set; }

    [JsonPropertyName("secondsRemaining")]
    public double SecondsRemaining { get; set; }

    [JsonPropertyName("startTimeDerived")]
    public double StartTimeDerived { get; set; }

    [JsonPropertyName("missionLog")]
    public string MissionLog { get; set; }

    [JsonPropertyName("identifier")]
    public string Identifier { get; set; }
}

public class JsonPlayerMailState
{
    [JsonPropertyName("readMailIdsList")]
    public List<string> ReadMailIdsList { get; set; }

    [JsonPropertyName("tipsStatesList")]
    public List<object> TipsStatesList { get; set; }

    [JsonPropertyName("tipsChecksum")]
    public string TipsChecksum { get; set; }
}

public class JsonPlayerShells
{
    [JsonPropertyName("introAlert")]
    public bool IntroAlert { get; set; }

    [JsonPropertyName("contractsIntroAlert")]
    public bool ContractsIntroAlert { get; set; }

    [JsonPropertyName("numNewList")]
    public List<int> NumNewList { get; set; }
}

public class JsonPlayerMisc
{
    [JsonPropertyName("chickenBtnPrefBig")]
    public bool ChickenBtnPrefBig { get; set; }

    [JsonPropertyName("freeHatcheryRefillGiven")]
    public bool FreeHatcheryRefillGiven { get; set; }

    [JsonPropertyName("lastShareFarmValue")]
    public double LastShareFarmValue { get; set; }

    [JsonPropertyName("lastShareSwarmFarmValue")]
    public double LastShareSwarmFarmValue { get; set; }

    [JsonPropertyName("lastShareSwarmSize")]
    public int LastShareSwarmSize { get; set; }

    [JsonPropertyName("lastPrestigeAlertSoulEggsDeprecated")]
    public int LastPrestigeAlertSoulEggsDeprecated { get; set; }

    [JsonPropertyName("friendRank")]
    public int FriendRank { get; set; }

    [JsonPropertyName("friendRankPop")]
    public int FriendRankPop { get; set; }

    [JsonPropertyName("globalRank")]
    public int GlobalRank { get; set; }

    [JsonPropertyName("globalRankPop")]
    public int GlobalRankPop { get; set; }

    [JsonPropertyName("challengesAlert")]
    public bool ChallengesAlert { get; set; }

    [JsonPropertyName("trophyAlert")]
    public bool TrophyAlert { get; set; }

    [JsonPropertyName("arAlert")]
    public bool ArAlert { get; set; }

    [JsonPropertyName("contractsAlert")]
    public bool ContractsAlert { get; set; }

    [JsonPropertyName("contractsAlertV2")]
    public bool ContractsAlertV2 { get; set; }

    [JsonPropertyName("coopAlert")]
    public bool CoopAlert { get; set; }

    [JsonPropertyName("coopAlertV2")]
    public bool CoopAlertV2 { get; set; }

    [JsonPropertyName("switchAlert")]
    public bool SwitchAlert { get; set; }

    [JsonPropertyName("eggOfProphecyAlert")]
    public bool EggOfProphecyAlert { get; set; }

    [JsonPropertyName("boostTokenAlert")]
    public bool BoostTokenAlert { get; set; }

    [JsonPropertyName("soulEggAlert")]
    public bool SoulEggAlert { get; set; }

    [JsonPropertyName("backupReminderAlert")]
    public bool BackupReminderAlert { get; set; }

    [JsonPropertyName("maxButtonAlert")]
    public bool MaxButtonAlert { get; set; }

    [JsonPropertyName("missionTargetAlert")]
    public bool MissionTargetAlert { get; set; }

    [JsonPropertyName("colleggtiblesAlert")]
    public bool ColleggtiblesAlert { get; set; }
}

public class JsonPlayerShellDb
{
    [JsonPropertyName("shellInventoryList")]
    public List<JsonPlayerShellInventory> ShellInventoryList { get; set; }

    [JsonPropertyName("shellElementInventoryList")]
    public List<JsonPlayerShellElementInventory> ShellElementInventoryList { get; set; }

    [JsonPropertyName("shellVariationInventoryList")]
    public List<JsonPlayerShellVariationInventory> ShellVariationInventoryList { get; set; }

    [JsonPropertyName("shellSetInventoryList")]
    public List<JsonPlayerShellSetInventory> ShellSetInventoryList { get; set; }

    [JsonPropertyName("shellObjectInventoryList")]
    public List<JsonPlayerShellObjectInventory> ShellObjectInventoryList { get; set; }

    [JsonPropertyName("newShellsDownloadedList")]
    public List<string> NewShellsDownloadedList { get; set; }

    [JsonPropertyName("newShellsSeenList")]
    public List<string> NewShellsSeenList { get; set; }

    [JsonPropertyName("lastShowcaseFeaturedTimeSeen")]
    public long LastShowcaseFeaturedTimeSeen { get; set; }

    [JsonPropertyName("lightingControlsUnlocked")]
    public bool LightingControlsUnlocked { get; set; }
}

public class JsonPlayerShellInventory
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; }

    [JsonPropertyName("owned")]
    public bool Owned { get; set; }
}

public class JsonPlayerShellElementInventory
{
    [JsonPropertyName("element")]
    public int Element { get; set; }

    [JsonPropertyName("setIdentifier")]
    public string SetIdentifier { get; set; }

    [JsonPropertyName("variationIdentifier")]
    public string VariationIdentifier { get; set; }

    [JsonPropertyName("decoratorIdentifier")]
    public string DecoratorIdentifier { get; set; }
}

public class JsonPlayerShellVariationInventory
{
    [JsonPropertyName("setIdentifier")]
    public string SetIdentifier { get; set; }

    [JsonPropertyName("ownedVariationsList")]
    public List<string> OwnedVariationsList { get; set; }
}

public class JsonPlayerShellSetInventory
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; }

    [JsonPropertyName("owned")]
    public bool Owned { get; set; }
}

public class JsonPlayerShellObjectInventory
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; }

    [JsonPropertyName("owned")]
    public bool Owned { get; set; }
}

public class JsonPlayerFarmConfig
{
    [JsonPropertyName("lockedElementsList")]
    public List<object> LockedElementsList { get; set; }

    [JsonPropertyName("shellConfigsList")]
    public List<JsonPlayerShellConfig> ShellConfigsList { get; set; }

    [JsonPropertyName("shellSetConfigsList")]
    public List<JsonPlayerShellSetConfig> ShellSetConfigsList { get; set; }

    [JsonPropertyName("configureChickensByGroup")]
    public bool ConfigureChickensByGroup { get; set; }

    [JsonPropertyName("groupConfigsList")]
    public List<JsonPlayerGroupConfig> GroupConfigsList { get; set; }

    [JsonPropertyName("chickenConfigsList")]
    public List<JsonPlayerChickenConfig> ChickenConfigsList { get; set; }

    [JsonPropertyName("lightingConfigEnabled")]
    public bool LightingConfigEnabled { get; set; }

    [JsonPropertyName("lightingConfig")]
    public JsonPlayerLightingConfig LightingConfig { get; set; }
}

public class JsonPlayerShellConfig
{
    [JsonPropertyName("assetType")]
    public int AssetType { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("shellIdentifier")]
    public string ShellIdentifier { get; set; }
}

public class JsonPlayerShellSetConfig
{
    [JsonPropertyName("element")]
    public int Element { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("shellSetIdentifier")]
    public string ShellSetIdentifier { get; set; }

    [JsonPropertyName("variationIdentifier")]
    public string VariationIdentifier { get; set; }

    [JsonPropertyName("decoratorIdentifier")]
    public string DecoratorIdentifier { get; set; }
}

public class JsonPlayerGroupConfig
{
    [JsonPropertyName("assetType")]
    public int AssetType { get; set; }

    [JsonPropertyName("groupIdentifier")]
    public string GroupIdentifier { get; set; }
}

public class JsonPlayerChickenConfig
{
    [JsonPropertyName("chickenIdentifier")]
    public string ChickenIdentifier { get; set; }

    [JsonPropertyName("hatIdentifier")]
    public string HatIdentifier { get; set; }
}

public class JsonPlayerLightingConfig
{
    [JsonPropertyName("lightDir")]
    public JsonPlayerVector3 LightDir { get; set; }

    [JsonPropertyName("lightDirectColor")]
    public JsonPlayerColor LightDirectColor { get; set; }

    [JsonPropertyName("lightDirectIntensity")]
    public double LightDirectIntensity { get; set; }

    [JsonPropertyName("lightAmbientColor")]
    public JsonPlayerColor LightAmbientColor { get; set; }

    [JsonPropertyName("lightAmbientIntensity")]
    public double LightAmbientIntensity { get; set; }

    [JsonPropertyName("fogColor")]
    public JsonPlayerColor FogColor { get; set; }

    [JsonPropertyName("fogNear")]
    public int FogNear { get; set; }

    [JsonPropertyName("fogFar")]
    public int FogFar { get; set; }

    [JsonPropertyName("fogDensity")]
    public double FogDensity { get; set; }
}

public class JsonPlayerVector3
{
    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("z")]
    public double Z { get; set; }
}

public class JsonPlayerColor
{
    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("z")]
    public double Z { get; set; }

    [JsonPropertyName("w")]
    public double W { get; set; }
}
