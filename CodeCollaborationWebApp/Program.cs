using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using CodeCollaborationWebApp.Hubs;
using CodeCollaborationWebApp.Services;
using Microsoft.Extensions.Logging.AzureAppServices;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Profiler.AspNetCore; // Add this using directive

var builder = WebApplication.CreateBuilder(args);

// Add services for Razor Pages, Controllers, and SignalR
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// Initialize Redis Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
    options.InstanceName = "CodeCollaboration_";
});

// Configure TelemetryConfiguration to include more details
builder.Services.Configure<TelemetryConfiguration>((config) =>
{
    config.TelemetryInitializers.Add(new HttpDependenciesParsingTelemetryInitializer());
});

builder.Logging.AddApplicationInsights(
    configureTelemetryConfiguration: (config) =>
        config.ConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"],
    configureApplicationInsightsLoggerOptions: (options) => { }
);

builder.Services.AddSignalR();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddAzureWebAppDiagnostics();
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddServiceProfiler(); 

// Add the RoomService as a singleton
builder.Services.AddSingleton<IRoomService, RedisRoomService>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapControllers();

// Map the SignalR hub
app.MapHub<CollaborationHub>("/collaborationHub");

app.UseExceptionHandler("/Error");

app.Run();
