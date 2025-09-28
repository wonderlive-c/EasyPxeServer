using MarcusW.VncClient;
using MarcusW.VncClient.Protocol.Implementation;
using MarcusW.VncClient.Protocol.Implementation.Services.Transports;
using ReactiveUI;
using System.Net;
using System.Threading;
using MarcusW.VncClient.Blazor.Extensions;
using MarcusW.VncClient.Rendering;

namespace EasyPxeServer.Services
{
    // 实际的VNC服务类
    public class VncService(IServiceProvider serviceProvider, ILogger<VncService> logger)
    {
        private readonly Dictionary<string, VncSession> _activeSessions = new();

        public event Action<string, VncSession>? SessionCreated;
        public event Action<string>?             SessionClosed;
        private IServiceProvider                 ServiceProvider { get; set; } = serviceProvider;

        private InteractiveAuthenticationHandler InteractiveAuthenticationHandler => serviceProvider.GetRequiredService<InteractiveAuthenticationHandler>();
        public  bool                             IsTightAvailable                 => DefaultImplementation.IsTightAvailable;


        public VncSession? GetSession(string sessionId)
        {
            _activeSessions.TryGetValue(sessionId, out var session);
            return session;
        }

        public async Task<string> CreateSessionAsync(string host, int port, string password = "")
        {
            try
            {
                // 创建会话ID
                var sessionId = Guid.NewGuid().ToString();

                // 创建VNC连接选项

                // 创建VNC客户端
                var client = new VncClient(ServiceProvider.GetRequiredService<ILoggerFactory>());

                // 建立连接
                var connection = await client.ConnectAsync(new ConnectParameters
                {
                    TransportParameters = new TcpTransportParameters
                    {
                        Host = host,
                        Port = port
                    },
                    AuthenticationHandler = InteractiveAuthenticationHandler
                });

                // 创建会话
                var session = new VncSession
                {
                    SessionId   = sessionId,
                    Connection  = connection,
                    Host        = host,
                    Port        = port,
                    IsConnected = connection.ConnectionState is ConnectionState.Connected
                };

                // 存储会话
                _activeSessions[sessionId] = session;

                // 触发会话创建事件
                SessionCreated?.Invoke(sessionId, session);

                return sessionId;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to create VNC session: " + ex.Message);
                throw;
            }
        }

        public async Task CloseSessionAsync(string sessionId)
        {
            if (_activeSessions.TryGetValue(sessionId, out var session))
                try
                {
                    if (session.Connection != null
                     && session.IsConnected)
                    {
                        await session.Connection.CloseAsync();
                        session.IsConnected = false;
                    }
                }
                catch (Exception ex) { Console.WriteLine("Error closing VNC connection: " + ex.Message); }
                finally
                {
                    _activeSessions.Remove(sessionId);
                    SessionClosed?.Invoke(sessionId);
                }
        }

        public List<VncSessionInfo> GetActiveSessions()
        {
            return _activeSessions.Values.Select(s => new VncSessionInfo
            {
                SessionId   = s.SessionId,
                Host        = s.Host,
                Port        = s.Port,
                IsConnected = s.IsConnected,
                ConnectedAt = s.ConnectedAt
            }).ToList();
        }

        /*
        // 发送键盘事件到VNC服务器
        public async Task SendKeyEventAsync(string sessionId, ushort keyCode, bool isPressed)
        {
            if (_activeSessions.TryGetValue(sessionId, out var session)
             && session.Connection != null
             && session.IsConnected) await session.Connection.SendMessageAsync<>(keyCode, isPressed);
        }

        // 发送鼠标事件到VNC服务器
        public async Task SendMouseEventAsync(string sessionId, int x, int y, MouseButton buttons)
        {
            if (_activeSessions.TryGetValue(sessionId, out var session)
             && session.Connection != null
             && session.IsConnected) await session.Connection.SendPointerEventAsync(x, y, buttons);
        }*/
    }

    public class VncSession
    {
        public string         SessionId   { get; set; } = string.Empty;
        public RfbConnection? Connection  { get; set; }
        public string         Host        { get; set; } = string.Empty;
        public int            Port        { get; set; }
        public bool           IsConnected { get; set; }
        public DateTime       ConnectedAt { get; set; } = DateTime.Now;
    }

    public class VncSessionInfo
    {
        public string   SessionId   { get; set; } = string.Empty;
        public string   Host        { get; set; } = string.Empty;
        public int      Port        { get; set; }
        public bool     IsConnected { get; set; }
        public DateTime ConnectedAt { get; set; }
    }

    [Flags]
    public enum MouseButton
    {
        None      = 0,
        Left      = 1,
        Middle    = 2,
        Right     = 4,
        WheelUp   = 8,
        WheelDown = 16
    }
}