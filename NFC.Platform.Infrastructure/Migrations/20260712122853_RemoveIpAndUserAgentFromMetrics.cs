using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveIpAndUserAgentFromMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "ProfileMetrics");

            migrationBuilder.DropColumn(
                name: "UserAgent",
                table: "ProfileMetrics");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "ProfileMetrics",
                type: "nvarchar(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UserAgent",
                table: "ProfileMetrics",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }
    }
}
