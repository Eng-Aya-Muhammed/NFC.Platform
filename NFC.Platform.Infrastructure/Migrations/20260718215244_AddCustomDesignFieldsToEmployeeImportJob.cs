using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomDesignFieldsToEmployeeImportJob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
