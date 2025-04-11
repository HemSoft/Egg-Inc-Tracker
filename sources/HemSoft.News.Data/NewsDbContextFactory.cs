using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;
using System.IO;

namespace HemSoft.News.Data;

/// <summary>
/// Factory for creating NewsDbContext instances
/// </summary>
public class NewsDbContextFactory : IDesignTimeDbContextFactory<NewsDbContext>
{
    /// <summary>
    /// Creates a new instance of a NewsDbContext
    /// </summary>
    /// <param name="args">Arguments provided by the design-time service</param>
    /// <returns>A new instance of a NewsDbContext</returns>
    public NewsDbContext CreateDbContext(string[] args)
    {
        // Get the solution root directory
        var currentDirectory = Directory.GetCurrentDirectory();
        Console.WriteLine($"CurrentDirectory: {currentDirectory}");

        var solutionRoot = FindSolutionRoot(currentDirectory);
        Console.WriteLine($"SolutionRoot: {solutionRoot}");

        var databasePath = Path.Combine(solutionRoot, "resources", "news-database", "news.db");
        Console.WriteLine($"DatabasePath: {databasePath}");

        // Ensure the directory exists
        var databaseDir = Path.GetDirectoryName(databasePath) ?? string.Empty;
        Console.WriteLine($"Creating directory: {databaseDir}");
        Directory.CreateDirectory(databaseDir);

        var connectionString = $"Data Source={databasePath}";
        Console.WriteLine($"ConnectionString: {connectionString}");

        var optionsBuilder = new DbContextOptionsBuilder<NewsDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        return new NewsDbContext(optionsBuilder.Options);
    }

    /// <summary>
    /// Finds the solution root directory by looking for the .sln file
    /// </summary>
    /// <param name="startDirectory">The directory to start searching from</param>
    /// <returns>The solution root directory, or the current directory if not found</returns>
    private string FindSolutionRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        string solutionRoot = startDirectory;

        while (directory != null && directory.Parent != null)
        {
            if (directory.GetFiles("*.sln").Length > 0)
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }

        // If we can't find the solution root, try one more approach
        directory = new DirectoryInfo(startDirectory);
        while (directory != null && directory.Parent != null)
        {
            directory = directory.Parent;
            if (directory.GetFiles("*.sln").Length > 0)
            {
                return directory.FullName;
            }
        }

        // If we still can't find it, return the original directory
        return startDirectory;
    }

    /// <summary>
    /// Registers the NewsDbContext and related services with the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">The connection string</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddNewsDbContext(IServiceCollection services, string connectionString)
    {
        services.AddDbContext<NewsDbContext>(options =>
            options.UseSqlite(connectionString));

        return services;
    }
}
