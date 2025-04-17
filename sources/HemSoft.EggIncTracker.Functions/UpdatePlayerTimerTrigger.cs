namespace UpdatePlayerFunction;

using System;
using System.Globalization;
using System.Text;

using HemSoft.EggIncTracker.Data;
using HemSoft.EggIncTracker.Data.Dtos;
using HemSoft.EggIncTracker.Domain;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

public class UpdatePlayerTimerTrigger
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    // private readonly IApiService _apiService; // Commented out
    //private const string DiscordWebhookUrl = "https://discord.com/api/webhooks/1257956380875161680/1RuZ7b8i2JzpW_E4NUkWLgSF2t_JBjgfqar5h7kLmJavZ6tXH_VT8PIUr8R6zcGlAlRP";

    private const string DiscordWebhookUrl =
        "https://discord.com/api/webhooks/1350331849074937856/v7_DxPkI-aWYqwBASUqqtn9R66QIA_uAAZqSQsIfgBxMMyzW8t4Q86riBlR7pAMBn_9p";
    private const string KingFridayEid = "EI6335140328505344";
    private const string KingFridayPlayerName = "King Friday!";
    private const string KingSaturdayEid = "EI5435770400276480";
    private const string KingSaturdayPlayerName = "King Saturday!";
    private const string KingSundayEid = "EI6306349753958400";
    private const string KingSundayPlayerName = "King Sunday!";
    private const string KingMondayEid = "EI6725967592947712";
    private const string KingMondayPlayerName = "King Monday!";

    // Updated constructor to remove IApiService injection
    public UpdatePlayerTimerTrigger(ILoggerFactory loggerFactory, HttpClient httpClient)
    {
        _logger = loggerFactory.CreateLogger<UpdatePlayerTimerTrigger>();
        // _apiService = apiService; // Removed assignment
        _httpClient = httpClient;

        _logger.LogDebug($"UpdatePlayerTimerTrigger initialized with HttpClient BaseAddress: {_httpClient.BaseAddress} and Timeout: {_httpClient.Timeout.TotalSeconds} seconds");

        // Removed ServiceLocator registration as it's no longer used here or defined elsewhere
        // ServiceLocator.RegisterService(_logger);
    }

    [Function("UpdateMain")]
    public async Task UpdateMain([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer)
    {
        try
        {
            // Read the request body
            _logger.LogInformation($"Function UpdateMain() triggered at: {DateTime.Now}");

            await using var context = new EggIncContext();
            await context.Database.EnsureCreatedAsync();

            await ProcessPlayer(KingFridayEid, KingFridayPlayerName);
            await CheckPlayerContracts(KingFridayEid, KingFridayPlayerName);

            await ProcessPlayer(KingSaturdayEid, KingSaturdayPlayerName);
            await CheckPlayerContracts(KingSaturdayEid, KingSaturdayPlayerName);

            await ProcessPlayer(KingSundayEid, KingSundayPlayerName);
            await CheckPlayerContracts(KingSundayEid, KingSundayPlayerName);

            await ProcessPlayer(KingMondayEid, KingMondayPlayerName);
            await CheckPlayerContracts(KingMondayEid, KingMondayPlayerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateMain");
            await SendDiscordMessage(ex.ToString());
        }
        finally
        {
            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogDebug($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }

    [Function("UpdateAlts")]
    public async Task UpdateAlts([TimerTrigger("0 0/30 * * *")] TimerInfo myTimer)
    {
        try
        {
            // Read the request body
            _logger.LogInformation($"Function UpdateAlts() triggered at: {DateTime.Now}");

            await using var context = new EggIncContext();
            await context.Database.EnsureCreatedAsync();

            await ProcessPlayer(KingSaturdayEid, KingSaturdayPlayerName);
            await ProcessPlayer(KingSundayEid, KingSundayPlayerName);
            await ProcessPlayer(KingMondayEid, KingMondayPlayerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateAlts");
            await SendDiscordMessage(ex.ToString());
        }
        finally
        {
            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogDebug($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }

    [Function("CheckEvents")]
    public async Task CheckEvents([TimerTrigger("0 0 * * * *")] TimerInfo myTimer)
    {
        try
        {
            // Read the request body
            _logger.LogInformation($"Function CheckEvents() triggered at: {DateTime.Now}");

            await using var context = new EggIncContext();
            await context.Database.EnsureCreatedAsync();

            var (events, contracts) = Api.CallEventInfoApi("EI6335140328505344", "King Friday!").Result;
            foreach (var eventInfo in events.Where(eventInfo => EventManager.SaveEvent(eventInfo, _logger)))
            {
                await SendDiscordMessage(eventInfo);
            }

            foreach (var contract in contracts.Where(contract => ContractManager.SaveContract(contract, _logger)))
            {
                await SendDiscordMessage(contract);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CheckEvents");
            await SendDiscordMessage(ex.ToString());
        }
        finally
        {
            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogDebug($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }

    [Function("CheckPlayerContracts")]
    public async Task CheckPlayerContracts([TimerTrigger("0 0 * * * *")] TimerInfo myTimer)
    {
        try
        {
            // Read the request body
            _logger.LogInformation($"Function CheckPlayerContracts() triggered at: {DateTime.Now}");

            await using var context = new EggIncContext();
            await context.Database.EnsureCreatedAsync();

            await CheckPlayerContracts(KingFridayEid, KingFridayPlayerName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CheckPlayerContracts");
            await SendDiscordMessage(ex.ToString());
        }
        finally
        {
            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogDebug($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }

    [Function("CheckMajSEPlayerRankings")]
    public async Task CheckMajSEPlayerRankings([TimerTrigger("0 0 * * * *")] TimerInfo myTimer)
    {
        try
        {
            // Read the request body
            _logger.LogInformation($"Function CheckMajSEPlayerRankings() triggered at: {DateTime.Now}");

            await using var context = new EggIncContext();
            await context.Database.EnsureCreatedAsync();

            await CheckMajSEPlayerRankings();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CheckPlayerContracts");
            await SendDiscordMessage(ex.ToString());
        }
        finally
        {
            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogDebug($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }

    #region CheckMajSEPlayerRankings()

    private async Task CheckMajSEPlayerRankings()
    {
        var playerMajSERankings = await Api.CallMajSEPlayerRankingsApi();
        foreach (var p in playerMajSERankings)
        {
            var (success, previousRanking) = await MajPlayerRankingManager.SaveMajPlayerRankingAsync(p, _logger);
            if (!success)
            {
                _logger.LogWarning("Failed to save ranking for {PlayerIGN}", p.IGN);
            }
        }
    }

    #endregion

    #region CheckPlayerContracts()

    private async Task CheckPlayerContracts(string playerEid, string playerName)
    {
        var playerContracts = Api.CallPlayerContractsApi(playerEid, playerName).Result;
        foreach (var playerContract in playerContracts)
        {
            var result = PlayerContractManager.SavePlayerContract(playerContract, _logger);
            if (result)
            {
                await SendDiscordMessage(playerContract);
            }
        }
    }

    #endregion

    #region ProcessPlayer()

    private async Task ProcessPlayer(string playerEid, string playerName)
    {
        var (player, fullPlayerInfo) = Api.CallPlayerInfoApi(playerEid, playerName).Result;
        var (saveResult, previousEntry) = PlayerManager.SavePlayer(player, fullPlayerInfo, _logger);
        await SendDiscordMessage(player, fullPlayerInfo, previousEntry, saveResult);
    }

    #endregion

    #region SendDiscordMessage()

    private async Task SendDiscordMessage(PlayerDto player, JsonPlayerRoot fullPlayerInfo, PlayerDto previousEntry, bool stiged)
    {
        var seDiff = string.Empty;
        var message = string.Empty;

        if (stiged)
        {
            seDiff = BigNumberCalculator.CalculateDifference(previousEntry.SoulEggsFull, player.SoulEggsFull);
            message = $"_**{player.PlayerName}: Change detected**_ {player.Title} {player.TitleProgress:F2}%:\n" +
                      $"<:old:1326734414461403177> `{previousEntry.ProphecyEggs,3}` <:egg_prophecy:460316384530923520> `{previousEntry.SoulEggs,8}` <:egg_soulegg:455666530290499586> `{previousEntry.EarningsBonusPercentage,10}` MER=`{previousEntry.MER:F2}` JER=`{previousEntry.JER:F2}` LLC=`{previousEntry.LLC}`\n" +
                      $":new: `{player.ProphecyEggs,3}` <:egg_prophecy:460316384530923520> `{player.SoulEggs,8}` <:egg_soulegg:455666530290499586> `{player.EarningsBonusPercentage,10}` MER=`{player.MER:F2}` JER=`{player.JER:F2}` LLC=`{player.LLC}`\n" +
                      $":chart_with_upwards_trend: `{player.ProphecyEggs - previousEntry.ProphecyEggs,3}` <:egg_prophecy:460316384530923520> `{seDiff,8}`\n";
        }


        // Contract Information:
        if (fullPlayerInfo.Contracts.ContractsList.Count > 0)
        {
            message += $"_**{player.PlayerName} -- Active Contracts **_:\n";
            var status = string.Empty;
            foreach (var contract in fullPlayerInfo.Contracts.ContractsList)
            {
                var contractDetails = await Api.CallPlayerContractDetailsApi(player.EID, player.PlayerName, contract.Contract.Identifier, contract.CoopIdentifier);

                var coopUrl = $"https://eicoop-carpet.netlify.app/{contract.Contract.Identifier}/{contract.CoopIdentifier}/";
                var myRank = 0;

                if (contract.CoopContributionFinalized)
                {
                    status = "<:contract_scroll_green:1291615037059891280> ";
                }
                else if (contractDetails is { AllGoalsAchieved: true })
                {
                    status = "<:contract_scroll_yellow:1291615040817860641> ";
                }

                if (contractDetails != null)
                {
                    var totalAmount = contractDetails.TotalAmount;
                    var totalAmountString = totalAmount.ToString(CultureInfo.InvariantCulture);
                    var formattedTotalAmount = Utils.FormatBigInteger(totalAmountString, false, true);

                    var goalValue = contract.Contract.GradeSpecsList[4].GoalsList.Last().TargetAmount;
                    var goalValueString = goalValue.ToString(CultureInfo.InvariantCulture);
                    var formattedGoalValue = Utils.FormatBigInteger(goalValueString, false, true);

                    var percentComplete = (totalAmount / (decimal)goalValue) * 100;
                    var myContribution = contractDetails.ContributorsList.OrderByDescending(x => x.ContributionAmount).First(y => y.UserName == player.PlayerName);
                    var orderedContributionList =
                        contractDetails.ContributorsList.OrderByDescending(x => x.ContributionAmount).ToList();
                    myRank = orderedContributionList.FindIndex(x => x.UserName == player.PlayerName) + 1;
                    status = $"Goals: `{contract.NumGoalsAchieved}/3` -- Pr.: `{formattedTotalAmount}/{formattedGoalValue} ({percentComplete:F2}%)`";
                }

                if (myRank > 0)
                {
                    message += $"_Contract_: `{contract.Contract.Name,-20}` -- `{contract.CoopIdentifier,-10}` -- " +
                               $"`{myRank,2}/{contract.Contract.MaxCoopSize,2}`:farmer: -- Started: :calendar:`{Utils.ConvertUnixTimestampToCST(contract.Contract.StartTime),-22}` -- " +
                               $"{status}" + "\n";
                }
                else
                {
                    message += $"_Contract_: `{contract.Contract.Name,-20}` -- `{contract.CoopIdentifier,-10}` -- " +
                               $"`   {contract.Contract.MaxCoopSize,2}`:farmer: -- Started: :calendar:`{Utils.ConvertUnixTimestampToCST(contract.Contract.StartTime),-22}` -- " +
                               $"{status}" + "\n";
                }
            }
        }

        if (!stiged && !fullPlayerInfo.Contracts.ContractsList.Any())
        {
            return;
        }

        // Fuel Status:
        var x = 0;
        message += "_**Fuel Status**_:\n:fuelpump: ";
        foreach (var f in fullPlayerInfo.Artifacts.TankFuelsList)
        {
            x++;
            if (f > 0)
            {
                message += $"`{x}={Utils.FormatBigInteger(f.ToString(CultureInfo.InvariantCulture), true)}/{Utils.FormatBigInteger((fullPlayerInfo.Artifacts.TankLimitsList[x - 1] * 500000000000000).ToString(CultureInfo.InvariantCulture), true)}` ";
            }
        }

        // Mission Status:
        var approxTime = Utils.ConvertUnixTimestampToCST(fullPlayerInfo.ApproxTime);

        //fullPlayerInfo.Game.
        message += "\n_**Henliner Mission return times**_: ";
        var timeLeft = Utils.DoubleToReadableTime(fullPlayerInfo.ArtifactsDb.MissionInfosList[0].SecondsRemaining);
        message += $"Time left: `{timeLeft}`\n";
        if (fullPlayerInfo.ArtifactsDb.MissionInfosList.Count > 0)
        {
            message += $"'{approxTime.AddSeconds(fullPlayerInfo.ArtifactsDb.MissionInfosList[0].SecondsRemaining):yyyy-MM-dd HH:mm:ss}' ";
        }
        if (fullPlayerInfo.ArtifactsDb.MissionInfosList.Count > 1)
        {
            message += $"- '{approxTime.AddSeconds(fullPlayerInfo.ArtifactsDb.MissionInfosList[1].SecondsRemaining):yyyy-MM-dd HH:mm:ss}' ";
        }
        if (fullPlayerInfo.ArtifactsDb.MissionInfosList.Count > 2)
        {
            message += $"- '{approxTime.AddSeconds(fullPlayerInfo.ArtifactsDb.MissionInfosList[2].SecondsRemaining):yyyy-MM-dd HH:mm:ss}'";
        }
        message += "\n===================================================================================\n";

        // Prepare the message content
        var content = new StringContent(JsonConvert.SerializeObject(new { content = message }), Encoding.UTF8, "application/json");

        // Make the API call
        var response = await _httpClient.PostAsync(DiscordWebhookUrl, content);
    }

    private async Task SendDiscordMessage(string message)
    {
        // Prepare the message content
        var content = new StringContent(JsonConvert.SerializeObject(new { content = message }), Encoding.UTF8, "application/json");

        // Make the API call
        HttpResponseMessage response = await _httpClient.PostAsync(DiscordWebhookUrl, content);
    }

    private async Task SendDiscordMessage(EventDto eventInfo)
    {
        // Prepare the message content
        var icon = eventInfo.EventType switch
        {
            "hab-sale" => "<:hab_coop:658552734860967936> ",
            "piggy-boost" => "<:icon_piggy_bank:715663243665997884> ",
            "mission-fuel" => ":rocket: ",
            "boost-duration" => "<:boost_beacon_purple:1291611659634348065> ",
            "prestige-boost" => "<:egg_soulegg:455666530290499586> ",
            _ => ":question: "
        };

        var message = ":zap: New Event Started!! `" + eventInfo.StartTime.ToString("yyyy-MM-dd hh:mm:ss tt") + "` -- " + icon + eventInfo.SubTitle;
        var content = new StringContent(JsonConvert.SerializeObject(new { content = message }), Encoding.UTF8, "application/json");

        // Make the API call
        await _httpClient.PostAsync(DiscordWebhookUrl, content);
    }

    private async Task SendDiscordMessage(ContractDto contract)
    {
        // Prepare the message content
        var icon = string.Empty;

        var message = ":zap: New Contract Found!! `" + contract.StartTime.ToString("yyyy-MM-dd hh:mm:ss tt") + "` -- " + contract.Name;
        if (contract.HasProphecyEggReward)
        {
            message += " -- <:egg_prophecy:460316384530923520>";
        }
        if (contract.HasArtifactCaseReward)
        {
            message += " -- Artifact Case!";
        }
        var content = new StringContent(JsonConvert.SerializeObject(new { content = message }), Encoding.UTF8, "application/json");

        // Make the API call
        await _httpClient.PostAsync(DiscordWebhookUrl, content);
    }

    private async Task SendDiscordMessage(PlayerContractDto playerContract)
    {
        // Prepare the message content
        var icon = string.Empty;

        if (string.IsNullOrEmpty(playerContract.CoopId))
        {
            return;
        }
        var message = ":zap: New Player Contract Coop Found!! CoopId: " + playerContract.CoopId + " -- KevId: " + playerContract.KevId;
        var content = new StringContent(JsonConvert.SerializeObject(new { content = message }), Encoding.UTF8, "application/json");

        // Make the API call
        await _httpClient.PostAsync(DiscordWebhookUrl, content);
    }

    #endregion
}
