using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountCodesVipCustomersAndProfileEnrichment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_Subdomain",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "Subdomain",
                table: "UserProfiles");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "SubscriptionPlans",
                newName: "NameEn");

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "UserProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVip",
                table: "UserProfiles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "VipDisplayOrder",
                table: "UserProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                table: "SubscriptionPlans",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "Companies",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsVip",
                table: "Companies",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Companies",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "VipDisplayOrder",
                table: "Companies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "WebsiteUrl",
                table: "Companies",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DiscountCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DiscountValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountCodes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_IsVip_IsDeleted_VipDisplayOrder",
                table: "UserProfiles",
                columns: new[] { "IsVip", "IsDeleted", "VipDisplayOrder" },
                filter: "[EmployeeId] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_IsVip_IsDeleted_VipDisplayOrder",
                table: "Companies",
                columns: new[] { "IsVip", "IsDeleted", "VipDisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_DiscountCodes_Code",
                table: "DiscountCodes",
                column: "Code",
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DiscountCodes");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_IsVip_IsDeleted_VipDisplayOrder",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Companies_IsVip_IsDeleted_VipDisplayOrder",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "IsVip",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "VipDisplayOrder",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "NameAr",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "Bio",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "IsVip",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "VipDisplayOrder",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "WebsiteUrl",
                table: "Companies");

            migrationBuilder.RenameColumn(
                name: "NameEn",
                table: "SubscriptionPlans",
                newName: "Name");

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
    }
}
