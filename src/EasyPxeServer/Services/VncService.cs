using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;

namespace EasyPxeServer.Services
{
    /// <summary>
    /// VNC连接服务，用于管理VNC连接状态和操作
    /// </summary>
    public class VncService(ILogger<VncService> logger)
    {
        private readonly Dictionary<string, VncConnection> _connections = new();

        /// <summary>
        /// 创建VNC连接
        /// </summary>
        /// <param name="connectionId">连接ID</param>
        /// <param name="host">主机地址</param>
        /// <param name="port">端口号</param>
        /// <param name="password">密码（可选）</param>
        /// <param name="path">WebSocket路径（可选）</param>
        /// <returns>连接ID</returns>
        public async Task<VncConnection> CreateConnection(string connectionId, string scheme,string host, int port, string vncHost, int vncPort, string password = null, string path = "websockify")
        {
            if (string.IsNullOrEmpty(connectionId)) { connectionId = System.Guid.NewGuid().ToString(); }

            // 构建WebSocket URL
            var wsUrl = $"{scheme}://{host}:{port}/{path}?host={vncHost}&port={vncPort}&scale=true";
            logger.LogInformation("创建VNC连接: {0}", wsUrl);
            // 保存连接信息
            _connections[connectionId] = new VncConnection
            {
                ConnectionId = connectionId,
                Host         = host,
                Port         = port,
                Password     = password,
                WebSocketUrl = wsUrl,
                IsConnected  = false
            };

            return _connections[connectionId];
        }

        public async Task<VncConnection> GetConnection(string connectionId)
        {
            if (_connections.TryGetValue(connectionId, out var connection)) { return connection; }
            else { return null; }
        }

        /// <summary>
        /// 初始化VNC连接
        /// </summary>
        /// <param name="connectionId">连接ID</param>
        /// <param name="dotNetRef">DotNet对象引用</param>
        public async Task InitConnection(string connectionId, DotNetObjectReference<VncSession> dotNetRef)
        {
            if (_connections.TryGetValue(connectionId, out var connection)) { logger.LogInformation("初始化VNC连接: {0}", connectionId); }
            else { logger.LogWarning("未找到连接: {0}", connectionId); }
        }

        /// <summary>
        /// 断开VNC连接
        /// </summary>
        /// <param name="connectionId">连接ID</param>
        public async Task Disconnect(string connectionId)
        {
            if (_connections.ContainsKey(connectionId)) { _connections.Remove(connectionId); }
        }

        /// <summary>
        /// 获取所有活跃的VNC连接
        /// </summary>
        public List<VncConnection> GetActiveConnections() { return [.._connections.Values]; }

        /// <summary>
        /// 检查连接是否存在
        /// </summary>
        public bool ConnectionExists(string connectionId) { return _connections.ContainsKey(connectionId); }
    }

    /// <summary>
    /// VNC连接信息类
    /// </summary>
    public class VncConnection
    {
        public string ConnectionId { get; set; }
        public string Host         { get; set; }
        public int    Port         { get; set; }
        public string Password     { get; set; }
        public string WebSocketUrl { get; set; }
        public bool   IsConnected  { get; set; }
        public string DesktopName  { get; set; }
    }

    /// <summary>
    /// VNC会话类，用于接收JavaScript回调
    /// </summary>
    public class VncSession(VncService vncService, string connectionId)
    {
        private readonly VncService _vncService   = vncService;
        private readonly string     _connectionId = connectionId;

        // 原始回调方法，使用Handle前缀
        [JSInvokable]
        public void HandleConnect()
        {
            try
            {
                // 处理连接成功事件
                Console.WriteLine($"VNC连接成功: {_connectionId}");
            }
            catch (Exception ex) { Console.WriteLine($"处理VNC连接成功事件时出错: {ex.Message}"); }
        }

        [JSInvokable]
        public void HandleDisconnect()
        {
            try
            {
                // 处理断开连接事件
                Console.WriteLine($"VNC连接断开: {_connectionId}");
            }
            catch (Exception ex) { Console.WriteLine($"处理VNC连接断开事件时出错: {ex.Message}"); }
        }

        [JSInvokable]
        public void HandleError(string error)
        {
            try
            {
                // 处理错误事件
                Console.WriteLine($"VNC连接错误: {error}");
            }
            catch (Exception ex) { Console.WriteLine($"处理VNC连接错误事件时出错: {ex.Message}"); }
        }

        [JSInvokable]
        public void HandleBell()
        {
            try
            {
                // 处理铃声事件
                Console.WriteLine($"VNC铃声: {_connectionId}");
            }
            catch (Exception ex) { Console.WriteLine($"处理VNC铃声事件时出错: {ex.Message}"); }
        }

        [JSInvokable]
        public void HandleClipboard(string text)
        {
            try
            {
                // 处理剪贴板事件
                Console.WriteLine($"VNC剪贴板: {text}");
            }
            catch (Exception ex) { Console.WriteLine($"处理VNC剪贴板事件时出错: {ex.Message}"); }
        }

        // 兼容VncClient组件的回调方法，使用On前缀
        [JSInvokable]
        public Task OnConnect()
        {
            try
            {
                Console.WriteLine($"VNC客户端连接成功回调: {_connectionId}");
                // 调用原始处理方法以保持一致性
                HandleConnect();
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理VNC客户端连接成功回调时出错: {ex.Message}");
                return Task.CompletedTask;
            }
        }

        [JSInvokable]
        public Task OnDisconnect()
        {
            try
            {
                Console.WriteLine($"VNC客户端断开连接回调: {_connectionId}");
                // 调用原始处理方法以保持一致性
                HandleDisconnect();
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理VNC客户端断开连接回调时出错: {ex.Message}");
                return Task.CompletedTask;
            }
        }

        [JSInvokable]
        public Task OnError(string errorMsg)
        {
            try
            {
                Console.WriteLine($"VNC客户端错误回调: {errorMsg}");
                // 调用原始处理方法以保持一致性
                HandleError(errorMsg);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理VNC客户端错误回调时出错: {ex.Message}");
                return Task.CompletedTask;
            }
        }
    }
}