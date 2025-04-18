﻿namespace HemSoft.EggIncTracker.Data.Dtos;

using System.Text.Json.Serialization;

using Newtonsoft.Json;

public class ContractDetailsDto
{
    public static JsonContractDetailsResponse ApiToContractDetails(string playerId, string playerName, string apiResponse)
    {
        var root = JsonConvert.DeserializeObject<JsonContractDetailsResponse>(apiResponse);
        return root;
    }

    public class JsonContractDetailsResponse
    {
        [JsonPropertyName("responseStatus")]
        public int ResponseStatus { get; set; }

        [JsonPropertyName("contractIdentifier")]
        public string ContractIdentifier { get; set; }

        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("coopIdentifier")]
        public string CoopIdentifier { get; set; }

        [JsonPropertyName("grade")]
        public int Grade { get; set; }

        [JsonPropertyName("contributorsList")]
        public List<JsonContractDetailsContributor> ContributorsList { get; set; }

        [JsonPropertyName("autoGenerated")]
        public bool AutoGenerated { get; set; }

        [JsonPropertyName("pb_public")]
        public bool PbPublic { get; set; }

        [JsonPropertyName("creatorId")]
        public string CreatorId { get; set; }

        [JsonPropertyName("secondsRemaining")]
        public double SecondsRemaining { get; set; }

        [JsonPropertyName("allGoalsAchieved")]
        public bool AllGoalsAchieved { get; set; }

        [JsonPropertyName("allMembersReporting")]
        public bool AllMembersReporting { get; set; }

        [JsonPropertyName("gracePeriodSecondsRemaining")]
        public double GracePeriodSecondsRemaining { get; set; }

        [JsonPropertyName("clearedForExit")]
        public bool ClearedForExit { get; set; }

        [JsonPropertyName("giftsList")]
        public List<object> GiftsList { get; set; }

        [JsonPropertyName("chickenRunsList")]
        public List<object> ChickenRunsList { get; set; }

