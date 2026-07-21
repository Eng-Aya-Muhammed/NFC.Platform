using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCardEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CardOrderItems_Cards_LinkedCardId",
                table: "CardOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfileMetrics_Cards_CardId",
                table: "ProfileMetrics");

            migrationBuilder.DropTable(
                name: "Cards");

            migrationBuilder.DropIndex(
                name: "IX_ProfileMetrics_CardId",
                table: "ProfileMetrics");

            migrationBuilder.DropIndex(
                name: "IX_CardOrderItems_LinkedCardId",
                table: "CardOrderItems");

            migrationBuilder.DropColumn(
                name: "CardId",
                table: "ProfileMetrics");

            migrationBuilder.DropColumn(
                name: "LinkedCardId",
                table: "CardOrderItems");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CardId",
                table: "ProfileMetrics",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LinkedCardId",
                table: "CardOrderItems",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Cards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    UniqueCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cards_CardOrders_CardOrderId",
                        column: x => x.CardOrderId,
                        principalTable: "CardOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cards_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Cards_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProfileMetrics_CardId",
                table: "ProfileMetrics",
                column: "CardId");

            migrationBuilder.CreateIndex(
                name: "IX_CardOrderItems_LinkedCardId",
                table: "CardOrderItems",
                column: "LinkedCardId");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_CardOrderId",
                table: "Cards",
                column: "CardOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_TenantId_UniqueCode",
                table: "Cards",
                columns: new[] { "TenantId", "UniqueCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cards_UserProfileId",
                table: "Cards",
                column: "UserProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_CardOrderItems_Cards_LinkedCardId",
                table: "CardOrderItems",
                column: "LinkedCardId",
                principalTable: "Cards",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfileMetrics_Cards_CardId",
                table: "ProfileMetrics",
                column: "CardId",
                principalTable: "Cards",
                principalColumn: "Id");
        }
    }
}
