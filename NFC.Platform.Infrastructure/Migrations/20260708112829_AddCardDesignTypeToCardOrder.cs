using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCardDesignTypeToCardOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CardDesignType",
                table: "CardOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CardOrders_CardTemplates_PrintTemplateId",
                table: "CardOrders");

            migrationBuilder.DropIndex(
                name: "IX_CardOrders_PrintTemplateId",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "CardDesignType",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "PrintTemplateId",
                table: "CardOrders");
        }
    }
}
