using EggDash.Client.Services;
using MudBlazor.Services;
using EggDash.Components;
using HemSoft.EggIncTracker.Data;
using Microsoft.AspNetCore.Components.Server;

var builder = WebApplication.CreateBuilder(args);

// Add MudBlazor services
builder.Services.AddMudServices();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Register DbContext
builder.Services.AddDbContext<EggIncContext>();

builder.Services.AddSingleton<DashboardState>();

// Register HttpClient
builder.Services.AddHttpClient<ApiService>();

// Register ApiService
builder.Services.AddScoped<ApiService>();

// Configure CircuitOptions
builder.Services.Configure<CircuitOptions>(options =>
{
    options.DetailedErrors = true;
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

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(EggDash.Client._Imports).Assembly);

app.Run();