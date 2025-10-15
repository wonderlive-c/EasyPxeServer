using FluentPxeServer.Components;
using FluentPxeServer.Components.Infrastructure;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using PxeServices;
using PxeStorageLite;
using Serilog;

namespace FluentPxeServer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration().WriteTo.File("log.txt").WriteTo.Console().CreateBootstrapLogger();
            var builder = WebApplication.CreateBuilder(args);

            StaticWebAssetsLoader.UseStaticWebAssets(builder.Environment, builder.Configuration);

            // Add services to the container.
            builder.Services.AddRazorComponents().AddInteractiveServerComponents();
            builder.Services.AddRazorPages();
            builder.Services.AddHttpClient();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddFluentUIComponents();
            builder.Services.AddFluentUIServerServices();
            builder.Logging.AddSerilog();
            builder.Services.AddControllers();
            builder.Services.AddSingleton<DhcpService>();
            builder.Services.AddSingleton<TftpService>();
            builder.Services.AddSingleton<PxeServerService>();
            builder.Services.AddSingleton<VncService>();
            builder.Services.ConfigureDhcpUserStorage();

            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                // 从作用域中获取服务提供程序
                var scopedProvider = scope.ServiceProvider;

                // 现在可以安全地解析DbContext了

                try
                {
                    await using var db = scopedProvider.GetRequiredService<PxeDbContext>();

                    var created = db.Database.EnsureCreatedAsync();
                    Log.Information("Database created: {created}", created.IsCompletedSuccessfully);
                }
                catch (Exception e) { Log.Error(e, "Error creating database"); }
            }

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

        #if NET8_0
app.UseStaticFiles();
        #else
            app.MapStaticAssets();
        #endif
            app.UseAntiforgery();
            // 配置WebSocket支持，允许所有请求路径使用WebSocket
            app.UseWebSockets();
            //app.UseRouting();
            app.MapControllers();
            app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}