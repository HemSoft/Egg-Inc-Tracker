namespace HemSoft.News.Tools;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering News Tools services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds News Tools services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddNewsToolsServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register the MCPToolHandler as a singleton
        services.AddSingleton<MCPToolHandler>();

        // Register the NewsContentParser as a singleton
        services.AddSingleton<NewsContentParser>();

        return services;
    }
}