        [JsonPropertyName("localTimestamp")]
        public int LocalTimestamp { get; set; }
    }

    public class JsonContractDetailsContributor
    {
        [JsonPropertyName("uuid")]
        public string Uuid { get; set; }

        [JsonPropertyName("userId")]
        public string UserId { get; set; }

        [JsonPropertyName("userName")]
        public string UserName { get; set; }

        [JsonPropertyName("contractIdentifier")]
        public string ContractIdentifier { get; set; }

        [JsonPropertyName("contributionAmount")]
        public decimal ContributionAmount { get; set; }

        [JsonPropertyName("contributionRate")]
        public double ContributionRate { get; set; }

        [JsonPropertyName("soulPower")]
        public double SoulPower { get; set; }

        [JsonPropertyName("productionParams")]
        public JsonContractDetailsProductionParams ProductionParams { get; set; }

        [JsonPropertyName("farmInfo")]
        public JsonContractDetailsFarmInfo FarmInfo { get; set; }

        [JsonPropertyName("recentlyActive")]
        public bool RecentlyActive { get; set; }

        [JsonPropertyName("active")]
        public bool Active { get; set; }

        [JsonPropertyName("ccMember")]
        public bool CcMember { get; set; }

        [JsonPropertyName("leech")]
        public bool Leech { get; set; }

        [JsonPropertyName("finalized")]
        public bool Finalized { get; set; }

        [JsonPropertyName("timeCheatDetected")]
        public bool TimeCheatDetected { get; set; }

        [JsonPropertyName("platform")]
        public int Platform { get; set; }

        [JsonPropertyName("autojoined")]
        public bool Autojoined { get; set; }

        [JsonPropertyName("boostTokens")]
        public int BoostTokens { get; set; }

        [JsonPropertyName("boostTokensSpent")]
        public int BoostTokensSpent { get; set; }

        [JsonPropertyName("buffHistoryList")]
        public List<JsonContractDetailsBuffHistory> BuffHistoryList { get; set; }

        [JsonPropertyName("chickenRunCooldown")]
        public string ChickenRunCooldown { get; set; }
    }

    public class JsonContractDetailsProductionParams
    {
        [JsonPropertyName("farmPopulation")]
        public long FarmPopulation { get; set; }

        [JsonPropertyName("farmCapacity")]
        public long FarmCapacity { get; set; }

        [JsonPropertyName("elr")]
        public double Elr { get; set; }

        [JsonPropertyName("ihr")]
        public double Ihr { get; set; }

        [JsonPropertyName("sr")]
        public double Sr { get; set; }

        [JsonPropertyName("delivered")]
        public decimal Delivered { get; set; }
    }

    public class JsonContractDetailsFarmInfo
    {
        [JsonPropertyName("clientVersion")]
        public int ClientVersion { get; set; }

        [JsonPropertyName("soulEggs")]
        public decimal SoulEggs { get; set; }

        [JsonPropertyName("eggsOfProphecy")]
        public int EggsOfProphecy { get; set; }

        [JsonPropertyName("permitLevel")]
        public int PermitLevel { get; set; }

        [JsonPropertyName("hyperloopStation")]
        public bool HyperloopStation { get; set; }

        [JsonPropertyName("eggMedalLevelList")]
        public List<int> EggMedalLevelList { get; set; }

        [JsonPropertyName("epicResearchList")]
        public List<JsonContractDetailsResearch> EpicResearchList { get; set; }

        [JsonPropertyName("eggType")]
        public int EggType { get; set; }

        [JsonPropertyName("cashOnHand")]
        public string CashOnHand { get; set; }

        [JsonPropertyName("habsList")]
        public List<int> HabsList { get; set; }

        [JsonPropertyName("habPopulationList")]
        public List<long> HabPopulationList { get; set; }

        [JsonPropertyName("habCapacityList")]
        public List<long> HabCapacityList { get; set; }

        [JsonPropertyName("vehiclesList")]
        public List<int> VehiclesList { get; set; }

        [JsonPropertyName("trainLengthList")]
        public List<int> TrainLengthList { get; set; }

        [JsonPropertyName("silosOwned")]
        public int SilosOwned { get; set; }

        [JsonPropertyName("commonResearchList")]
        public List<JsonContractDetailsResearch> CommonResearchList { get; set; }

        [JsonPropertyName("activeBoostsList")]
        public List<object> ActiveBoostsList { get; set; }

        [JsonPropertyName("boostTokensOnHand")]
        public int BoostTokensOnHand { get; set; }

        [JsonPropertyName("equippedArtifactsList")]
        public List<JsonContractDetailsEquippedArtifact> EquippedArtifactsList { get; set; }

        [JsonPropertyName("artifactInventoryScore")]
        public int ArtifactInventoryScore { get; set; }

        [JsonPropertyName("farmAppearance")]
        public JsonContractDetailsFarmAppearance FarmAppearance { get; set; }

        [JsonPropertyName("timestamp")]
        public double Timestamp { get; set; }
    }

    public class JsonContractDetailsResearch
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("level")]
        public int Level { get; set; }
    }

    public class JsonContractDetailsEquippedArtifact
    {
        [JsonPropertyName("spec")]
        public JsonContractDetailsArtifactSpec Spec { get; set; }

        [JsonPropertyName("stonesList")]
        public List<JsonContractDetailsArtifactSpec> StonesList { get; set; }
    }

    public class JsonContractDetailsArtifactSpec
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

    public class JsonContractDetailsFarmAppearance
    {
        [JsonPropertyName("lockedElementsList")]
        public List<object> LockedElementsList { get; set; }

        [JsonPropertyName("shellConfigsList")]
        public List<JsonContractDetailsShellConfig> ShellConfigsList { get; set; }

        [JsonPropertyName("shellSetConfigsList")]
        public List<JsonContractDetailsShellSetConfig> ShellSetConfigsList { get; set; }

        [JsonPropertyName("configureChickensByGroup")]
        public bool ConfigureChickensByGroup { get; set; }

        [JsonPropertyName("groupConfigsList")]
        public List<JsonContractDetailsGroupConfig> GroupConfigsList { get; set; }

        [JsonPropertyName("chickenConfigsList")]
        public List<JsonContractDetailsChickenConfig> ChickenConfigsList { get; set; }

        [JsonPropertyName("lightingConfigEnabled")]
        public bool LightingConfigEnabled { get; set; }

        [JsonPropertyName("lightingConfig")]
        public JsonContractDetailsLightingConfig LightingConfig { get; set; }
    }

    public class JsonContractDetailsShellConfig
    {
        [JsonPropertyName("assetType")]
        public int AssetType { get; set; }

        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("shellIdentifier")]
        public string ShellIdentifier { get; set; }
    }

    public class JsonContractDetailsShellSetConfig
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

    public class JsonContractDetailsGroupConfig
    {
        [JsonPropertyName("assetType")]
        public int AssetType { get; set; }

        [JsonPropertyName("groupIdentifier")]
        public string GroupIdentifier { get; set; }
    }

    public class JsonContractDetailsChickenConfig
    {
        [JsonPropertyName("chickenIdentifier")]
        public string ChickenIdentifier { get; set; }

        [JsonPropertyName("hatIdentifier")]
        public string HatIdentifier { get; set; }
    }

    public class JsonContractDetailsLightingConfig
    {
        [JsonPropertyName("lightDir")]
        public JsonContractDetailsVector3 LightDir { get; set; }

        [JsonPropertyName("lightDirectColor")]
        public JsonContractDetailsVector4 LightDirectColor { get; set; }

        [JsonPropertyName("lightDirectIntensity")]
        public double LightDirectIntensity { get; set; }

        [JsonPropertyName("lightAmbientColor")]
        public JsonContractDetailsVector4 LightAmbientColor { get; set; }

        [JsonPropertyName("lightAmbientIntensity")]
        public double LightAmbientIntensity { get; set; }

        [JsonPropertyName("fogColor")]
        public JsonContractDetailsVector4 FogColor { get; set; }

        [JsonPropertyName("fogNear")]
        public int FogNear { get; set; }

        [JsonPropertyName("fogFar")]
        public int FogFar { get; set; }

        [JsonPropertyName("fogDensity")]
        public double FogDensity { get; set; }
    }

    public class JsonContractDetailsVector3
    {
        [JsonPropertyName("x")]
        public double X { get; set; }

        [JsonPropertyName("y")]
        public double Y { get; set; }

        [JsonPropertyName("z")]
        public double Z { get; set; }
    }

    public class JsonContractDetailsVector4
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

    public class JsonContractDetailsBuffHistory
    {
        [JsonPropertyName("eggLayingRate")]
        public double EggLayingRate { get; set; }

        [JsonPropertyName("earnings")]
        public double Earnings { get; set; }

        [JsonPropertyName("serverTimestamp")]
        public double ServerTimestamp { get; set; }
    }
}