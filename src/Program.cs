using MarcusW.VncClient.Blazor.Extensions;
using Microsoft.AspNetCore.Mvc;
using EasyPxeServer.Services;
using Serilog;

Log.Logger = new LoggerConfiguration().WriteTo.File("log.txt").WriteTo.Console().CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile(@"appsettings.log.json");
builder.Host.UseSerilog();
// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();
builder.Services.AddSingleton<DHCPService>();
builder.Services.AddSingleton<TFTPService>();
builder.Services.AddSingleton<VncService>();
builder.Services.AddSingleton<PxeServerService>();
// Add VNC client services
builder.Services.AddVncClientServices(options =>
{
    //options.EnableDirtyRectangleRendering = true;
    //options.MaxDirtyRectangles = 50;
    //options.EnableFramebufferCaching = true;
    //options.DefaultCanvasSize = new Size(800, 600);
    //options.DefaultDpi = 96.0;
});

// Add application-specific services
builder.Services.AddScoped<ConnectionManager>();
builder.Services.AddScoped<InteractiveAuthenticationHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();