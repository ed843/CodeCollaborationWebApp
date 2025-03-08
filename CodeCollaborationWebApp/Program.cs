using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;
using CodeCollaborationWebApp.Hubs;
using CodeCollaborationWebApp.Services;
using Microsoft.Extensions.Logging.AzureAppServices;

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

builder.Services.AddSignalR();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddAzureWebAppDiagnostics();

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
