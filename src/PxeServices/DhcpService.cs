using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using DotNetProjects.DhcpServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PxeServices.Entities.Dhcp;
using PxeServices.Entities.Settings;
using PxeStorageLite;

namespace PxeServices
{
    // 在关键操作和分支处增加详细日志，便于调试
    public class DhcpService(ILogger<DhcpService> logger, IServiceProvider serviceProvider) : IHostedService
    {
        private CancellationTokenSource? _cancellationTokenSource;

        static  byte         nextIP = 10;
        private DHCPServer?  dhcpServer;
        private DhcpSetting? dhcp;

        public bool   IsRunning           => dhcpServer != null;
        public string BootFileName        => dhcp?.BootFile;
        public string NextServerIp        { get; set; } = "";
        public string SelectedInterfaceIp { get; set; } = "";


        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (dhcpServer != null)
            {
                logger.LogWarning("DHCP服务已在运行，StartAsync被忽略");
                return;
            }

            var eth0If = NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                                         .FirstOrDefault(x => x.GetIPProperties().UnicastAddresses
                                                               .Any(u => u.Address.AddressFamily == AddressFamily.InterNetwork && u.Address.ToString() == SelectedInterfaceIp));


            logger.LogInformation("SelectedInterfaceIp {Name}",            SelectedInterfaceIp);
            logger.LogInformation("SendDhcpAnswerNetworkInterface {Name}", eth0If?.Name);

            using var scope = serviceProvider.CreateScope();
            var       repo  = scope.ServiceProvider.GetRequiredService<IObjectSettingRepository>();
            dhcp = repo.GetObjectSetting<DhcpSetting>() ?? DhcpSetting.Default;

            dhcpServer                                =  new DHCPServer(IPAddress.Parse(SelectedInterfaceIp));
            dhcpServer.ServerName                     =  dhcp.ServerName;
            dhcpServer.OnDataReceived                 += Request;
            dhcpServer.BroadcastAddress               =  dhcp.Broadcast;
            dhcpServer.SendDhcpAnswerNetworkInterface =  eth0If;
            dhcpServer.Start();
        }

        private async void Request(DHCPRequest dhcpRequest)
        {
            try
            {
                var type = dhcpRequest.GetMsgType();
                var mac  = ByteArrayToString(dhcpRequest.GetChaddr());

                logger.LogInformation("收到DHCP请求，来自: {RemoteEndPoint}", mac);
                DebugRequest(dhcpRequest);

                using var scope              = serviceProvider.CreateScope();
                var       dhcpUserRepository = scope.ServiceProvider.GetRequiredService<IDhcpUserRepository>();
                var existsDhcpUser = await dhcpUserRepository.GetByMacAddressOrCreate(mac,
                                                                                      user =>
                                                                                      {
                                                                                          var ipAddress = dhcp!.NextAddress();
                                                                                          user.IpAddress    = ipAddress;
                                                                                          user.MacAddress   = mac;
                                                                                          user.IsAuthorized = true;
                                                                                          user.IsEnabled    = true;
                                                                                      });


                logger.LogInformation("{type} request from {mac}, it will be {ip}", type, mac, existsDhcpUser!.IpAddress);


                var replyOptions = new DHCPReplyOptions();
                // Options should be filled with valid data. Only requested options will be sent.
                replyOptions.SubnetMask        = dhcp!.DhcpSubnetMask;
                replyOptions.DomainName        = dhcp!.DomainName;
                replyOptions.ServerIdentifier  = dhcp!.ServerIdentifier;
                replyOptions.RouterIP          = dhcp!.DhcpGateway;
                replyOptions.DomainNameServers = dhcp!.DomainNameServers;
                replyOptions.ServerIpAddress   = dhcp!.ServerIpAddress;
                // Some static routes
                replyOptions.StaticRoutes = [];

                replyOptions.OtherRequestedOptions.Add(DHCPOption.BroadcastAddress, dhcp.Broadcast.GetAddressBytes());
                replyOptions.OtherRequestedOptions.Add(DHCPOption.TFTPServerName,   Encoding.UTF8.GetBytes(dhcp.TFTPServerName));
                replyOptions.OtherRequestedOptions.Add(DHCPOption.BootfileName,     Encoding.UTF8.GetBytes(dhcp.BootFile));
                replyOptions.OtherRequestedOptions.Add((DHCPOption)175,             [0x01, 0x01, 0x01, 0x08, 0x01, 0x01]);

                switch (type)
                {
                    // Lets send reply to client!
                    case DHCPMsgType.DHCPDISCOVER:
                        dhcpRequest.SendDHCPReply(DHCPMsgType.DHCPOFFER, existsDhcpUser.IpAddress, replyOptions);
                        break;
                    case DHCPMsgType.DHCPREQUEST:
                        dhcpRequest.SendDHCPReply(DHCPMsgType.DHCPACK, existsDhcpUser.IpAddress, replyOptions);
                        break;
                }
            }
            catch (Exception ex) { logger.LogError(ex, "处理DHCP请求时出错"); }
        }

        private void DebugRequest(DHCPRequest dhcpRequest)
        {
            // Lets show some request info
            var requestedOptions = dhcpRequest.GetRequestedOptionsList();
            if (requestedOptions != null)
            {
                logger.LogInformation("Requested options:");
                foreach (var option in requestedOptions) logger.LogInformation(" " + option.ToString());
            }

            // Option 82 info
            var relayInfoN = dhcpRequest.GetRelayInfo();
            if (relayInfoN != null)
            {
                var relayInfo = (RelayInfo)relayInfoN;
                if (relayInfo.AgentCircuitID != null)
                    logger.LogInformation("Relay agent circuit ID: " + ByteArrayToString(relayInfo.AgentCircuitID));
                if (relayInfo.AgentRemoteID != null)
                    logger.LogInformation("Relay agent remote ID: " + ByteArrayToString(relayInfo.AgentRemoteID));
            }

            var options = dhcpRequest.GetAllOptions();
            logger.LogInformation("Options:");
            foreach (var option in options.Keys) { logger.LogInformation("{name}:{value}", option, ByteArrayToString(options[option])); }
        }

        private static string ByteArrayToString(byte[] ar) { return BitConverter.ToString(ar); }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (dhcpServer == null)
            {
                logger.LogWarning("DHCP服务未运行，StopAsync被忽略");
                return;
            }

            try
            {
                logger.LogInformation("正在停止DHCP服务...");
                if (_cancellationTokenSource != null) { await _cancellationTokenSource?.CancelAsync()!; }

                dhcpServer.Dispose();
                dhcpServer = null;
                logger.LogInformation("DHCP服务已停止");
            }
            catch (Exception ex) { logger.LogError(ex, "停止DHCP服务时出错"); }
        }
    }
}