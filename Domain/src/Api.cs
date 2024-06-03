namespace HemSoft.EggIncTracker.Domain;

using System;
using System.Net.Http;
using System.Threading.Tasks;

using HemSoft.EggIncTracker.Data.Dtos;

public static class Api
{
    private static readonly HttpClient client = new HttpClient();

    public static async Task<PlayerDto> CallApi(string eId, string playerName, string httpEndpoint)
    {
        try
        {
            HttpResponseMessage response = await client.GetAsync(httpEndpoint);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            var pi = PlayerDto.ApiToPlayer(eId, playerName, result);
            pi.EarningsBonusPercentage = PlayerManager.CalculateEarningsBonusPercentage(pi);
            (pi.Title, pi.NextTitle, pi.TitleProgress) = 
                PlayerManager.GetTitleWithProgress(PlayerManager.CalculateEarningsBonusPercentageNumber(pi));
            pi.ProjectedTitleChange = PlayerManager.CalculateProjectedTitleChange(pi);
            return pi;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("\nException Caught!");
            Console.WriteLine("Message :{0} ", e.Message);
            throw;
        }
    }
}
