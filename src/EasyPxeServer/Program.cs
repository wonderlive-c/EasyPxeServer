using MarcusW.VncClient.Blazor.Extensions;
using Microsoft.AspNetCore.Mvc;
using EasyPxeServer.Services;
using Serilog;
using System.Security.Principal;
using System.Reflection;
using System.IO;
using Microsoft.Extensions.FileProviders;


if (Environment.OSVersion.Platform is PlatformID.Win32Windows or PlatformID.Win32NT or PlatformID.Win32S or PlatformID.WinCE)
{
    // 检查是否具有管理员权限
    var isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
    if (!isAdmin)
    {
        Console.WriteLine("错误: 程序需要管理员权限才能运行。");
        Console.WriteLine("请右键点击程序并选择'以管理员身份运行'。");
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
        return;
    }

    // 检查是否在C盘运行
    var currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    if (currentDirectory != null
     && currentDirectory.StartsWith("C:\\", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("错误: 程序禁止在C盘运行。");
        Console.WriteLine("请将程序移至其他磁盘后再运行。");
        Console.WriteLine("按任意键退出...");
        Console.ReadKey();
        return;
    }
}


Log.Logger = new LoggerConfiguration().WriteTo.File("log.txt").WriteTo.Console().CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile(@"appsettings.log.json");
builder.Logging.AddSerilog();
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
builder.Services.AddSingleton<InteractiveAuthenticationHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// 不要硬编码环境名称，让ASP.NET Core根据实际环境自动确定

app.UseHttpsRedirection();

app.UseStaticFiles();

// 配置对node_modules目录的静态文件访问支持
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "node_modules")),
    RequestPath = "/node_modules"
});

// 配置WebSocket支持，允许所有请求路径使用WebSocket
app.UseWebSockets();

app.UseRouting();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();