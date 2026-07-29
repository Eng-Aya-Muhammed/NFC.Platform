using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MoveCardTypeIdToCardDesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CardOrders_CardPackages_CardPackageId",
                table: "CardOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_CardOrders_CardTypes_CardTypeId",
                table: "CardOrders");

            migrationBuilder.DropIndex(
                name: "IX_CardOrders_CardPackageId",
                table: "CardOrders");

            migrationBuilder.DropIndex(
                name: "IX_CardOrders_CardTypeId",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "BackDesignUrl",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "CardDesignType",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "CardName",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "CardPackageId",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "CardTypeId",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "DeliveryMethod",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "ExcelDataUrl",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "FrontDesignUrl",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "ShippingAddress",
                table: "CardOrders");

            migrationBuilder.AddColumn<Guid>(
                name: "CardDesignId",
                table: "CardOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuantityPerEmployee",
                table: "CardOrders",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateTable(
                name: "CardDesigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardPackageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomQuantity = table.Column<int>(type: "int", nullable: true),
                    TotalQuantity = table.Column<int>(type: "int", nullable: false),
                    UsedQuantity = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ExcelDataUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CardDesignType = table.Column<int>(type: "int", nullable: false),
                    FrontDesignUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    BackDesignUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsPaid = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    PaymentStatus = table.Column<int>(type: "int", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentTransactionId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardDesigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CardDesigns_CardPackages_CardPackageId",
                        column: x => x.CardPackageId,
                        principalTable: "CardPackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CardDesigns_CardTypes_CardTypeId",
                        column: x => x.CardTypeId,
                        principalTable: "CardTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CardDesigns_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CardDesigns_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardOrders_CardDesignId",
                table: "CardOrders",
                column: "CardDesignId");

            migrationBuilder.CreateIndex(
                name: "IX_CardDesigns_CardPackageId",
                table: "CardDesigns",
                column: "CardPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_CardDesigns_CardTypeId",
                table: "CardDesigns",
                column: "CardTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_CardDesigns_TenantId",
                table: "CardDesigns",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CardDesigns_UserId",
                table: "CardDesigns",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_CardOrders_CardDesigns_CardDesignId",
                table: "CardOrders",
                column: "CardDesignId",
                principalTable: "CardDesigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CardOrders_CardDesigns_CardDesignId",
                table: "CardOrders");

            migrationBuilder.DropTable(
                name: "CardDesigns");

            migrationBuilder.DropIndex(
                name: "IX_CardOrders_CardDesignId",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "CardDesignId",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "QuantityPerEmployee",
                table: "CardOrders");

            migrationBuilder.AddColumn<string>(
                name: "BackDesignUrl",
                table: "CardOrders",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CardDesignType",
                table: "CardOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CardName",
                table: "CardOrders",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "CardPackageId",
                table: "CardOrders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "CardTypeId",
                table: "CardOrders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "DeliveryMethod",
                table: "CardOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ExcelDataUrl",
                table: "CardOrders",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FrontDesignUrl",
                table: "CardOrders",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddress",
                table: "CardOrders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CardOrders_CardPackageId",
                table: "CardOrders",
                column: "CardPackageId");

            migrationBuilder.CreateIndex(
                name: "IX_CardOrders_CardTypeId",
                table: "CardOrders",
                column: "CardTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_CardOrders_CardPackages_CardPackageId",
                table: "CardOrders",
                column: "CardPackageId",
                principalTable: "CardPackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CardOrders_CardTypes_CardTypeId",
                table: "CardOrders",
                column: "CardTypeId",
                principalTable: "CardTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
