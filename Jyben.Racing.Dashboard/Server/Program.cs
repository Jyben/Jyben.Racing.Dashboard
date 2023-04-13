using Jyben.Racing.Dashboard.Server.Hubs;
using Jyben.Racing.Dashboard.Server.Services;
using Jyben.Racing.Dashboard.Server.Services.Impl;
using Jyben.Racing.Dashboard.Shared.Models;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});
builder.Services.AddSignalR();

builder.Services.Configure<RacingDatabaseSettings>(
    builder.Configuration.GetSection("RacingDatabase"));

builder.Services.AddSingleton<ITelemetrieService, TelemetrieService>();
builder.Services.AddSingleton<ICircuitsService, CircuitsService>();
builder.Services.AddSingleton<IPiloteService, PiloteService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();


app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.UseResponseCompression();

app.MapHub<TelemetryHub>("signalr/telemetry");

app.Run();

