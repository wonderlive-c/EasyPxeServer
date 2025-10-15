using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PxeStorageLite.Migrations
{
    /// <inheritdoc />
    public partial class addVnc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "VncConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ConnectionName = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    ConnectionId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Host = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    Port = table.Column<int>(type: "INTEGER", nullable: false),
                    Password = table.Column<string>(type: "TEXT", maxLength: 32, nullable: true),
                    WebSocketUrl = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    IsConnected = table.Column<bool>(type: "INTEGER", nullable: false),
                    DesktopName = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VncConnections", x => x.Id);
                    table.UniqueConstraint("AK_VncConnections_ConnectionName", x => x.ConnectionName);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VncConnections");
        }
    }
}
