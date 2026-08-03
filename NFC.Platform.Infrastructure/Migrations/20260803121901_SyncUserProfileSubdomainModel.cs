using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncUserProfileSubdomainModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Subdomain",
                table: "UserProfiles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_Subdomain",
                table: "UserProfiles",
                column: "Subdomain",
                unique: true,
                filter: "[Subdomain] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_Subdomain",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "Subdomain",
                table: "UserProfiles");
        }
    }
}
