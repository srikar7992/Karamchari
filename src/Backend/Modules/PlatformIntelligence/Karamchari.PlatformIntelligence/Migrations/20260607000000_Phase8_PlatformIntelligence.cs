using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.PlatformIntelligence.Migrations
{
    /// <inheritdoc />
    public partial class Phase8_PlatformIntelligence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Platform_Decisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ScopeKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RecommendationsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConflictsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Platform_Decisions", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Platform_Decisions_TenantId_ScopeKey_GeneratedAt",
                table: "Platform_Decisions",
                columns: new[] { "TenantId", "ScopeKey", "GeneratedAt" });

            migrationBuilder.CreateTable(
                name: "Platform_Scenarios",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConstraintsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResultJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CoverageScore = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: true),
                    OvertimeRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    EstimatedCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsOptimal = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EvaluatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Platform_Scenarios", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Platform_Scenarios_TenantId_IsOptimal",
                table: "Platform_Scenarios",
                columns: new[] { "TenantId", "IsOptimal" });

            migrationBuilder.CreateIndex(
                name: "IX_Platform_Scenarios_TenantId_Status",
                table: "Platform_Scenarios",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateTable(
                name: "Platform_SimulationRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParametersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OutcomeJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RequestedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Platform_SimulationRuns", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Platform_SimulationRuns_TenantId_CreatedAt",
                table: "Platform_SimulationRuns",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateTable(
                name: "Platform_Optimizations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ScopeKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ObjectiveType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ConstraintsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RecommendationsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EstimatedAnnualSavings = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Platform_Optimizations", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Platform_Optimizations_TenantId_ScopeKey_GeneratedAt",
                table: "Platform_Optimizations",
                columns: new[] { "TenantId", "ScopeKey", "GeneratedAt" });

            migrationBuilder.CreateTable(
                name: "Platform_ExecutiveDigests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Period = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PeriodDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HeadlineJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetricSummaryJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TopRisksJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TopRecommendationsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_Platform_ExecutiveDigests", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Platform_ExecutiveDigests_TenantId_Period_PeriodDate",
                table: "Platform_ExecutiveDigests",
                columns: new[] { "TenantId", "Period", "PeriodDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Platform_ExecutiveDigests");
            migrationBuilder.DropTable(name: "Platform_Optimizations");
            migrationBuilder.DropTable(name: "Platform_SimulationRuns");
            migrationBuilder.DropTable(name: "Platform_Scenarios");
            migrationBuilder.DropTable(name: "Platform_Decisions");
        }
    }
}
