using Microsoft.Extensions.FileProviders;
using Microsoft.FluentUI.AspNetCore.Components;
using Serilog;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using PxeServices;


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
// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();
builder.Services.AddSingleton<DhcpService>();
builder.Services.AddSingleton<TftpService>();
builder.Services.AddSingleton<PxeServerService>();
builder.Services.AddSingleton<VncService>(); 
builder.Services.AddFluentUIComponents();


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

// 配置WebSocket支持，允许所有请求路径使用WebSocket
app.UseWebSockets();

app.UseRouting();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();