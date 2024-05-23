namespace HemSoft.EggIncTracker;

using System;
using System.Net.Http;
using System.Threading.Tasks;

using HemSoft.EggIncTracker.Models;

public static class Api
{
    private static readonly HttpClient client = new HttpClient();

    public static async Task<PlayerInfo> CallApi(string httpEndpoint)
    {
        try
        {
            HttpResponseMessage response = await client.GetAsync(httpEndpoint);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();
            var pi = PlayerInfo.ApiToPLayerInfo("EI6335140328505344", "HemSoft", result);
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
