using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CodeCollaborationWebApp;

var builder = WebApplication.CreateBuilder(args);

// Add services for Razor Pages, Controllers, and SignalR
builder.Services.AddRazorPages();
builder.Services.AddControllers();
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
