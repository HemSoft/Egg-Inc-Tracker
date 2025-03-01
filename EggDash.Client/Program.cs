using EggDash.Client.Services;

using HemSoft.EggIncTracker.Data;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.EntityFrameworkCore;

using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Register the DashboardState service
builder.Services.AddSingleton<DashboardState>();

// Register the ApiService
builder.Services.AddHttpClient<ApiService>(client => 
{
    client.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);
});

builder.Services.AddMudServices();

await builder.Build().RunAsync();