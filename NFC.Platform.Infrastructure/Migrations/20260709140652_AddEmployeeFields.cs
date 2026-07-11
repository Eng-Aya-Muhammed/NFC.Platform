using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CardOrders_Companies_CompanyId",
                table: "CardOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfileMetrics_UserProfiles_UserProfileId",
                table: "ProfileMetrics");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProfiles_CardTemplates_CardTemplateId",
                table: "UserProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSubscriptions_Companies_CompanyId",
                table: "UserSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_CompanyId",
                table: "UserSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Users_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_Username",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Cards_ActivationCode",
                table: "Cards");

            migrationBuilder.DropIndex(
                name: "IX_CardOrders_CompanyId",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "CardOrders");

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "UserSubscriptions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Users",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Bio",
                table: "UserProfiles",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "UserProfiles",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "UserProfiles",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "RefreshTokens",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ProfileMetrics",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "ProfileLinks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Companies",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "CardTemplates",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Cards",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "CardOrders",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "JobTitle",
                table: "CardOrderItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EmployeeName",
                table: "CardOrderItems",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "Department",
                table: "CardOrderItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ActivationCode",
                table: "CardOrderItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "CardOrderItems",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_TenantId",
                table: "UserSubscriptions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_Email",
                table: "Users",
                columns: ["TenantId", "Email"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_Username",
                table: "Users",
                columns: ["TenantId", "Username"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserProfiles_TenantId",
                table: "UserProfiles",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TenantId_UserId",
                table: "RefreshTokens",
                columns: ["TenantId", "UserId"]);

            migrationBuilder.CreateIndex(
                name: "IX_ProfileMetrics_TenantId",
                table: "ProfileMetrics",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileLinks_TenantId",
                table: "ProfileLinks",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_TenantId",
                table: "Companies",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CardTemplates_TenantId",
                table: "CardTemplates",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_TenantId_ActivationCode",
                table: "Cards",
                columns: ["TenantId", "ActivationCode"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CardOrders_TenantId",
                table: "CardOrders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CardOrderItems_ActivationCode",
                table: "CardOrderItems",
                column: "ActivationCode");

            migrationBuilder.CreateIndex(
                name: "IX_CardOrderItems_TenantId",
                table: "CardOrderItems",
                column: "TenantId");

            migrationBuilder.AddForeignKey(
                name: "FK_CardOrderItems_Tenants_TenantId",
                table: "CardOrderItems",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CardOrders_Tenants_TenantId",
                table: "CardOrders",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_Tenants_TenantId",
                table: "Cards",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CardTemplates_Tenants_TenantId",
                table: "CardTemplates",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_Tenants_TenantId",
                table: "Companies",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfileLinks_Tenants_TenantId",
                table: "ProfileLinks",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfileMetrics_Tenants_TenantId",
                table: "ProfileMetrics",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfileMetrics_UserProfiles_UserProfileId",
                table: "ProfileMetrics",
                column: "UserProfileId",
                principalTable: "UserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfiles_CardTemplates_CardTemplateId",
                table: "UserProfiles",
                column: "CardTemplateId",
                principalTable: "CardTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfiles_Tenants_TenantId",
                table: "UserProfiles",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSubscriptions_Tenants_TenantId",
                table: "UserSubscriptions",
                column: "TenantId",
                principalTable: "Tenants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CardOrderItems_Tenants_TenantId",
                table: "CardOrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_CardOrders_Tenants_TenantId",
                table: "CardOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_Cards_Tenants_TenantId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_CardTemplates_Tenants_TenantId",
                table: "CardTemplates");

            migrationBuilder.DropForeignKey(
                name: "FK_Companies_Tenants_TenantId",
                table: "Companies");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfileLinks_Tenants_TenantId",
                table: "ProfileLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfileMetrics_Tenants_TenantId",
                table: "ProfileMetrics");

            migrationBuilder.DropForeignKey(
                name: "FK_ProfileMetrics_UserProfiles_UserProfileId",
                table: "ProfileMetrics");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProfiles_CardTemplates_CardTemplateId",
                table: "UserProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserProfiles_Tenants_TenantId",
                table: "UserProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Tenants_TenantId",
                table: "Users");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSubscriptions_Tenants_TenantId",
                table: "UserSubscriptions");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_UserSubscriptions_TenantId",
                table: "UserSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId_Email",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId_Username",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_UserProfiles_TenantId",
                table: "UserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_TenantId_UserId",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_ProfileMetrics_TenantId",
                table: "ProfileMetrics");

            migrationBuilder.DropIndex(
                name: "IX_ProfileLinks_TenantId",
                table: "ProfileLinks");

            migrationBuilder.DropIndex(
                name: "IX_Companies_TenantId",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_CardTemplates_TenantId",
                table: "CardTemplates");

            migrationBuilder.DropIndex(
                name: "IX_Cards_TenantId_ActivationCode",
                table: "Cards");

            migrationBuilder.DropIndex(
                name: "IX_CardOrders_TenantId",
                table: "CardOrders");

            migrationBuilder.DropIndex(
                name: "IX_CardOrderItems_ActivationCode",
                table: "CardOrderItems");

            migrationBuilder.DropIndex(
                name: "IX_CardOrderItems_TenantId",
                table: "CardOrderItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Bio",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ProfileMetrics");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ProfileLinks");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CardTemplates");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CardOrders");

            migrationBuilder.DropColumn(
                name: "ActivationCode",
                table: "CardOrderItems");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CardOrderItems");

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "UserSubscriptions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                table: "CardOrders",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "JobTitle",
                table: "CardOrderItems",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "EmployeeName",
                table: "CardOrderItems",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Department",
                table: "CardOrderItems",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptions_CompanyId",
                table: "UserSubscriptions",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cards_ActivationCode",
                table: "Cards",
                column: "ActivationCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CardOrders_CompanyId",
                table: "CardOrders",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_CardOrders_Companies_CompanyId",
                table: "CardOrders",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProfileMetrics_UserProfiles_UserProfileId",
                table: "ProfileMetrics",
                column: "UserProfileId",
                principalTable: "UserProfiles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfiles_CardTemplates_CardTemplateId",
                table: "UserProfiles",
                column: "CardTemplateId",
                principalTable: "CardTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSubscriptions_Companies_CompanyId",
                table: "UserSubscriptions",
                column: "CompanyId",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
