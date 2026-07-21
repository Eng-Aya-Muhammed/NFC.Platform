using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLegacyCardFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cards_TenantId_ActivationCode",
                table: "Cards");

            migrationBuilder.DropIndex(
                name: "IX_CardOrderItems_ActivationCode",
                table: "CardOrderItems");

            migrationBuilder.DropColumn(
                name: "ActivationCode",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "ProfileUrl",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "QrCodeUrl",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "ActivationCode",
                table: "CardOrderItems");

            migrationBuilder.AlterColumn<string>(
                name: "UniqueCode",
                table: "Cards",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_TenantId_UniqueCode",
                table: "Cards",
                columns: new[] { "TenantId", "UniqueCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cards_TenantId_UniqueCode",
                table: "Cards");

            migrationBuilder.AlterColumn<string>(
                name: "UniqueCode",
                table: "Cards",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "ActivationCode",
                table: "Cards",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProfileUrl",
                table: "Cards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "QrCodeUrl",
                table: "Cards",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActivationCode",
                table: "CardOrderItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cards_TenantId_ActivationCode",
                table: "Cards",
                columns: new[] { "TenantId", "ActivationCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CardOrderItems_ActivationCode",
                table: "CardOrderItems",
                column: "ActivationCode");
        }
    }
}
