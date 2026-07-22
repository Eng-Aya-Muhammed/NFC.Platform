using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NFC.Platform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionTemplateLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomDesignRequestsUsed",
                table: "UserSubscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TemplateChangesUsed",
                table: "UserSubscriptions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxCustomDesignRequests",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxTemplateChanges",
                table: "SubscriptionPlans",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SubscriptionPlanTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubscriptionPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardTemplateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlanTemplates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionPlanTemplates_CardTemplates_CardTemplateId",
                        column: x => x.CardTemplateId,
                        principalTable: "CardTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubscriptionPlanTemplates_SubscriptionPlans_SubscriptionPlanId",
                        column: x => x.SubscriptionPlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlanTemplates_CardTemplateId",
                table: "SubscriptionPlanTemplates",
                column: "CardTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlanTemplates_SubscriptionPlanId_CardTemplateId",
                table: "SubscriptionPlanTemplates",
                columns: new[] { "SubscriptionPlanId", "CardTemplateId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubscriptionPlanTemplates");

            migrationBuilder.DropColumn(
                name: "CustomDesignRequestsUsed",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "TemplateChangesUsed",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "MaxCustomDesignRequests",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "MaxTemplateChanges",
                table: "SubscriptionPlans");
        }
    }
}
