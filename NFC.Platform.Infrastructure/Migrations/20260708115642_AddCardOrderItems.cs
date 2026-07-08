using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCardOrderItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CardOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    JobTitle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Department = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ActivationCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LinkedCardId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsActivated = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardOrderItems_CardOrders_CardOrderId",
                        column: x => x.CardOrderId,
                        principalTable: "CardOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CardOrderItems_Cards_LinkedCardId",
                        column: x => x.LinkedCardId,
                        principalTable: "Cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardOrderItems_ActivationCode",
                table: "CardOrderItems",
                column: "ActivationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CardOrderItems_CardOrderId",
                table: "CardOrderItems",
                column: "CardOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_CardOrderItems_LinkedCardId",
                table: "CardOrderItems",
                column: "LinkedCardId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardOrderItems");
        }
    }
}
