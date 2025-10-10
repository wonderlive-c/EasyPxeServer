using FluentPxeServer.Components;
using FluentPxeServer.Components.Infrastructure;
using Microsoft.AspNetCore.Hosting.StaticWebAssets;
using Microsoft.FluentUI.AspNetCore.Components;
using Serilog;

namespace FluentPxeServer
{
    public class Program
    {
        public static void Main(string[] args)
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

            var app = builder.Build();

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
            
            app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}