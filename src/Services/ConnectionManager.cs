#region Copyright
// ===============================================================================
//   Project Name        :    PxeBlazorServer
//   Project Description :
//   ===============================================================================
//   File Name           :    ConnectionManager.cs
//   File Version        :    v1.0.0.0
//   File Description    :
//   Author              :    wonderlive
//   Create Time         :    2025-9-26 17:20
//   Update Time         :    2025-9-26 17:20
// 
//   ===============================================================================
//      _____  _____  / /_
//     / ___/ / ___/ / __/
//    (__  ) / /__  / /_
//   /____/  \___/  \__/     Copyright © 2025 SCT Tech. Co., Ltd. All rights reserved.
//   ===============================================================================
#endregion

using MarcusW.VncClient;
using MarcusW.VncClient.Blazor.Adapters.Logging;
using MarcusW.VncClient.Rendering;

namespace PxeBlazorServer.Services
{
    public class ConnectionManager
    {
        private readonly InteractiveAuthenticationHandler _interactiveAuthenticationHandler;
        private readonly MarcusW.VncClient.VncClient      _vncClient;

        public ConnectionManager(InteractiveAuthenticationHandler interactiveAuthenticationHandler, ILoggerFactory? loggerFactory = null)
        {
            _interactiveAuthenticationHandler = interactiveAuthenticationHandler ?? throw new ArgumentNullException(nameof(interactiveAuthenticationHandler));

            // Create and populate default logger factory for logging to Blazor logging sinks
            if (loggerFactory == null)
            {
                var factory = new LoggerFactory();
                factory.AddProvider(new BlazorLoggerProvider());
                _vncClient = new MarcusW.VncClient.VncClient(factory);
            }
            else
            {
                _vncClient = new MarcusW.VncClient.VncClient(loggerFactory);
            }
        }

        public Task<RfbConnection> ConnectAsync(ConnectParameters parameters, CancellationToken cancellationToken = default)
        {
            parameters.AuthenticationHandler = _interactiveAuthenticationHandler;

            // Enable rectangle-based updates for better performance
            parameters.RenderFlags |= RenderFlags.UpdateByRectangle;

            // Uncomment for debugging/visualization purposes
            //parameters.RenderFlags |= RenderFlags.VisualizeRectangles;

            return _vncClient.ConnectAsync(parameters, cancellationToken);
        }
    }
}