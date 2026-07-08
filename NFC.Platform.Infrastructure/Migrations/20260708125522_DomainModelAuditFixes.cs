using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DomainModelAuditFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CardOrderItems_ActivationCode",
                table: "CardOrderItems");

            migrationBuilder.DropColumn(
                name: "CardNumber",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "ActivationCode",
                table: "CardOrderItems");

            migrationBuilder.DropColumn(
                name: "IsActivated",
                table: "CardOrderItems");

            migrationBuilder.AddColumn<DateTime>(
                name: "ActivatedAt",
                table: "Cards",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CardOrderId",
                table: "Cards",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NfcChipUid",
                table: "Cards",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_AdminUserId",
                table: "Companies",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_ActivationCode",
                table: "Cards",
                column: "ActivationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cards_CardOrderId",
                table: "Cards",
                column: "CardOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_CardOrders_CardOrderId",
                table: "Cards",
                column: "CardOrderId",
                principalTable: "CardOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Users_AdminUserId",
                table: "Companies",
                column: "AdminUserId",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_RefreshTokens_Users_UserId",
                table: "RefreshTokens",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_CardOrders_CardOrderId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Users_AdminUserId",
                table: "Companies");

            migrationBuilder.DropForeignKey(
                name: "FK_RefreshTokens_Users_UserId",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_Companies_AdminUserId",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Cards_ActivationCode",
                table: "Cards");

            migrationBuilder.DropIndex(
                name: "IX_Cards_CardOrderId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "ActivatedAt",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "CardOrderId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "NfcChipUid",
                table: "Cards");

            migrationBuilder.AddColumn<string>(
                name: "CardNumber",
                table: "Cards",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Cards",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Cards",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ActivationCode",
                table: "CardOrderItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActivated",
                table: "CardOrderItems",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_CardOrderItems_ActivationCode",
                table: "CardOrderItems",
                column: "ActivationCode",
                unique: true);
        }
    }
}
