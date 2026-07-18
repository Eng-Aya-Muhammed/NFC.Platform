using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeparateCardDesignFromTemplateRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TemplateRequests_CardOrders_LinkedOrderId",
                table: "TemplateRequests");

            migrationBuilder.DropIndex(
                name: "IX_TemplateRequests_LinkedOrderId",
                table: "TemplateRequests");

            migrationBuilder.DropColumn(
                name: "LinkedOrderId",
                table: "TemplateRequests");

            migrationBuilder.AddColumn<int>(
                name: "RequestType",
                table: "TemplateRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DesignNotes",
                table: "CardOrders",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DesignReferenceUrl",
                table: "CardOrders",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "CardOrders",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequestType",
                table: "TemplateRequests");

            migrationBuilder.DropColumn(
                name: "DesignNotes",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "DesignReferenceUrl",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "CardOrders");

            migrationBuilder.AddColumn<Guid>(
                name: "LinkedOrderId",
                table: "TemplateRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TemplateRequests_LinkedOrderId",
                table: "TemplateRequests",
                column: "LinkedOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_TemplateRequests_CardOrders_LinkedOrderId",
                table: "TemplateRequests",
                column: "LinkedOrderId",
                principalTable: "CardOrders",
                principalColumn: "Id");
        }
    }
}
