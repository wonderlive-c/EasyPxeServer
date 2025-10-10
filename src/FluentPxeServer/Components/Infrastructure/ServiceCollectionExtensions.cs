// ------------------------------------------------------------------------
// This file is licensed to you under the MIT License.
// ------------------------------------------------------------------------

using FluentPxeServer.Components.Cookies;

namespace FluentPxeServer.Components.Infrastructure;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Add common client services required by the Fluent UI Web Components for Blazor library
    /// </summary>
    /// <param name="services">Service collection</param>
    public static IServiceCollection AddFluentUIClientServices(this IServiceCollection services)
    {
        services.AddScoped<CacheStorageAccessor>();
        services.AddScoped<CookieConsentService>();

        return services;
    }

    /// <summary>
    /// Add common server services required by the Fluent UI Web Components for Blazor library
    /// </summary>
    /// <param name="services">Service collection</param>
    public static IServiceCollection AddFluentUIServerServices(this IServiceCollection services)
    {
        services.AddScoped<CacheStorageAccessor>();
        services.AddScoped<CookieConsentService>();

        return services;
    }
}
