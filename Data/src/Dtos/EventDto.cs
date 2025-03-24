namespace HemSoft.EggIncTracker.Data.Dtos;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

public class EventDto
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string EventId { get; set; } = null!;

    [Required]
    public int SecondsRemaining { get; set; }

    [Required]
    [MaxLength(50)]
    public string EventType { get; set; } = null!;

    [Required]
    public float Multiplier { get; set; }

    [Required]
    [MaxLength(50)]
    public string SubTitle { get; set; } = null!;

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public int DurationInSeconds { get; set; }

    public DateTime? EndTime { get; set; }

    public static List<EventDto> ApiToEvents(string playerId, string playerName, string apiResponse)
    {
        List<EventDto> events = [];
        JsonEventRoot? root = null;

        try
        {
            root = JsonConvert.DeserializeObject<JsonEventRoot>(apiResponse);
            if (root?.Contracts is null)
            {
                Console.WriteLine("Bummer.");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        foreach (var ev in root?.Events.EventsList)
        {
            var eventInfo = new EventDto
            {
                EventId = ev.Identifier,
                SecondsRemaining = (int)ev.SecondsRemaining,
                EventType = ev.Type,
                Multiplier = (float)ev.Multiplier,
                SubTitle = ev.Subtitle,
                StartTime = ConvertUnixTimestampToCST(ev.StartTime),
                DurationInSeconds = ev.Duration,
                EndTime = ConvertUnixTimestampToCST(ev.StartTime).AddSeconds(ev.Duration)
            };
            events.Add(eventInfo);
        }

        return events;
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

public class JsonEventRoot
{
    [JsonPropertyName("sales")]
    public JsonEventSales Sales { get; set; }

    [JsonPropertyName("events")]
    public JsonEventEvents Events { get; set; }

    [JsonPropertyName("contracts")]
    public JsonEventContracts Contracts { get; set; }

    [JsonPropertyName("customEggsList")]
    public List<JsonEventCustomEgg> CustomEggsList { get; set; }

    [JsonPropertyName("totalEop")]
    public int TotalEop { get; set; }

    [JsonPropertyName("serverTime")]
    public double ServerTime { get; set; }

    [JsonPropertyName("maxEop")]
    public int MaxEop { get; set; }
}

public class JsonEventSales
{
    [JsonPropertyName("salesList")]
    public List<object> SalesList { get; set; }
}

public class JsonEventEvents
{
    [JsonPropertyName("eventsList")]
    public List<JsonEventEvent> EventsList { get; set; }
}

public class JsonEventEvent
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; }

    [JsonPropertyName("secondsRemaining")]
    public double SecondsRemaining { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("multiplier")]
    public double Multiplier { get; set; }

    [JsonPropertyName("subtitle")]
    public string Subtitle { get; set; }

    [JsonPropertyName("startTime")]
    public long StartTime { get; set; }

    [JsonPropertyName("duration")]
    public int Duration { get; set; }

    [JsonPropertyName("ccOnly")]
    public bool CcOnly { get; set; }
}

public class JsonEventContracts
{
    [JsonPropertyName("contractsList")]
    public List<JsonEventContract> ContractsList { get; set; }
}

public class JsonEventContract
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("egg")]
    public int Egg { get; set; }

    [JsonPropertyName("goalsList")]
    public List<object> GoalsList { get; set; }

    [JsonPropertyName("goalSetsList")]
    public List<JsonEventGoalSet> GoalSetsList { get; set; }

    [JsonPropertyName("gradeSpecsList")]
    public List<JsonEventGradeSpec> GradeSpecsList { get; set; }

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
    public long ExpirationTime { get; set; }

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

public class JsonEventGoalSet
{
    [JsonPropertyName("goalsList")]
    public List<JsonEventGoal> GoalsList { get; set; }
}

public class JsonEventGoal
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
    public int RewardAmount { get; set; }

    [JsonPropertyName("targetSoulEggs")]
    public long TargetSoulEggs { get; set; }
}

public class JsonEventGradeSpec
{
    [JsonPropertyName("grade")]
    public int Grade { get; set; }

    [JsonPropertyName("goalsList")]
    public List<JsonEventGoal> GoalsList { get; set; }

    [JsonPropertyName("modifiersList")]
    public List<JsonEventModifier> ModifiersList { get; set; }

    [JsonPropertyName("lengthSeconds")]
    public int LengthSeconds { get; set; }
}

public class JsonEventModifier
{
    [JsonPropertyName("dimension")]
    public int Dimension { get; set; }

    [JsonPropertyName("value")]
    public double Value { get; set; }
}

public class JsonEventCustomEgg
{
    [JsonPropertyName("identifier")]
    public string Identifier { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("value")]
    public double Value { get; set; }

    [JsonPropertyName("hatcheryId")]
    public string HatcheryId { get; set; }

    [JsonPropertyName("hatcheryMaxX")]
    public double HatcheryMaxX { get; set; }

    [JsonPropertyName("icon")]
    public JsonEventIcon Icon { get; set; }

    [JsonPropertyName("iconWidth")]
    public int IconWidth { get; set; }

    [JsonPropertyName("iconHeight")]
    public int IconHeight { get; set; }

    [JsonPropertyName("buffsList")]
    public List<JsonEventModifier> BuffsList { get; set; }
}

public class JsonEventIcon
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("directory")]
    public string Directory { get; set; }

    [JsonPropertyName("ext")]
    public string Ext { get; set; }

    [JsonPropertyName("compressed")]
    public bool Compressed { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("checksum")]
    public string Checksum { get; set; }
}