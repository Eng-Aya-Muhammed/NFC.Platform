using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CardOrders_CardDesignId",
                table: "CardOrders");

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_TenantId_IsActive_EndDate",
                table: "UserSubscriptions",
                columns: new[] { "TenantId", "IsActive", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email_IsDeleted",
                table: "Users",
                columns: new[] { "Email", "IsDeleted" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfileMetrics_TenantId_CreatedAt",
                table: "ProfileMetrics",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CardOrders_CardDesignId_Status",
                table: "CardOrders",
                columns: new[] { "CardDesignId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CardOrders_TenantId_Status_CreatedAt",
                table: "CardOrders",
                columns: new[] { "TenantId", "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_TenantId_IsActive_EndDate",
                table: "UserSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email_IsDeleted",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_ProfileMetrics_TenantId_CreatedAt",
                table: "ProfileMetrics");

            migrationBuilder.DropIndex(
                name: "IX_CardOrders_CardDesignId_Status",
                table: "CardOrders");

            migrationBuilder.DropIndex(
                name: "IX_CardOrders_TenantId_Status_CreatedAt",
                table: "CardOrders");

            migrationBuilder.CreateIndex(
                name: "IX_CardOrders_CardDesignId",
                table: "CardOrders",
                column: "CardDesignId");
        }
    }
}
