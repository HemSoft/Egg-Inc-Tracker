using EggDash.Client.Services;
using MudBlazor.Services;
using EggDash.Components;
using HemSoft.EggIncTracker.Data;
using Microsoft.AspNetCore.Components.Server; // Keep this using statement

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

// Register HttpClient with extended timeout
builder.Services.AddHttpClient<ApiService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30); // Increase timeout to 30 seconds
});

// Register ApiService
builder.Services.AddScoped<ApiService>();
builder.Services.AddScoped<EggDash.Client.Services.IApiService>(sp => sp.GetRequiredService<ApiService>());

// Register the new PlayerDataService for server-side rendering
builder.Services.AddScoped<PlayerDataService>();

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

app.UseStaticFiles(); // Ensure static files middleware is present
app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(EggDash.Client._Imports).Assembly);

app.Run();
