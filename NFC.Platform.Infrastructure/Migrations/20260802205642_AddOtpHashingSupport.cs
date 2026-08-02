using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOtpHashingSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OtpCode",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeliveryOtp",
                table: "CardOrders");

            migrationBuilder.AddColumn<string>(
                name: "OtpHash",
                table: "Users",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryOtpHash",
                table: "CardOrders",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OtpHash",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DeliveryOtpHash",
                table: "CardOrders");

            migrationBuilder.AddColumn<string>(
                name: "OtpCode",
                table: "Users",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryOtp",
                table: "CardOrders",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
