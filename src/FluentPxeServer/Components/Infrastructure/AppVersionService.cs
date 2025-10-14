using System.Reflection;

namespace FluentPxeServer.Components.Infrastructure;

internal class AppVersionService : IAppVersionService
{
    public string Version => GetVersionFromAssembly();

    public string CompanyName => GetCompanyName();

    public string Copyright => GetCopyright();

    private static string GetVersionFromAssembly()
    {
        string strVersion       = default!;
        var    versionAttribute = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (versionAttribute != null)
        {
            var version   = versionAttribute.InformationalVersion;
            var plusIndex = version.IndexOf('+');
            if (plusIndex     >= 0
             && plusIndex + 9 < version.Length) { strVersion = version[..(plusIndex + 9)]; }
            else { strVersion                                = version; }
        }

        return strVersion;
    }

    private static string GetCompanyName() { return Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? "SCT"; }

    private static string GetCopyright() { return Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "\u00a9 2025, Wonder-Live, All rights reserved"; }
}