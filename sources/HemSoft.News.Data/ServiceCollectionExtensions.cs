using HemSoft.News.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HemSoft.News.Data;

/// <summary>
/// Extension methods for IServiceCollection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the news data services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="connectionString">The connection string</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddNewsDataServices(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<NewsDbContext>(options =>
            options.UseSqlite(connectionString));

        services.AddScoped<INewsRepository, NewsRepository>();

        return services;
    }
}
