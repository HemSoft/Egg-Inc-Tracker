using EggDash.Client.Services;
using MudBlazor.Services;
using EggDash.Components;
using HemSoft.EggIncTracker.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add MudBlazor services
builder.Services.AddMudServices();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Add Controllers with API Endpoints
builder.Services.AddControllers();

// Register DbContext
builder.Services.AddDbContext<EggIncContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=localhost;Initial Catalog=db-egginc;Integrated Security=True;Encrypt=False;Trust Server Certificate=True;Connection Timeout=30",
        sqlServerOptions =>
        {
            sqlServerOptions.CommandTimeout(60); // Sets timeout for commands to 60 seconds
        });
});

// Register HttpClient for server-side
builder.Services.AddHttpClient();

// Configure API URL - get from appsettings.json or use a default
var apiBaseUrl = builder.Configuration.GetValue<string>("ApiBaseUrl") ?? 
                 builder.Configuration.GetValue<string>("ApiSettings:BaseUrl") ?? 
                 "https://localhost:51412"; // Updated with correct port

builder.Services.AddSingleton<DashboardState>();

// Register PlayerApiClient with the correct API URL
builder.Services.AddTransient<PlayerApiClient>(sp => {
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();
    httpClient.BaseAddress = new Uri(apiBaseUrl);
    return new PlayerApiClient(httpClient, sp.GetService<ILogger<PlayerApiClient>>());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Map controllers for API endpoints
app.MapControllers();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(EggDash.Client._Imports).Assembly);

app.Run();
