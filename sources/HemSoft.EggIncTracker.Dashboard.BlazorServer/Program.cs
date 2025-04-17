// Removed client services using
using MudBlazor.Services;
using HemSoft.EggIncTracker.Dashboard.BlazorServer.Components;
using HemSoft.EggIncTracker.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Server;
using HemSoft.EggIncTracker.Domain; // Add Domain using for static managers
using HemSoft.EggIncTracker.Dashboard.BlazorServer.Services; // Add Server services using
using Microsoft.EntityFrameworkCore; // Add EF Core using

var builder = WebApplication.CreateBuilder(args);

// Add MudBlazor services with custom configuration
builder.Services.AddMudServices(config =>
{
    config.PopoverOptions.ThrowOnDuplicateProvider = false; // Prevent errors with duplicate providers
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(); // Removed AddInteractiveWebAssemblyComponents()

// Register DbContext with explicit connection string from configuration
builder.Services.AddDbContext<EggIncContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=localhost;Initial Catalog=db-egginc;Integrated Security=True;Encrypt=False;Trust Server Certificate=True;Connection Timeout=30"));

// Register Dispatcher service for UI thread synchronization
builder.Services.AddScoped(sp => Dispatcher.CreateDefault());

// Register DashboardState as Scoped for Blazor Server
builder.Services.AddScoped<DashboardState>();

// Removed HttpClient, ApiService, IApiService registrations as we use direct domain access

// Register services (Scoped is appropriate)
builder.Services.AddScoped<PlayerDataService>();
builder.Services.AddScoped<PlayerCardService>();

// NOTE: Static Domain Managers (PlayerManager, etc.) are not registered via DI
// They instantiate their own DbContext, which is not ideal but kept for now.

// Configure CircuitOptions
builder.Services.Configure<CircuitOptions>(options =>
{
    options.DetailedErrors = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.UseWebAssemblyDebugging(); // Removed as we are no longer using WebAssembly
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
    .AddInteractiveServerRenderMode(); // Removed AddInteractiveWebAssemblyRenderMode and AddAdditionalAssemblies

app.Run();
