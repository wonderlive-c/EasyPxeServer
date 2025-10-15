using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PxeStorageLite.Migrations
{
    /// <inheritdoc />
    public partial class init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DhcpUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsSuperUser = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsAuthorized = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsLocked = table.Column<bool>(type: "INTEGER", nullable: false),
                    MacAddress = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    IpAddress = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    SubnetMask = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    DefaultGateway = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    DomainName = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    NtpServer = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    DnsServer = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    DhcpTftpServer = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    DhcpTftpPath = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    DhcpTftpUsername = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    DhcpTftpPassword = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    DhcpTftpMode = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    DhcpTftpVendorClass = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    DhcpTftpNextServer = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    DhcpTftpFilename = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    DhcpTftpOptions = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    DhcpTftpRootPath = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    DhcpTftpRebootTime = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    DhcpTftpRebootServer = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    DhcpTftpRebootFilename = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    DhcpTftpRebootOptions = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DhcpUsers", x => x.Id);
                    table.UniqueConstraint("AK_DhcpUsers_MacAddress", x => x.MacAddress);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DhcpUsers");
        }
    }
}
