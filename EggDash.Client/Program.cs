using EggDash.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

using Microsoft.EntityFrameworkCore;

using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Register the DashboardState service
builder.Services.AddSingleton<DashboardState>();

// Configure logging
builder.Logging.SetMinimumLevel(LogLevel.Information);

// Get the API URL from configuration or use a default
var apiBaseUrl = builder.Configuration.GetValue<string>("ApiBaseUrl") ?? 
                 "https://localhost:51412"; // Updated with correct port

// Register HttpClient with the correct base URL
builder.Services.AddScoped(sp => new HttpClient { 
    BaseAddress = new Uri(apiBaseUrl) 
});

// Register the PlayerApiClient
builder.Services.AddScoped<PlayerApiClient>();

builder.Services.AddMudServices();

await builder.Build().RunAsync();