using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Intelligence.Migrations
{
    /// <inheritdoc />
    public partial class Phase61_WorkforceIntelligenceMaturity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Intel_ScoreSnapshots ─────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Intel_ScoreSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScoreType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intel_ScoreSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Intel_ScoreSnapshots_TenantId_EmployeeId_ScoreType_CalculatedAt",
                table: "Intel_ScoreSnapshots",
                columns: new[] { "TenantId", "EmployeeId", "ScoreType", "CalculatedAt" });

            // ── Intel_Forecasts ──────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Intel_Forecasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScoreType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CurrentScore = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    ProjectedScore = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    ForecastHorizonDays = table.Column<int>(type: "int", nullable: false),
                    PointsPerDay = table.Column<decimal>(type: "decimal(8,3)", precision: 8, scale: 3, nullable: false),
                    DaysUntilCritical = table.Column<int>(type: "int", nullable: true),
                    Trend = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DataPointsUsed = table.Column<int>(type: "int", nullable: false),
                    Confidence = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intel_Forecasts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Intel_Forecasts_TenantId_EmployeeId_ScoreType",
                table: "Intel_Forecasts",
                columns: new[] { "TenantId", "EmployeeId", "ScoreType" },
                unique: true);

            // ── Intel_TalentRiskScores ───────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Intel_TalentRiskScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BurnoutComponent = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    AttritionComponent = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    DependencyComponent = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    CompoundPenalty = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intel_TalentRiskScores", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Intel_TalentRiskScores_TenantId_EmployeeId",
                table: "Intel_TalentRiskScores",
                columns: new[] { "TenantId", "EmployeeId" },
                unique: true);

            // ── Intel_WorkloadFairness ───────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Intel_WorkloadFairness",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SiteCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    OverallScore = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    OtGiniCoefficient = table.Column<decimal>(type: "decimal(8,4)", precision: 8, scale: 4, nullable: false),
                    OtConcentrationTop10Pct = table.Column<decimal>(type: "decimal(8,4)", precision: 8, scale: 4, nullable: false),
                    TeamSize = table.Column<int>(type: "int", nullable: false),
                    AvgOtHoursPerMember = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    MaxOtHoursAnyMember = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intel_WorkloadFairness", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Intel_WorkloadFairness_TenantId_SiteCode_TeamId",
                table: "Intel_WorkloadFairness",
                columns: new[] { "TenantId", "SiteCode", "TeamId" },
                unique: true,
                filter: "[TeamId] IS NOT NULL");

            // ── Intel_AbsenceContagion ───────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Intel_AbsenceContagion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SiteCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CurrentAbsenceRate = table.Column<decimal>(type: "decimal(8,4)", precision: 8, scale: 4, nullable: false),
                    BaselineAbsenceRate = table.Column<decimal>(type: "decimal(8,4)", precision: 8, scale: 4, nullable: false),
                    ZScore = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TeamSize = table.Column<int>(type: "int", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intel_AbsenceContagion", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Intel_AbsenceContagion_TenantId_SiteCode_TeamId",
                table: "Intel_AbsenceContagion",
                columns: new[] { "TenantId", "SiteCode", "TeamId" },
                unique: true,
                filter: "[TeamId] IS NOT NULL");

            // ── Intel_FeatureSnapshots ───────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Intel_FeatureSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SnapshotDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ConsecutiveWorkDays = table.Column<int>(type: "int", nullable: false),
                    OvertimeHours28d = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    DaysWithoutLeave = table.Column<int>(type: "int", nullable: false),
                    LateArrivalsCurrentMonth = table.Column<int>(type: "int", nullable: false),
                    ShiftSwaps30d = table.Column<int>(type: "int", nullable: false),
                    HighIntensityShiftRatio = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    EmergencyFillIns90d = table.Column<int>(type: "int", nullable: false),
                    LateArrivalSlope = table.Column<decimal>(type: "decimal(8,3)", precision: 8, scale: 3, nullable: false),
                    LeaveFrequencyRatio = table.Column<decimal>(type: "decimal(8,3)", precision: 8, scale: 3, nullable: false),
                    SickLeaveDays30d = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    OvertimeRejections30d = table.Column<int>(type: "int", nullable: false),
                    PeerAttendanceGap = table.Column<decimal>(type: "decimal(8,2)", precision: 8, scale: 2, nullable: false),
                    ManagerFrictionScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    BurnoutScore = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    AttritionScore = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    TalentRiskScore = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    BurnoutRiskLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttritionRiskLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intel_FeatureSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Intel_FeatureSnapshots_TenantId_EmployeeId_SnapshotDate",
                table: "Intel_FeatureSnapshots",
                columns: new[] { "TenantId", "EmployeeId", "SnapshotDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Intel_FeatureSnapshots_TenantId_SnapshotDate",
                table: "Intel_FeatureSnapshots",
                columns: new[] { "TenantId", "SnapshotDate" });

            // ── Intel_OutcomeLabels ──────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Intel_OutcomeLabels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Outcome = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OutcomeDate = table.Column<DateOnly>(type: "date", nullable: false),
                    BurnoutScoreAtOutcome = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    AttritionScoreAtOutcome = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intel_OutcomeLabels", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Intel_OutcomeLabels_TenantId_EmployeeId_Outcome_OutcomeDate",
                table: "Intel_OutcomeLabels",
                columns: new[] { "TenantId", "EmployeeId", "Outcome", "OutcomeDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Intel_OutcomeLabels");
            migrationBuilder.DropTable(name: "Intel_FeatureSnapshots");
            migrationBuilder.DropTable(name: "Intel_AbsenceContagion");
            migrationBuilder.DropTable(name: "Intel_WorkloadFairness");
            migrationBuilder.DropTable(name: "Intel_TalentRiskScores");
            migrationBuilder.DropTable(name: "Intel_Forecasts");
            migrationBuilder.DropTable(name: "Intel_ScoreSnapshots");
        }
    }
}
