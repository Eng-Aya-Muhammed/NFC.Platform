using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCardReissueAndMetricTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CardId",
                table: "ProfileMetrics",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddress",
                table: "CardOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfileMetrics_CardId",
                table: "ProfileMetrics",
                column: "CardId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProfileMetrics_Cards_CardId",
                table: "ProfileMetrics",
                column: "CardId",
                principalTable: "Cards",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProfileMetrics_Cards_CardId",
                table: "ProfileMetrics");

            migrationBuilder.DropIndex(
                name: "IX_ProfileMetrics_CardId",
                table: "ProfileMetrics");

            migrationBuilder.DropColumn(
                name: "CardId",
                table: "ProfileMetrics");

            migrationBuilder.DropColumn(
                name: "ShippingAddress",
                table: "CardOrders");
        }
    }
}
