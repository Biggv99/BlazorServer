using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptimusPrime.Migrations
{
    /// <inheritdoc />
    public partial class Devices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_User_Ruolo",
                table: "User");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "User");

            migrationBuilder.DropColumn(
                name: "PublicKey",
                table: "User");

            migrationBuilder.RenameColumn(
                name: "Ruolo",
                table: "User",
                newName: "Role");

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<string>(type: "text", nullable: false),
                    PublicKey = table.Column<string>(type: "text", nullable: false),
                    DeviceName = table.Column<string>(type: "text", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devices_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_User_Role",
                table: "User",
                sql: "\"Role\" IN ('Admin', 'Editor', 'Viewer')");

            migrationBuilder.CreateIndex(
                name: "IX_Refresh_UserId",
                table: "Refresh",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Audit_UserId",
                table: "Audit",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Devices_DeviceId",
                table: "Devices",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devices_UserId",
                table: "Devices",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Audit_User_UserId",
                table: "Audit",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Refresh_User_UserId",
                table: "Refresh",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Audit_User_UserId",
                table: "Audit");

            migrationBuilder.DropForeignKey(
                name: "FK_Refresh_User_UserId",
                table: "Refresh");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropCheckConstraint(
                name: "CK_User_Role",
                table: "User");

            migrationBuilder.DropIndex(
                name: "IX_Refresh_UserId",
                table: "Refresh");

            migrationBuilder.DropIndex(
                name: "IX_Audit_UserId",
                table: "Audit");

            migrationBuilder.RenameColumn(
                name: "Role",
                table: "User",
                newName: "Ruolo");

            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "User",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PublicKey",
                table: "User",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddCheckConstraint(
                name: "CK_User_Ruolo",
                table: "User",
                sql: "\"Ruolo\" IN ('Admin', 'Editor', 'Viewer')");
        }
    }
}
