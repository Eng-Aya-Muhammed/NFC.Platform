using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProfileIdToCardOrderItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserProfileId",
                table: "CardOrderItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CardOrderItems_UserProfileId",
                table: "CardOrderItems",
                column: "UserProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_CardOrderItems_UserProfiles_UserProfileId",
                table: "CardOrderItems",
                column: "UserProfileId",
                principalTable: "UserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CardOrderItems_UserProfiles_UserProfileId",
                table: "CardOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_CardOrderItems_UserProfileId",
                table: "CardOrderItems");

            migrationBuilder.DropColumn(
                name: "UserProfileId",
                table: "CardOrderItems");
        }
    }
}
