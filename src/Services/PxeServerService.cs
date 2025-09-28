using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace PxeBlazorServer.Services
{
    public class PxeServerService(DHCPService dhcpService, TFTPService tftpService,ILogger<PxeServerService> logger) : IHostedService
    {
        private bool _isRunning = false;

        public event Action? StatusChanged;

        public bool IsRunning => _isRunning;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_isRunning)
                return;

            try
            {
                await dhcpService.StartAsync(cancellationToken);
                await tftpService.StartAsync(cancellationToken);
                _isRunning = true;
                StatusChanged?.Invoke();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to start PXE server: ");
                throw;
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (!_isRunning)
                return;

            await dhcpService.StopAsync(cancellationToken);
            await tftpService.StopAsync(cancellationToken);
            _isRunning = false;
            StatusChanged?.Invoke();
        }

        public async Task RestartAsync(CancellationToken cancellationToken)
        {
            await StopAsync(cancellationToken);
            await StartAsync(cancellationToken);
        }

        // 获取网络接口信息
        public List<NetworkInterfaceInfo> GetNetworkInterfaces()
        {
            var interfaces        = new List<NetworkInterfaceInfo>();
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            foreach (var ni in networkInterfaces)
            {
                var ipProperties = ni.GetIPProperties();
                var ipv4Address  = ipProperties.UnicastAddresses.FirstOrDefault(a => a.Address.AddressFamily == AddressFamily.InterNetwork);

                if (ipv4Address != null)
                {
                    interfaces.Add(new NetworkInterfaceInfo
                    {
                        Name        = ni.Name,
                        Description = ni.Description,
                        IPAddress   = ipv4Address.Address.ToString(),
                        SubnetMask  = ipv4Address.IPv4Mask.ToString()
                    });
                }
            }

            return interfaces;
        }
    }

    public class NetworkInterfaceInfo
    {
        public string Name        { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IPAddress   { get; set; } = string.Empty;
        public string SubnetMask  { get; set; } = string.Empty;
    }
}