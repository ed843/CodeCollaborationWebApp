using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Newtonsoft.Json;
using CodeCollaborationWebApp.Hubs;
using CodeCollaborationWebApp.Services;

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

// Add the RoomService as a singleton
builder.Services.AddSingleton<IRoomService, RoomService>();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();
app.MapControllers();

// Map the SignalR hub
app.MapHub<CollaborationHub>("/collaborationHub");

app.Run();
