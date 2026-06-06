using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Intelligence.Migrations;

/// <inheritdoc />
public partial class Phase64_WorkforceDecisionIntelligence : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── Intel_EmployeeScopes ─────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "Intel_EmployeeScopes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SiteCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                DepartmentCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                ManagerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Intel_EmployeeScopes", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Intel_EmployeeScopes_TenantId_EmployeeId",
            table: "Intel_EmployeeScopes",
            columns: new[] { "TenantId", "EmployeeId" },
            unique: true);

        // ── Intel_CoverageFragility ──────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "Intel_CoverageFragility",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                ScopeKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                FragilityScore = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                FragilityLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                DaysUntilCoverageFailure = table.Column<int>(type: "int", nullable: true),
                BurnoutPressureComponent = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                DependencyConcentrationComponent = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                CriticalExposureComponent = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                ScopeEmployeeCount = table.Column<int>(type: "int", nullable: false),
                ComputedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Intel_CoverageFragility", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Intel_CoverageFragility_TenantId_ScopeKey",
            table: "Intel_CoverageFragility",
            columns: new[] { "TenantId", "ScopeKey" },
            unique: true);

        // ── Intel_CausalChains ───────────────────────────────────────────────
        migrationBuilder.CreateTable(
            name: "Intel_CausalChains",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ActiveLinksJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                ActiveLinkCount = table.Column<int>(type: "int", nullable: false),
                ChainSeverity = table.Column<string>(type: "nvarchar(450)", nullable: false),
                ComputedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Intel_CausalChains", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Intel_CausalChains_TenantId_EmployeeId",
            table: "Intel_CausalChains",
            columns: new[] { "TenantId", "EmployeeId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Intel_CausalChains_TenantId_ChainSeverity",
            table: "Intel_CausalChains",
            columns: new[] { "TenantId", "ChainSeverity" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Intel_CausalChains");
        migrationBuilder.DropTable(name: "Intel_CoverageFragility");
        migrationBuilder.DropTable(name: "Intel_EmployeeScopes");
    }
}
