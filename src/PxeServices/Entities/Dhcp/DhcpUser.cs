using PxeServices.Entities;
using System.ComponentModel.DataAnnotations;
using System.Net;

namespace PxeStorageLite;

public class DhcpUser : Entity<Guid>
{
    public bool IsEnabled    { get; set; }
    public bool IsSuperUser  { get; set; }
    public bool IsAuthorized { get; set; }
    public bool IsLocked     { get; set; }

    [MaxLength(32)]
    public string MacAddress { get; set; }

    [MaxLength(32)]
    public IPAddress IpAddress { get; set; }

    [MaxLength(32)]
    public string SubnetMask { get; set; }

    [MaxLength(32)]
    public string DefaultGateway { get; set; }

    [MaxLength(32)]
    public string DomainName { get; set; }

    [MaxLength(32)]
    public string NtpServer { get; set; }

    [MaxLength(32)]
    public string DnsServer { get; set; }

    [MaxLength(32)]
    public string DhcpTftpServer { get; set; }

    [MaxLength(32)]
    public string DhcpTftpPath { get; set; }

    [MaxLength(32)]
    public string DhcpTftpUsername { get; set; }

    [MaxLength(32)]
    public string DhcpTftpPassword { get; set; }

    [MaxLength(32)]
    public string DhcpTftpMode { get; set; }

    [MaxLength(32)]
    public string DhcpTftpVendorClass { get; set; }

    [MaxLength(32)]
    public string DhcpTftpNextServer { get; set; }

    [MaxLength(32)]
    public string DhcpTftpFilename { get; set; }

    [MaxLength(32)]
    public string DhcpTftpOptions { get; set; }

    [MaxLength(32)]
    public string DhcpTftpRootPath { get; set; }

    [MaxLength(32)]
    public string DhcpTftpRebootTime { get; set; }

    [MaxLength(32)]
    public string DhcpTftpRebootServer { get; set; }

    [MaxLength(32)]
    public string DhcpTftpRebootFilename { get; set; }

    [MaxLength(32)]
    public string DhcpTftpRebootOptions { get; set; }

    #region Implementation of IEntity<Guid>

    public Guid Id { get; set; } = Guid.NewGuid();

    #endregion
}