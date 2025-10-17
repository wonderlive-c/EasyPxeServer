using System.Net;
using System.Net.Sockets;
using System.Text.Json.Serialization;

namespace PxeServices.Entities.Settings;

public class DhcpSetting
{
    public static DhcpSetting Default =>
        new()
        {
            ServerName        = @"pxeserver",
            DhcpEnabled       = true,
            DhcpStartAddress  = IPAddress.Parse("192.168.1.100"),
            DhcpEndAddress    = IPAddress.Parse("192.168.1.199"),
            DhcpSubnetMask    = IPAddress.Parse("255.255.255.0"),
            DhcpGateway       = IPAddress.Parse("192.168.1.1"),
            Broadcast         = IPAddress.Parse("192.168.1.255"),
            DomainName        = @"localdomain",
            ServerIdentifier  = IPAddress.Parse("192.168.1.1"),
            DomainNameServers = [IPAddress.Parse("192.168.1.1")],
            ServerIpAddress   = IPAddress.Parse("192.168.1.1"),
            TFTPServerName    = @"pxeserver",
            BootFile          = @"menu.txt",
            LastCount         = 0
        };

    public string      ServerName        { get; set; }
    public bool        DhcpEnabled       { get; set; }
    
    public IPAddress   DhcpStartAddress  { get; set; }
    
    public IPAddress   DhcpEndAddress    { get; set; }
    
    public IPAddress   DhcpSubnetMask    { get; set; }
    
    public IPAddress   DhcpGateway       { get; set; }
    
    public IPAddress   Broadcast         { get; set; }
    public string      DomainName        { get; set; }
    
    public IPAddress   ServerIdentifier  { get; set; }
    
    public IPAddress[] DomainNameServers { get; set; }
    
    public IPAddress   ServerIpAddress   { get; set; }
    public string      TFTPServerName    { get; set; }
    public string      BootFile          { get; set; }
    public int         LastCount         { get; set; }

    /*
    伪代码 / 计划（中文）：
    1. 验证 DhcpStartAddress、DhcpEndAddress、DhcpSubnetMask 不为 null 且为 IPv4。
    2. 将起始、结束和子网掩码的 IPAddress 转换为对应的 uint (大端网络字节序) 以便算术运算。
    3. 计算子网网络地址 = start & mask；广播地址 = network | ~mask。
    4. 计算可用主机范围（排除 network 与 broadcast）：
       usableStart = max(start, network + 1)
       usableEnd   = min(end, broadcast - 1)
    5. 如果 usableStart > usableEnd 则没有可用地址，抛出异常。
    6. 计算范围大小 range = usableEnd - usableStart + 1。
    7. 根据 LastCount 计算下一个偏移：
       nextOffset = (LastCount + 1) % range  （处理负数）
    8. 得到下一个地址 = usableStart + nextOffset。
    9. 更新 LastCount 为 nextOffset（使后续调用循环遍历该可用池）。
    10. 将 uint 转回 IPAddress 并返回。
    注意：仅支持 IPv4；方法在参数无效时抛出相应的异常。
    */

    public IPAddress NextAddress()
    {
        if (DhcpStartAddress == null) throw new InvalidOperationException("DhcpStartAddress must be set.");
        if (DhcpEndAddress   == null) throw new InvalidOperationException("DhcpEndAddress must be set.");
        if (DhcpSubnetMask   == null) throw new InvalidOperationException("DhcpSubnetMask must be set.");

        if (DhcpStartAddress.AddressFamily != AddressFamily.InterNetwork
         || DhcpEndAddress.AddressFamily   != AddressFamily.InterNetwork
         || DhcpSubnetMask.AddressFamily   != AddressFamily.InterNetwork) { throw new NotSupportedException("Only IPv4 addresses are supported by NextAddress."); }

        uint start = ToUInt32(DhcpStartAddress);
        uint end   = ToUInt32(DhcpEndAddress);
        uint mask  = ToUInt32(DhcpSubnetMask);

        if (start > end) throw new InvalidOperationException("DhcpStartAddress must be less than or equal to DhcpEndAddress.");

        uint network   = start & mask;
        uint broadcast = network | ~mask;

        // 可用主机范围，排除 network 与 broadcast
        uint usableStart = Math.Max(start, network + 1);
        uint usableEnd   = Math.Min(end, broadcast - 1);

        if (usableStart > usableEnd) throw new InvalidOperationException("No usable IP addresses in the configured range/mask.");

        uint range = usableEnd - usableStart + 1;

        // 计算下一个偏移（相对于 usableStart），处理 LastCount 可能为负或超出范围的情况
        long nextOffsetLong                    = ((long)LastCount + 1) % (long)range;
        if (nextOffsetLong < 0) nextOffsetLong += range;
        uint nextOffset                        = (uint)nextOffsetLong;

        uint nextIpUInt = usableStart + nextOffset;

        // 更新 LastCount 为 offset（表示在可用池中的位置），便于下次调用继续循环
        LastCount = (int)nextOffset;

        return FromUInt32(nextIpUInt);
    }

    private static uint ToUInt32(IPAddress ip)
    {
        byte[] b = ip.GetAddressBytes();
        if (b.Length != 4) throw new ArgumentException("Only IPv4 addresses supported.");
        return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
    }

    private static IPAddress FromUInt32(uint value)
    {
        byte[] bytes = new byte[]
        {
            (byte)(value >> 24),
            (byte)(value >> 16),
            (byte)(value >> 8),
            (byte)value
        };
        return new IPAddress(bytes);
    }
}