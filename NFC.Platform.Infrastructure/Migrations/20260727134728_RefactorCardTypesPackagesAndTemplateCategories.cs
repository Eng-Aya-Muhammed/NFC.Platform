using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorCardTypesPackagesAndTemplateCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CardTemplates_Tenants_TenantId",
                table: "CardTemplates");

            migrationBuilder.DropTable(
                name: "CardPricings");

            migrationBuilder.DropIndex(
                name: "IX_CardTemplates_TenantId",
                table: "CardTemplates");

            migrationBuilder.DropColumn(
                name: "CardType",
                table: "EmployeeImportJobs");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "CardTemplates");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "CardTemplates");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "CardTemplates");

            migrationBuilder.DropColumn(
                name: "StyleConfigJson",
                table: "CardTemplates");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CardTemplates");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "CardTemplates");

            migrationBuilder.DropColumn(
                name: "CardType",
                table: "CardOrders");

            migrationBuilder.AddColumn<int>(
                name: "PreferredLanguage",
                table: "UserProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "CardPackageId",
                table: "EmployeeImportJobs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CardTypeId",
                table: "EmployeeImportJobs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CategoryId",
                table: "CardTemplates",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "FileUrl",
                table: "CardTemplates",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NameAr",
                table: "CardTemplates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NameEn",
                table: "CardTemplates",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "CardTemplates",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CardPackageId",
                table: "CardOrders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CardTypeId",
                table: "CardOrders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "NumberOfCardsRequired",
                table: "CardOrderItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresCard",
                table: "CardOrderItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CardPackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NumberOfCards = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardPackages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CardTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PhotoUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TemplateCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    NameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TemplateCategories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeImportJobs_CardPackageId",
                table: "EmployeeImportJobs",
                column: "CardPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeImportJobs_CardTypeId",
                table: "EmployeeImportJobs",
                column: "CardTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CardTemplates_CategoryId",
                table: "CardTemplates",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CardOrders_CardPackageId",
                table: "CardOrders",
                column: "CardPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_CardOrders_CardTypeId",
                table: "CardOrders",
                column: "CardTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_CardOrders_CardPackages_CardPackageId",
                table: "CardOrders",
                column: "CardPackageId",
                principalTable: "CardPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CardOrders_CardTypes_CardTypeId",
                table: "CardOrders",
                column: "CardTypeId",
                principalTable: "CardTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CardTemplates_TemplateCategories_CategoryId",
                table: "CardTemplates",
                column: "CategoryId",
                principalTable: "TemplateCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeImportJobs_CardPackages_CardPackageId",
                table: "EmployeeImportJobs",
                column: "CardPackageId",
                principalTable: "CardPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeImportJobs_CardTypes_CardTypeId",
                table: "EmployeeImportJobs",
                column: "CardTypeId",
                principalTable: "CardTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CardOrders_CardPackages_CardPackageId",
                table: "CardOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_CardOrders_CardTypes_CardTypeId",
                table: "CardOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_CardTemplates_TemplateCategories_CategoryId",
                table: "CardTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeImportJobs_CardPackages_CardPackageId",
                table: "EmployeeImportJobs");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeImportJobs_CardTypes_CardTypeId",
                table: "EmployeeImportJobs");

            migrationBuilder.DropTable(
                name: "CardPackages");

            migrationBuilder.DropTable(
                name: "CardTypes");

            migrationBuilder.DropTable(
                name: "TemplateCategories");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeImportJobs_CardPackageId",
                table: "EmployeeImportJobs");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeImportJobs_CardTypeId",
                table: "EmployeeImportJobs");

            migrationBuilder.DropIndex(
                name: "IX_CardTemplates_CategoryId",
                table: "CardTemplates");

            migrationBuilder.DropIndex(
                name: "IX_CardOrders_CardPackageId",
                table: "CardOrders");

            migrationBuilder.DropIndex(
                name: "IX_CardOrders_CardTypeId",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "PreferredLanguage",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "CardPackageId",
                table: "EmployeeImportJobs");

            migrationBuilder.DropColumn(
                name: "CardTypeId",
                table: "EmployeeImportJobs");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "CardTemplates");

            migrationBuilder.DropColumn(
                name: "FileUrl",
                table: "CardTemplates");

            migrationBuilder.DropColumn(
                name: "NameAr",
                table: "CardTemplates");

            migrationBuilder.DropColumn(
                name: "NameEn",
                table: "CardTemplates");

            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "CardTemplates");

            migrationBuilder.DropColumn(
                name: "CardPackageId",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "CardTypeId",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "NumberOfCardsRequired",
                table: "CardOrderItems");

            migrationBuilder.DropColumn(
                name: "RequiresCard",
                table: "CardOrderItems");

            migrationBuilder.AddColumn<int>(
                name: "CardType",
                table: "EmployeeImportJobs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "CardTemplates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "CardTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "CardTemplates",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "StyleConfigJson",
                table: "CardTemplates",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "CardTemplates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "CardTemplates",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CardType",
                table: "CardOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "CardPricings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardType = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardPricings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardTemplates_TenantId",
                table: "CardTemplates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CardPricings_CardType_IsActive",
                table: "CardPricings",
                columns: new[] { "CardType", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_CardTemplates_Tenants_TenantId",
                table: "CardTemplates",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
