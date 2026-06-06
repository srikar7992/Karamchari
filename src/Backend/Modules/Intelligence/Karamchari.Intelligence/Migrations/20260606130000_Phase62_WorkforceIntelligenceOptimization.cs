using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Intelligence.Migrations
{
    /// <inheritdoc />
    public partial class Phase62_WorkforceIntelligenceOptimization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Intel_ScoreAudits ────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Intel_ScoreAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScoreType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ComponentScoresJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RootCauseRankingJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SignalValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intel_ScoreAudits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Intel_ScoreAudits_TenantId_EmployeeId_ScoreType_CalculatedAt",
                table: "Intel_ScoreAudits",
                columns: new[] { "TenantId", "EmployeeId", "ScoreType", "CalculatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Intel_ScoreAudits_TenantId_CalculatedAt",
                table: "Intel_ScoreAudits",
                columns: new[] { "TenantId", "CalculatedAt" });

            // ── Intel_InterventionOutcomes ───────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Intel_InterventionOutcomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RecommendationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScoreType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ScoreAtRecommendation = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    ScoreAtEvaluation = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    ScoreDelta = table.Column<decimal>(type: "decimal(6,1)", precision: 6, scale: 1, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DaysAfterRecommendation = table.Column<int>(type: "int", nullable: false),
                    EvaluatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intel_InterventionOutcomes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Intel_InterventionOutcomes_TenantId_RecommendationId",
                table: "Intel_InterventionOutcomes",
                columns: new[] { "TenantId", "RecommendationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Intel_InterventionOutcomes_TenantId_EmployeeId",
                table: "Intel_InterventionOutcomes",
                columns: new[] { "TenantId", "EmployeeId" });

            // ── Intel_WorkforceHotspots ──────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Intel_WorkforceHotspots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ScopeKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TotalEmployees = table.Column<int>(type: "int", nullable: false),
                    MeanBurnoutScore = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    MeanAttritionScore = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    BurnoutStdDev = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    AttritionStdDev = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    HighRiskBurnoutCount = table.Column<int>(type: "int", nullable: false),
                    CriticalRiskBurnoutCount = table.Column<int>(type: "int", nullable: false),
                    HighRiskAttritionCount = table.Column<int>(type: "int", nullable: false),
                    CriticalRiskAttritionCount = table.Column<int>(type: "int", nullable: false),
                    HighCriticalBurnoutPct = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    HighCriticalAttritionPct = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    BurnoutSeverity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttritionSeverity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intel_WorkforceHotspots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Intel_WorkforceHotspots_TenantId_ScopeKey",
                table: "Intel_WorkforceHotspots",
                columns: new[] { "TenantId", "ScopeKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Intel_WorkforceHotspots");
            migrationBuilder.DropTable(name: "Intel_InterventionOutcomes");
            migrationBuilder.DropTable(name: "Intel_ScoreAudits");
        }
    }
}
