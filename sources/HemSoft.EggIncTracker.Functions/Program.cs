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

        // Register HttpClient with proper timeout
        services.AddHttpClient<FunctionApiService>(client =>
        {
            var apiBaseUrl = context.Configuration["ApiBaseUrl"] ?? "https://localhost:5000/";
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Register the FunctionApiService
        services.AddSingleton<IApiService>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var httpClient = httpClientFactory.CreateClient(nameof(FunctionApiService));
            var logger = sp.GetRequiredService<ILogger<FunctionApiService>>();
            return new FunctionApiService(httpClient, logger);
        });
    })
    .Build();

host.Run();
