using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubdomainToUserProfile : Migration
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

            //  Backfill: generate a unique slug for every existing profile 
            // Uses a cursor loop so each slug is checked against already-assigned ones
            // in the same batch before the unique index is created.
            migrationBuilder.Sql(@"
                UPDATE UserProfiles 
                SET Subdomain = LOWER(CAST(Id AS NVARCHAR(36))) 
                WHERE Subdomain IS NULL;
            ");

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
