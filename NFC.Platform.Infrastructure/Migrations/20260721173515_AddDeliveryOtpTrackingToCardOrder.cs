using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryOtpTrackingToCardOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryOtpExpiresAt",
                table: "CardOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveryOtpLastSentAt",
                table: "CardOrders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryOtpResendCount",
                table: "CardOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryOtpExpiresAt",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "DeliveryOtpLastSentAt",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "DeliveryOtpResendCount",
                table: "CardOrders");
        }
    }
}
