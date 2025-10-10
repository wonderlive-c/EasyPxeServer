using Microsoft.Extensions.Logging;

namespace PxeServices
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
        public async Task<VncConnection> CreateConnection(string connectionId, string scheme, string host, int port, string vncHost, int vncPort, string password = null, string path = "websockify")
        {
            if (string.IsNullOrEmpty(connectionId)) { connectionId = Guid.NewGuid().ToString(); }

            if (ConnectionExists(connectionId)) { return _connections[connectionId]; }

            // 构建WebSocket URL
            var wsUrl = $"{scheme}://{host}:{port}/{path}?host={vncHost}&port={vncPort}&scale=true";
            logger.LogInformation("创建VNC连接: {wsUrl},{connectionId}", wsUrl, connectionId);
            // 保存连接信息
            _connections[connectionId] = new VncConnection
            {
                ConnectionId = connectionId,
                Host         = vncHost,
                Port         = vncPort,
                Password     = password,
                WebSocketUrl = wsUrl,
                IsConnected  = false
            };

            return _connections[connectionId];
        }

        public async Task<VncConnection?> GetConnection(string connectionId)
        {
            logger.LogInformation("获取VNC连接: {connectionId}", connectionId);
            return _connections.TryGetValue(connectionId, out var connection) ? connection : null;
        }

        /// <summary>
        /// 断开VNC连接
        /// </summary>
        /// <param name="connectionId">连接ID</param>
        public async Task RemoveConnection(string connectionId)
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
        public string ConnectionName { get; set; }
        public string ConnectionId   { get; set; }
        public string Host           { get; set; }
        public int    Port           { get; set; }
        public string Password       { get; set; }
        public string WebSocketUrl   { get; set; }
        public bool   IsConnected    { get; set; }
        public string DesktopName    { get; set; }
    }
}