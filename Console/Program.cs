namespace HemSoft.EggIncTracker;

using HemSoft.EggIncTracker.Data;
using HemSoft.EggIncTracker.Domain;

public static class Program
{
    public static async Task Main(string[] args)
    {
        using var context = new EggIncContext();
        context.Database.EnsureCreated();

        var player = await Api.CallApi("EI6335140328505344", "King Friday!", "https://eggincdatacollection.azurewebsites.net/api/formulae/all?eid=EI6335140328505344");
        PlayerManager.SavePlayer(player, null);
        var player2 = await Api.CallApi("EI5435770400276480", "HemSoft02", "https://eggincdatacollection.azurewebsites.net/api/formulae/all?eid=EI5435770400276480");
        PlayerManager.SavePlayer(player2, null);
        var player3 = await Api.CallApi("EI6306349753958400", "HemSoft03", "https://eggincdatacollection.azurewebsites.net/api/formulae/all?eid=EI6306349753958400");
        PlayerManager.SavePlayer(player3, null);

        Console.WriteLine(player.ToString());
        Console.WriteLine(player2.ToString());
        Console.WriteLine(player3.ToString());
        Console.WriteLine("\nPress any key to exit.");
        Console.ReadKey();
    }
}
