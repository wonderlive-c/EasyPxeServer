using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PxeServices
{
    /// <summary>
    /// Provides a service for managing the PXE server.
    /// </summary>
    /// <param name="dhcpService"></param>
    /// <param name="tftpService"></param>
    /// <param name="logger"></param>
    public class PxeServerService(DhcpService dhcpService, TftpService tftpService, ILogger<PxeServerService> logger) : IHostedService
    {
        private bool _isRunning = false;

        /// <summary>
        /// Occurs when the status of the PXE server changes.
        /// </summary>
        public event Action? StatusChanged;
        /// <summary>
        /// Gets a value indicating whether the PXE server is running.
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// Starts the PXE server.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Stops the PXE server.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (!_isRunning)
                return;

            await dhcpService.StopAsync(cancellationToken);
            await tftpService.StopAsync(cancellationToken);
            _isRunning = false;
            StatusChanged?.Invoke();
        }

        /// <summary>
        /// Restarts the PXE server.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task RestartAsync(CancellationToken cancellationToken)
        {
            await StopAsync(cancellationToken);
            await StartAsync(cancellationToken);
        }
        /// <summary>
        /// Gets a list of network interfaces on the system.
        /// </summary>
        /// <returns></returns>
        public static List<NetworkInterfaceInfo> GetNetworkInterfaces()
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

    /// <summary>
    /// Represents information about a network interface.
    /// </summary>
    public class NetworkInterfaceInfo
    {
        /// <summary>
        /// Gets or sets the name associated with the object.
        /// </summary>
        public string Name        { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the description of the object.
        /// </summary>
        public string Description { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the IP address of the network interface.
        /// </summary>
        public string IPAddress   { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the subnet mask of the network interface.
        /// </summary>
        public string SubnetMask  { get; set; } = string.Empty;
    }
}