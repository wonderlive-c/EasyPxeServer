using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PxeServices.Entities.VncClient;

namespace PxeServices
{
    /// <summary>
    /// VNC连接服务，用于管理VNC连接状态和操作
    /// </summary>
    public class VncService(ILogger<VncService> logger, IServiceProvider serviceProvider)
    {
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

            if (await ConnectionExists(connectionId)) { return await GetConnection(connectionId); }

            // 构建WebSocket URL
            var wsUrl = $"{scheme}://{host}:{port}/{path}?host={vncHost}&port={vncPort}&scale=true";
            logger.LogInformation("创建VNC连接: {wsUrl},{connectionId}", wsUrl, connectionId);
            // 保存连接信息
            var newConnection = new VncConnection
            {
                Id             = Guid.TryParse(connectionId, out var id) ? id : Guid.NewGuid(),
                ConnectionName = "Connection-" + vncHost + "-" + connectionId,
                ConnectionId   = connectionId,
                Host           = vncHost,
                Port           = vncPort,
                Password       = password,
                WebSocketUrl   = wsUrl,
                IsConnected    = false
            };

            using var scope      = serviceProvider.CreateScope();
            var       repository = scope.ServiceProvider.GetRequiredService<IVncConnectionRepository>();
            await repository.AddAsync(newConnection);

            return newConnection;
        }
        /// <summary>
        /// 获取指定ID的VNC连接
        /// </summary>
        /// <param name="connectionId"></param>
        /// <returns></returns>
        public async Task<VncConnection?> GetConnection(string connectionId)
        {
            if (Guid.TryParse(connectionId, out var guid))
            {
                using var scope      = serviceProvider.CreateScope();
                var       repository = scope.ServiceProvider.GetRequiredService<IVncConnectionRepository>();
                return await repository.GetAsync(guid);
            }

            return default;
        }

        /// <summary>
        /// 断开VNC连接
        /// </summary>
        /// <param name="connectionId">连接ID</param>
        public async Task RemoveConnection(string connectionId)
        {
            if (Guid.TryParse(connectionId, out var guid))
            {
                using var scope      = serviceProvider.CreateScope();
                var       repository = scope.ServiceProvider.GetRequiredService<IVncConnectionRepository>();
                await repository.DeleteAsync(guid);
            }
        }

        /// <summary>
        /// 获取所有活跃的VNC连接
        /// </summary>
        public List<VncConnection> GetActiveConnections()
        {
            using var scope      = serviceProvider.CreateScope();
            var       repository = scope.ServiceProvider.GetRequiredService<IVncConnectionRepository>();
            return (repository.GetList()).ToList();
        }

        /// <summary>
        /// 检查连接是否存在
        /// </summary>
        private async Task<bool> ConnectionExists(string connectionId)
        {
            if (Guid.TryParse(connectionId, out var guid))
            {
                using var scope      = serviceProvider.CreateScope();
                var       repository = scope.ServiceProvider.GetRequiredService<IVncConnectionRepository>();
                return await repository.ExistsAsync(guid);
            }

            return false;
        }
    }
}