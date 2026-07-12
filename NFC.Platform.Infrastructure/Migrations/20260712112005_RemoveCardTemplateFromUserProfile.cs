using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCardTemplateFromUserProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserProfiles_CardTemplates_CardTemplateId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_CardTemplateId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "CardTemplateId",
                table: "UserProfiles");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CardTemplateId",
                table: "UserProfiles",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_CardTemplateId",
                table: "UserProfiles",
                column: "CardTemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfiles_CardTemplates_CardTemplateId",
                table: "UserProfiles",
                column: "CardTemplateId",
                principalTable: "CardTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
