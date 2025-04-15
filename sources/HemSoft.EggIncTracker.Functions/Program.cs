using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
using UpdatePlayerFunction;
using HemSoft.EggIncTracker.Domain;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // TODO: Refactor Functions project to use Domain Managers directly instead of ApiService
        // Commenting out ApiService registration for now to allow build

        // Register HttpClient with proper timeout - Keep this for Discord webhook
        // services.AddHttpClient<FunctionApiService>(client => // Commented out specific type
        services.AddHttpClient("Default", client => // Register named or default client
        {
            var apiBaseUrl = context.Configuration["ApiBaseUrl"] ?? "https://localhost:5000/"; // This URL might not be needed anymore
            // client.BaseAddress = new Uri(apiBaseUrl); // Base address might not be needed if only calling Discord
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Register the FunctionApiService - Commented out
        /*
        services.AddSingleton<IApiService>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(FunctionApiService)); // This would fail
            var logger = sp.GetRequiredService<ILogger<FunctionApiService>>(); // This would fail
            // return new FunctionApiService(httpClient, logger); // This would fail
            throw new NotImplementedException("FunctionApiService registration needs refactoring."); // Added throw
        });
        */
        // Need a basic HttpClient registration for other potential uses (like Discord webhook) - Already added above
        // services.AddHttpClient();
    })
    .Build();

host.Run();
