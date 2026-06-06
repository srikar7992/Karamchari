using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Forecasting.Migrations
{
    /// <inheritdoc />
    public partial class Phase52RetirementPolicyAndJoinProbability : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── WFP_FutureHires: add JoiningProbability ───────────────────────
            // Replaces raw COUNT(*) supply contribution with SUM(JoiningProbability),
            // so offer drop-off rates (e.g. 75% graduate joining rate) are reflected in supply.
            migrationBuilder.AddColumn<decimal>(
                name: "JoiningProbability",
                schema: "__tenant__",
                table: "WFP_FutureHires",
                type: "decimal(4,3)",
                precision: 4,
                scale: 3,
                nullable: false,
                defaultValue: 1.0m);

            // ── WFP_RetirementPolicies: configurable retirement age per tenant/role ──
            // Replaces the hardcoded DoB+60 constant in the workforce planning consumer.
            // Lookup order: (TenantId, RoleCode) → (TenantId, NULL) → fallback constant 60.
            migrationBuilder.CreateTable(
                name: "WFP_RetirementPolicies",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    RetirementAge = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WFP_RetirementPolicies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WFP_RetirementPolicies_TenantId_RoleCode",
                schema: "__tenant__",
                table: "WFP_RetirementPolicies",
                columns: new[] { "TenantId", "RoleCode" },
                unique: true,
                filter: "[RoleCode] IS NOT NULL");

            // Separate index for the tenant-default policy row (RoleCode IS NULL)
            migrationBuilder.CreateIndex(
                name: "IX_WFP_RetirementPolicies_TenantId_DefaultPolicy",
                schema: "__tenant__",
                table: "WFP_RetirementPolicies",
                columns: new[] { "TenantId" },
                unique: true,
                filter: "[RoleCode] IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "WFP_RetirementPolicies", schema: "__tenant__");
            migrationBuilder.DropColumn(name: "JoiningProbability", schema: "__tenant__", table: "WFP_FutureHires");
        }
    }
}
