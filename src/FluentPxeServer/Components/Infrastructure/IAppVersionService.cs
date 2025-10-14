namespace FluentPxeServer.Components.Infrastructure;

public interface IAppVersionService
{
    string Version     { get; }
    string CompanyName { get; }
    string Copyright   { get; }
}