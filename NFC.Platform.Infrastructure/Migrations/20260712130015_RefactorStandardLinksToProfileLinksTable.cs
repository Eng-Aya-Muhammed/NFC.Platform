using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorStandardLinksToProfileLinksTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FacebookUrl",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "InstagramUrl",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "LinkedInUrl",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "WebsiteUrl",
                table: "UserProfiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FacebookUrl",
                table: "UserProfiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InstagramUrl",
                table: "UserProfiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LinkedInUrl",
                table: "UserProfiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WebsiteUrl",
                table: "UserProfiles",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}
