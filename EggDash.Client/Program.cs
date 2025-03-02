using EggDash.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

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

builder.Services.AddMudServices();

await builder.Build().RunAsync();