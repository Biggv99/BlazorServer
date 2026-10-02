using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OptimusPrime.Migrations
{
    /// <inheritdoc />
    public partial class DeviceSyncToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceSyncToken",
                table: "User",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FirebaseToken",
                table: "User",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeviceSyncToken",
                table: "User");

            migrationBuilder.DropColumn(
                name: "FirebaseToken",
                table: "User");
        }
    }
}
