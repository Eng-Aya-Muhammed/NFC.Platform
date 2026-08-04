using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixRaceConditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_TenantId",
                table: "UserSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_TenantId_IsActive_EndDate",
                table: "UserSubscriptions");

            migrationBuilder.AddColumn<int>(
                name: "PendingQuantity",
                table: "CardDesigns",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_TenantId",
                table: "UserSubscriptions",
                column: "TenantId",
                unique: true,
                filter: "[IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_TenantId",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "PendingQuantity",
                table: "CardDesigns");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_TenantId",
                table: "UserSubscriptions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_TenantId_IsActive_EndDate",
                table: "UserSubscriptions",
                columns: new[] { "TenantId", "IsActive", "EndDate" });
        }
    }
}
