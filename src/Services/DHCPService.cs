using DotNetProjects.DhcpServer;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace PxeBlazorServer.Services
{
    // 在关键操作和分支处增加详细日志，便于调试
    public class DHCPService(ILogger<DHCPService> logger) : IHostedService
    {
        private CancellationTokenSource? _cancellationTokenSource;

        static  byte                          nextIP = 10;
        static  Dictionary<string, IPAddress> leases = new();
        private DHCPServer?                   dhcpServer;

        public bool   IsRunning           => dhcpServer != null;
        public string BootFileName        { get; set; } = "menu.txt";
        public string NextServerIp        { get; set; } = "";
        public string RouterIp            { get; set; } = "";
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
            dhcpServer                                =  new DHCPServer(IPAddress.Parse(SelectedInterfaceIp));
            dhcpServer.ServerName                     =  "SharpDHCPServer";
            dhcpServer.OnDataReceived                 += Request;
            dhcpServer.BroadcastAddress               =  IPAddress.Broadcast;
            dhcpServer.SendDhcpAnswerNetworkInterface =  eth0If;
            dhcpServer.Start();
        }

        private void Request(DHCPRequest dhcpRequest)
        {
            var op = new DHCPReplyOptions
            {
                SubnetMask            = IPAddress.Parse("255.255.0.0"),
                ServerIpAddress       = IPAddress.Parse("10.10.10.254"),
                IPAddressLeaseTime    = 0,
                RenewalTimeValue_T1   = 3600,
                RebindingTimeValue_T2 = 7200,
                DomainName            = "8.8.8.8",
                ServerIdentifier      = IPAddress.Parse("10.10.10.254"),
                RouterIP              = IPAddress.Parse("0.0.0.0"),
                DomainNameServers = new IPAddress[]
                {
                },
                OtherRequestedOptions = []
            };

            try
            {
                var type = dhcpRequest.GetMsgType();
                var mac  = ByteArrayToString(dhcpRequest.GetChaddr());

                logger.LogInformation("收到DHCP请求，来自: {RemoteEndPoint}", mac);

                // IP for client
                IPAddress ip;
                if (!leases.TryGetValue(mac, out ip))
                {
                    ip          = new IPAddress([10, 10, 10, nextIP++]);
                    leases[mac] = ip;
                }

                logger.LogInformation("{type} request from {mac}, it will be {ip}", type, mac, ip);

                var options = dhcpRequest.GetAllOptions();
                logger.LogInformation("Options:");
                foreach (DHCPOption option in options.Keys) { logger.LogInformation("{name}:{value}", option, ByteArrayToString(options[option])); }

                // Lets show some request info
                var requestedOptions = dhcpRequest.GetRequestedOptionsList();
                if (requestedOptions != null)
                {
                    logger.LogInformation("Requested options:");
                    foreach (DHCPOption option in requestedOptions) logger.LogInformation(" " + option.ToString());
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

                var replyOptions = new DHCPReplyOptions();
                // Options should be filled with valid data. Only requested options will be sent.
                replyOptions.SubnetMask        = IPAddress.Parse("255.255.0.0");
                replyOptions.DomainName        = "PXE-DHCPServer";
                replyOptions.ServerIdentifier  = IPAddress.Parse(NextServerIp);
                replyOptions.RouterIP          = IPAddress.Parse("0.0.0.0");
                replyOptions.DomainNameServers = [IPAddress.Parse("8.8.8.8")];
                replyOptions.ServerIpAddress   = IPAddress.Parse(NextServerIp);
                // Some static routes
                replyOptions.StaticRoutes = [];

                replyOptions.OtherRequestedOptions.Add(DHCPOption.BroadcastAddress, (IPAddress.TryParse("10.10.255.255", out var b) ? b : IPAddress.Broadcast).GetAddressBytes());
                replyOptions.OtherRequestedOptions.Add(DHCPOption.TFTPServerName,   Encoding.UTF8.GetBytes(SelectedInterfaceIp));
                replyOptions.OtherRequestedOptions.Add(DHCPOption.BootfileName,     "menu.txt"u8.ToArray());
                replyOptions.OtherRequestedOptions.Add((DHCPOption)175,             [0x01, 0x01, 0x01, 0x08, 0x01, 0x01]);

                switch (type)
                {
                    // Lets send reply to client!
                    case DHCPMsgType.DHCPDISCOVER:
                        dhcpRequest.SendDHCPReply(DHCPMsgType.DHCPOFFER, ip, replyOptions);
                        break;
                    case DHCPMsgType.DHCPREQUEST:
                        dhcpRequest.SendDHCPReply(DHCPMsgType.DHCPACK, ip, replyOptions);
                        break;
                }
            }
            catch (Exception ex) { Console.WriteLine(ex); }
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