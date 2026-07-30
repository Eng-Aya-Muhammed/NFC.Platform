using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RenameWhatsAppOtpFieldsToEmailOtp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "WhatsAppOtpExpiresAt",
                table: "Users",
                newName: "OtpExpiresAt");

            migrationBuilder.RenameColumn(
                name: "WhatsAppOtpCode",
                table: "Users",
                newName: "OtpCode");

            migrationBuilder.RenameColumn(
                name: "IsWhatsAppVerified",
                table: "Users",
                newName: "IsEmailVerified");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OtpExpiresAt",
                table: "Users",
                newName: "WhatsAppOtpExpiresAt");

            migrationBuilder.RenameColumn(
                name: "OtpCode",
                table: "Users",
                newName: "WhatsAppOtpCode");

            migrationBuilder.RenameColumn(
                name: "IsEmailVerified",
                table: "Users",
                newName: "IsWhatsAppVerified");
        }
    }
}
