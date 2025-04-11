namespace HemSoft.AI;

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering AI services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds AI services to the service collection
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddAIServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Register the ChatClient as a singleton
        services.AddSingleton(provider => new ChatClient(configuration));

        return services;
    }

    /// <summary>
    /// Adds a specialized ChatClient for content parsing
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddContentParsingClient(this IServiceCollection services, IConfiguration configuration)
    {
        // Register a specialized ChatClient for content parsing
        services.AddSingleton(provider => ChatClientFactory.CreateForContentParsing(configuration));

        return services;
    }

    /// <summary>
    /// Adds a specialized ChatClient for NuGet parsing
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddNuGetParsingClient(this IServiceCollection services, IConfiguration configuration)
    {
        // Register a specialized ChatClient for NuGet parsing
        services.AddSingleton(provider => ChatClientFactory.CreateForNuGetParsing(configuration));

        return services;
    }
}
