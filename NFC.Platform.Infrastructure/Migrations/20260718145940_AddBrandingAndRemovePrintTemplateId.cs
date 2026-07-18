using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandingAndRemovePrintTemplateId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CardOrders_CardTemplates_PrintTemplateId",
                table: "CardOrders");

            migrationBuilder.DropIndex(
                name: "IX_CardOrders_PrintTemplateId",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "PrintTemplateId",
                table: "EmployeeImportJobs");

            migrationBuilder.DropColumn(
                name: "PrintTemplateId",
                table: "CardOrders");

            migrationBuilder.AddColumn<Guid>(
                name: "ProfileTemplateId",
                table: "UserProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Companies",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProfileTemplateId",
                table: "Companies",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_ProfileTemplateId",
                table: "UserProfiles",
                column: "ProfileTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_ProfileTemplateId",
                table: "Companies",
                column: "ProfileTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_CardTemplates_ProfileTemplateId",
                table: "Companies",
                column: "ProfileTemplateId",
                principalTable: "CardTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfiles_CardTemplates_ProfileTemplateId",
                table: "UserProfiles",
                column: "ProfileTemplateId",
                principalTable: "CardTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_CardTemplates_ProfileTemplateId",
                table: "Companies");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProfiles_CardTemplates_ProfileTemplateId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_ProfileTemplateId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Companies_ProfileTemplateId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ProfileTemplateId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ProfileTemplateId",
                table: "Companies");

            migrationBuilder.AddColumn<Guid>(
                name: "PrintTemplateId",
                table: "EmployeeImportJobs",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PrintTemplateId",
                table: "CardOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CardOrders_PrintTemplateId",
                table: "CardOrders",
                column: "PrintTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_CardOrders_CardTemplates_PrintTemplateId",
                table: "CardOrders",
                column: "PrintTemplateId",
                principalTable: "CardTemplates",
                principalColumn: "Id");
        }
    }
}
