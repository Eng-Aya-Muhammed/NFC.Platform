using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeImportJobsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeImportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ExcelFileUrl = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ExcelFilePublicId = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CardType = table.Column<int>(type: "int", nullable: false),
                    CardDesignType = table.Column<int>(type: "int", nullable: false),
                    PrintTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TotalRows = table.Column<int>(type: "int", nullable: false),
                    Imported = table.Column<int>(type: "int", nullable: false),
                    Skipped = table.Column<int>(type: "int", nullable: false),
                    ErrorsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CardOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeImportJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeImportJobs_CardOrders_CardOrderId",
                        column: x => x.CardOrderId,
                        principalTable: "CardOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_EmployeeImportJobs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeImportJobs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeImportJobs_CardOrderId",
                table: "EmployeeImportJobs",
                column: "CardOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeImportJobs_TenantId",
                table: "EmployeeImportJobs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeImportJobs_UserId",
                table: "EmployeeImportJobs",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeImportJobs");
        }
    }
}
