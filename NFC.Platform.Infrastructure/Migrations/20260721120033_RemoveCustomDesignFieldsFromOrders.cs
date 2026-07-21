using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCustomDesignFieldsFromOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DesignNotes",
                table: "EmployeeImportJobs");

            migrationBuilder.DropColumn(
                name: "DesignReferenceUrl",
                table: "EmployeeImportJobs");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "EmployeeImportJobs");

            migrationBuilder.DropColumn(
                name: "DesignNotes",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "DesignReferenceUrl",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "CardOrders");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DesignNotes",
                table: "EmployeeImportJobs",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DesignReferenceUrl",
                table: "EmployeeImportJobs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "EmployeeImportJobs",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DesignNotes",
                table: "CardOrders",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DesignReferenceUrl",
                table: "CardOrders",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "CardOrders",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }
    }
}
