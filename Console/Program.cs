namespace HemSoft.EggIncTracker;

using HemSoft.EggIncTracker.Data;
using HemSoft.EggIncTracker.Domain;

public static class Program
{
    public static async Task Main(string[] args)
    {
        using var context = new EggIncContext();
        context.Database.EnsureCreated();

        var (player, fullPlayerInfo) = await Api.CallPlayerInfoApi("EI6335140328505344", "King Friday!");
        PlayerManager.SavePlayer(player, fullPlayerInfo, null);
        var (player2, fullPlayerInfo2) = await Api.CallPlayerInfoApi("EI5435770400276480", "King Saturday!");
        PlayerManager.SavePlayer(player2, fullPlayerInfo2, null);
        var (player3, fullPlayerInfo3) = await Api.CallPlayerInfoApi("EI6306349753958400", "King Sunday!");
        PlayerManager.SavePlayer(player3, fullPlayerInfo3, null);
        var (player4, fullPlayerInfo4) = await Api.CallPlayerInfoApi("EI6725967592947712", "King Monday!");
        PlayerManager.SavePlayer(player4, fullPlayerInfo4, null);

        Console.WriteLine(player.ToString());
        Console.WriteLine(player2.ToString());
        Console.WriteLine(player3.ToString());
        Console.WriteLine(player4.ToString());
        Console.WriteLine("\nPress any key to exit.");
        Console.ReadKey();
    }
}
