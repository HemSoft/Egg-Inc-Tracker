namespace HemSoft.EggIncTracker.Domain;

using System;
using System.Net.Http;
using System.Threading.Tasks;

using HemSoft.EggIncTracker.Data.Dtos;

using Newtonsoft.Json;
using static HemSoft.EggIncTracker.Data.Dtos.ContractDetailsDto;

public static class Api
{
    private static readonly HttpClient client = new HttpClient();

    public static async Task<ContractDetailsDto.JsonContractDetailsResponse> CallPlayerContractDetailsApi(string eId, string playerName, string kevId, string coopId)
    {
        try
        {
            var httpEndpoint = $"https://ei_worker.tylertms.workers.dev/contract?EID={eId}&contract={kevId}&coop={coopId}";
            var response = await client.GetAsync(httpEndpoint);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            var root = JsonConvert.DeserializeObject<JsonContractDetailsResponse>(result);
            return root;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("\nException Caught!");
            Console.WriteLine("Message :{0} ", e.Message);
            throw;
        }
    }

    public static async Task<(PlayerDto, JsonPlayerRoot)> CallPlayerInfoApi(string eId, string playerName)
    {
        try
        {
            var httpEndpoint = "https://eggincdatacollection.azurewebsites.net/api/formulae/all?eid=" + eId;
            HttpResponseMessage response = await client.GetAsync(httpEndpoint);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();

            var httpEndpoint2 = "https://ei_worker.tylertms.workers.dev/backup?EID=" + eId;
            HttpResponseMessage response2 = await client.GetAsync(httpEndpoint2);
            response2.EnsureSuccessStatusCode();
            var result2 = await response2.Content.ReadAsStringAsync();

            var (pi, fullPlayerInfo) = PlayerDto.ApiToPlayer(eId, playerName, result, result2);
            pi.EarningsBonusPercentage = PlayerManager.CalculateEarningsBonusPercentage(pi);
            (pi.Title, pi.NextTitle, pi.TitleProgress) = 
                PlayerManager.GetTitleWithProgress(PlayerManager.CalculateEarningsBonusPercentageNumber(pi));
            pi.ProjectedTitleChange = PlayerManager.CalculateProjectedTitleChange(pi);
            return (pi, fullPlayerInfo);
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("\nException Caught!");
            Console.WriteLine("Message :{0} ", e.Message);
            throw;
        }
    }

    public static async Task<(List<EventDto>, List<ContractDto>)> CallEventInfoApi(string eId, string playerName)
    {
        try
        {
            var httpEndpoint = "https://ei_worker.tylertms.workers.dev/periodicals?EID=" + eId;
            HttpResponseMessage response = await client.GetAsync(httpEndpoint);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();

            var ei = EventDto.ApiToEvents(eId, playerName, result);
            var ci = ContractDto.ApiToContracts(eId, playerName, result);
            return (ei, ci);
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("\nException Caught!");
            Console.WriteLine("Message :{0} ", e.Message);
            throw;
        }
    }

    public static async Task<List<PlayerContractDto>> CallPlayerContractsApi(string eId, string playerName)
    {
        try
        {
            var httpEndpoint = "https://ei_worker.tylertms.workers.dev/archive?EID=" + eId;
            HttpResponseMessage response = await client.GetAsync(httpEndpoint);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();

            var pc = PlayerContractDto.ApiToPlayerContracts(eId, playerName, result);
            return pc;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("\nException Caught!");
            Console.WriteLine("Message :{0} ", e.Message);
            throw;
        }
    }

    public static async Task<List<MajPlayerRankingDto>> CallMajSEPlayerRankingsApi()
    {
        try
        {
            var spreadSheetId = "17juaBpcUiw1Rw3sMnVRbRkY_rxW-AdTCD-WyIO8YPs8";
            var range = "P1:Y1000";
            var apiKey = "AIzaSyBHa9pXDpkfxAtRKc2uzfrX5Q4KdjJ3dkw";
            var httpEndpoint = $"https://sheets.googleapis.com/v4/spreadsheets/{spreadSheetId}/values/SE!{range}?key={apiKey}";
            HttpResponseMessage response = await client.GetAsync(httpEndpoint);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();

            var prs = MajPlayerRankingDto.ApiToMajPlayerRankings(result);
            return prs;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("\nException Caught!");
            Console.WriteLine("Message :{0} ", e.Message);
            throw;
        }
    }

}
