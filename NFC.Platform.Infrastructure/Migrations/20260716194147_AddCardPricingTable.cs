using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCardPricingTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "CardOrders",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "CardOrders",
                type: "decimal(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "CardPricings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardType = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardPricings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardPricings_CardType_IsActive",
                table: "CardPricings",
                columns: new[] { "CardType", "IsActive" });

            migrationBuilder.InsertData(
                table: "CardPricings",
                columns: new[] { "Id", "CardType", "UnitPrice", "Currency", "IsActive", "EffectiveFrom", "EffectiveTo", "CreatedAt", "UpdatedAt", "IsDeleted" },
                values: new object[,]
                {
                    { Guid.NewGuid(), 3, 4.500m, "KWD", true, new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc), false }, // Plastic (3)
                    { Guid.NewGuid(), 4, 6.000m, "KWD", true, new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc), false }, // Wooden (4)
                    { Guid.NewGuid(), 2, 8.500m, "KWD", true, new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 7, 16, 0, 0, 0, DateTimeKind.Utc), false }  // Metal (2)
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardPricings");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "CardOrders");
        }
    }
}
