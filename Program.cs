namespace HemSoft.EggIncTracker;

public class Program
{
    public static async Task Main(string[] args)
    {
        var playerInfo = await Api.CallApi("https://eggincdatacollection.azurewebsites.net/api/formulae/all?eid=EI6335140328505344");
        Console.WriteLine(playerInfo.ToString());
        Console.WriteLine("\nPress any key to exit.");
        Console.ReadKey();
    }
}
