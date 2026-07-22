using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCardTemplatePreviewUrls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BackPreviewUrl",
                table: "CardTemplates");

            migrationBuilder.DropColumn(
                name: "FrontPreviewUrl",
                table: "CardTemplates");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BackPreviewUrl",
                table: "CardTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FrontPreviewUrl",
                table: "CardTemplates",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
