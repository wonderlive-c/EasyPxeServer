using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Options;
using PxeServices.Entities.Settings;
using PxeServices.Entities.VncClient;

namespace PxeStorageLite;

public class PxeDbContext(DbContextOptions<PxeDbContext> options) : DbContext(options)
{
    public             DbSet<DhcpUser>      DhcpUsers                                             { get; set; }
    public             DbSet<VncConnection> VncConnections                                        { get; set; }
    public             DbSet<ObjectSetting> ObjectSettings                                        { get; set; }
    protected override void                 OnConfiguring(DbContextOptionsBuilder optionsBuilder) { optionsBuilder.UseSqlite("Data Source=pxe_storage_lite.db"); }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DhcpUser>(entity =>
        {
            entity.ToTable("DhcpUsers");
            entity.HasKey(e => e.Id);
            entity.HasAlternateKey(e => e.MacAddress);
            entity.Property(e => e.MacAddress).IsRequired();
            entity.Property(e => e.IpAddress).IsRequired();
            entity.Property(e => e.SubnetMask).IsRequired(false);
            entity.Property(e => e.DefaultGateway).IsRequired(false);
            entity.Property(e => e.DomainName).IsRequired(false);
            entity.Property(e => e.NtpServer).IsRequired(false);
            entity.Property(e => e.DnsServer).IsRequired(false);
            entity.Property(e => e.DhcpTftpServer).IsRequired(false);
            entity.Property(e => e.DhcpTftpPath).IsRequired(false);
            entity.Property(e => e.DhcpTftpUsername).IsRequired(false);
            entity.Property(e => e.DhcpTftpPassword).IsRequired(false);
            entity.Property(e => e.DhcpTftpMode).IsRequired(false);
            entity.Property(e => e.DhcpTftpVendorClass).IsRequired(false);
            entity.Property(e => e.DhcpTftpNextServer).IsRequired(false);
            entity.Property(e => e.DhcpTftpFilename).IsRequired(false);
            entity.Property(e => e.DhcpTftpOptions).IsRequired(false);
            entity.Property(e => e.DhcpTftpRootPath).IsRequired(false);
            entity.Property(e => e.DhcpTftpRebootTime).IsRequired(false);
            entity.Property(e => e.DhcpTftpRebootServer).IsRequired(false);
            entity.Property(e => e.DhcpTftpRebootFilename).IsRequired(false);
            entity.Property(e => e.DhcpTftpRebootOptions).IsRequired(false);
        });
        modelBuilder.Entity<VncConnection>(entity =>
        {
            entity.ToTable("VncConnections");
            entity.HasKey(e => e.Id);
            entity.HasAlternateKey(e => e.ConnectionName);
            entity.Property(e => e.DesktopName).IsRequired(false);
            entity.Property(e => e.Host).IsRequired();
            entity.Property(e => e.Port).IsRequired();
            entity.Property(e => e.Password).IsRequired(false);
            entity.Property(e => e.WebSocketUrl).IsRequired();
            entity.Property(e => e.IsConnected).IsRequired();
            entity.Property(e => e.ConnectionId).IsRequired(false);
        });
        modelBuilder.Entity<ObjectSetting>(entity =>
        {
            entity.ToTable("ObjectSettings");
            entity.HasKey(e => e.Id);
            entity.HasAlternateKey(e => e.Name);
            entity.Property(e => e.Name).IsRequired();
            entity.Property(e => e.Value).IsRequired();
        });
    }
}