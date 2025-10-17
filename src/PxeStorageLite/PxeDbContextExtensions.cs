using System.Text.Json.Serialization.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PxeServices.Entities.Dhcp;
using PxeServices.Entities.Settings;
using PxeServices.Entities.VncClient;

namespace PxeStorageLite;

public static class PxeDbContextExtensions
{
    public static IServiceCollection ConfigureDhcpUserStorage(this IServiceCollection services)
    {
        services.AddScoped<IDhcpUserRepository, DhcpUserRepository>();
        services.AddScoped<IVncConnectionRepository, VncConnectionRepository>();
        services.AddScoped<IObjectSettingRepository, ObjectSettingRepository>();
        services.AddDbContext<PxeDbContext>(b=>
        {
            b.UseSqlite("Data Source=DhcpUsers.db");
        });
        services.AddDbContextFactory<PxeDbContext>(lifetime:ServiceLifetime.Scoped);

        return services;
    }
}