using EggDash.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using HemSoft.EggIncTracker.Domain;
using Microsoft.Extensions.Logging;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Register the DashboardState service
builder.Services.AddSingleton<DashboardState>();

// Register HttpClient with proper BaseAddress
builder.Services.AddHttpClient<ApiService>(client =>
{
    // Use the application URL from launchSettings.json or configuration
    var baseAddress = builder.Configuration["ApplicationUrl"] ?? "http://localhost:7117"; // Default for local dev
    client.BaseAddress = new Uri(baseAddress.EndsWith("/") ? baseAddress : baseAddress + "/");
});

// Register ApiService
builder.Services.AddScoped<ApiService>();
builder.Services.AddScoped<EggDash.Client.Services.IApiService>(sp => sp.GetRequiredService<ApiService>());

// Register the new PlayerDataService
builder.Services.AddScoped<PlayerDataService>();

builder.Services.AddMudServices();

var app = builder.Build();

// Register ApiService with ServiceLocator
var apiService = app.Services.GetRequiredService<ApiService>();
ServiceLocator.RegisterService<HemSoft.EggIncTracker.Domain.IApiService>(apiService);

// Register Logger with ServiceLocator
var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger("GlobalLogger");
ServiceLocator.RegisterService<ILogger>(logger);

await app.RunAsync();
