using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandOrderAndCardStatusLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Cards");

            migrationBuilder.AddColumn<Guid>(
                name: "LinkedOrderId",
                table: "TemplateRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProducedTemplateId",
                table: "TemplateRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BackPreviewUrl",
                table: "CardTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "CardTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FrontPreviewUrl",
                table: "CardTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfileUrl",
                table: "Cards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Cards",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UniqueCode",
                table: "Cards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ParentOrderId",
                table: "CardOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "CardOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateRequests_LinkedOrderId",
                table: "TemplateRequests",
                column: "LinkedOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TemplateRequests_ProducedTemplateId",
                table: "TemplateRequests",
                column: "ProducedTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_CardOrders_ParentOrderId",
                table: "CardOrders",
                column: "ParentOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_CardOrders_CardOrders_ParentOrderId",
                table: "CardOrders",
                column: "ParentOrderId",
                principalTable: "CardOrders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateRequests_CardOrders_LinkedOrderId",
                table: "TemplateRequests",
                column: "LinkedOrderId",
                principalTable: "CardOrders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateRequests_CardTemplates_ProducedTemplateId",
                table: "TemplateRequests",
                column: "ProducedTemplateId",
                principalTable: "CardTemplates",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CardOrders_CardOrders_ParentOrderId",
                table: "CardOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_TemplateRequests_CardOrders_LinkedOrderId",
                table: "TemplateRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_TemplateRequests_CardTemplates_ProducedTemplateId",
                table: "TemplateRequests");

            migrationBuilder.DropIndex(
                name: "IX_TemplateRequests_LinkedOrderId",
                table: "TemplateRequests");

            migrationBuilder.DropIndex(
                name: "IX_TemplateRequests_ProducedTemplateId",
                table: "TemplateRequests");

            migrationBuilder.DropIndex(
                name: "IX_CardOrders_ParentOrderId",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "LinkedOrderId",
                table: "TemplateRequests");

            migrationBuilder.DropColumn(
                name: "ProducedTemplateId",
                table: "TemplateRequests");

            migrationBuilder.DropColumn(
                name: "BackPreviewUrl",
                table: "CardTemplates");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "CardTemplates");

            migrationBuilder.DropColumn(
                name: "FrontPreviewUrl",
                table: "CardTemplates");

            migrationBuilder.DropColumn(
                name: "ProfileUrl",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "UniqueCode",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "ParentOrderId",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "CardOrders");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Cards",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
