using Microsoft.FluentUI.AspNetCore.Components.Utilities;
using Microsoft.JSInterop;

namespace FluentPxeServer.Components.Infrastructure;

public class VncHelper(IJSRuntime js) : JSModule(js, "/js/vnc-helper.js")
{
}