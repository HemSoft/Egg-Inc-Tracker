using HemSoft.News.Data;
using HemSoft.News.Data.Repositories;
using HemSoft.AI;
using HemSoft.News.Tools;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IO;
using HemSoft.News.Functions.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
        config.AddEnvironmentVariables();

        if (context.HostingEnvironment.IsDevelopment())
        {
            config.AddUserSecrets<Program>();
        }
    })
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();

        // Register HttpClient factory
        services.AddHttpClient<IFirecrawlService, FirecrawlService>();

        // Register services
        services.AddSingleton<IFirecrawlService, FirecrawlService>();
        services.AddSingleton<IAIContentParsingService, AIContentParsingService>();

        // Register AI services
        services.AddAIServices(context.Configuration);
        services.AddNewsToolsServices(context.Configuration);

        // Configure SQLite database
        // Get the solution root directory by finding the .sln file
        var contentRootPath = context.HostingEnvironment.ContentRootPath;
        Console.WriteLine($"ContentRootPath: {contentRootPath}");

        // Find the solution root by looking for the .sln file
        var directory = new DirectoryInfo(contentRootPath);
        string solutionRoot = contentRootPath;

        while (directory != null && directory.Parent != null)
        {
            directory = directory.Parent;
            if (directory.GetFiles("*.sln").Length > 0)
            {
                solutionRoot = directory.FullName;
                break;
            }
        }

        Console.WriteLine($"SolutionRoot: {solutionRoot}");

        var databasePath = Path.Combine(solutionRoot, "resources", "news-database", "news.db");
        Console.WriteLine($"DatabasePath: {databasePath}");

        // Ensure the directory exists
        var databaseDir = Path.GetDirectoryName(databasePath) ?? string.Empty;
        Console.WriteLine($"Creating directory: {databaseDir}");
        Directory.CreateDirectory(databaseDir);

        var connectionString = $"Data Source={databasePath}";
        Console.WriteLine($"ConnectionString: {connectionString}");
        services.AddNewsDataServices(connectionString);
    })
    .Build();

// Ensure the database is created
using (var scope = host.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<NewsDbContext>();
    dbContext.Database.EnsureCreated();
}

host.Run();

// This class is used for UserSecrets configuration
public partial class Program { }
