using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Forecasting.Migrations
{
    /// <inheritdoc />
    public partial class Phase5v2Intelligence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── New columns on WFP_Projections ────────────────────────────────────
            migrationBuilder.AddColumn<decimal>(
                name: "HistoricalAttritionRatePercent",
                schema: "__tenant__",
                table: "WFP_Projections",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DataPointsCount",
                schema: "__tenant__",
                table: "WFP_Projections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DayOfWeekAbsenceRatesJson",
                schema: "__tenant__",
                table: "WFP_Projections",
                type: "nvarchar(256)",
                nullable: false,
                defaultValue: "{}");

            // ── New columns on WFP_SupplyForecasts ────────────────────────────────
            migrationBuilder.AddColumn<int>(
                name: "SkillExpiryCount",
                schema: "__tenant__",
                table: "WFP_SupplyForecasts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ExpectedAttrition",
                schema: "__tenant__",
                table: "WFP_SupplyForecasts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // ── New columns on WFP_HiringGaps ─────────────────────────────────────
            migrationBuilder.AddColumn<int>(
                name: "OvertimeCapacityHeadcount",
                schema: "__tenant__",
                table: "WFP_HiringGaps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CrossSkillCapacityHeadcount",
                schema: "__tenant__",
                table: "WFP_HiringGaps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "NetHiringNeeded",
                schema: "__tenant__",
                table: "WFP_HiringGaps",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // ── WFP_SkillExpiries ─────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "WFP_SkillExpiries",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LocationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SkillCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WFP_SkillExpiries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WFP_SkillExpiries_EmployeeId",
                schema: "__tenant__",
                table: "WFP_SkillExpiries",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WFP_SkillExpiries_TenantId_LocationCode_SkillCode_ExpiryDate",
                schema: "__tenant__",
                table: "WFP_SkillExpiries",
                columns: new[] { "TenantId", "LocationCode", "SkillCode", "ExpiryDate" });

            // ── WFP_AttendanceSnapshots ───────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "WFP_AttendanceSnapshots",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LocationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SkillCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    ActualHeadcount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ExpectedHeadcount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WFP_AttendanceSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WFP_AttendanceSnapshots_TenantId_LocationCode_SkillCode_Date",
                schema: "__tenant__",
                table: "WFP_AttendanceSnapshots",
                columns: new[] { "TenantId", "LocationCode", "SkillCode", "Date" },
                unique: true);

            // ── WFP_AttritionSnapshots ────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "WFP_AttritionSnapshots",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LocationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SkillCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    TerminationCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WFP_AttritionSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WFP_AttritionSnapshots_TenantId_LocationCode_SkillCode_Year_Month",
                schema: "__tenant__",
                table: "WFP_AttritionSnapshots",
                columns: new[] { "TenantId", "LocationCode", "SkillCode", "Year", "Month" },
                unique: true);

            // ── WFP_ScenarioForecasts ─────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "WFP_ScenarioForecasts",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DemandMultiplier = table.Column<decimal>(type: "decimal(8,4)", precision: 8, scale: 4, nullable: false, defaultValue: 1.0m),
                    AbsenteeismDeltaPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    AttritionDeltaPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    AdditionalHireCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WFP_ScenarioForecasts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WFP_ScenarioForecasts_TenantId",
                schema: "__tenant__",
                table: "WFP_ScenarioForecasts",
                column: "TenantId");

            // ── WFP_ScenarioResults ───────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "WFP_ScenarioResults",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScenarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LocationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SkillCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Horizon = table.Column<int>(type: "int", nullable: false),
                    ScenarioDemandHeadcount = table.Column<int>(type: "int", nullable: false),
                    ScenarioSupplyHeadcount = table.Column<int>(type: "int", nullable: false),
                    ScenarioGap = table.Column<int>(type: "int", nullable: false),
                    ScenarioRiskLevel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    ScenarioNetHiringNeeded = table.Column<int>(type: "int", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WFP_ScenarioResults", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WFP_ScenarioResults_ScenarioId",
                schema: "__tenant__",
                table: "WFP_ScenarioResults",
                column: "ScenarioId");

            migrationBuilder.CreateIndex(
                name: "IX_WFP_ScenarioResults_TenantId_LocationCode_SkillCode_Horizon",
                schema: "__tenant__",
                table: "WFP_ScenarioResults",
                columns: new[] { "TenantId", "LocationCode", "SkillCode", "Horizon" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "WFP_ScenarioResults", schema: "__tenant__");
            migrationBuilder.DropTable(name: "WFP_ScenarioForecasts", schema: "__tenant__");
            migrationBuilder.DropTable(name: "WFP_AttritionSnapshots", schema: "__tenant__");
            migrationBuilder.DropTable(name: "WFP_AttendanceSnapshots", schema: "__tenant__");
            migrationBuilder.DropTable(name: "WFP_SkillExpiries", schema: "__tenant__");

            migrationBuilder.DropColumn(name: "NetHiringNeeded", schema: "__tenant__", table: "WFP_HiringGaps");
            migrationBuilder.DropColumn(name: "CrossSkillCapacityHeadcount", schema: "__tenant__", table: "WFP_HiringGaps");
            migrationBuilder.DropColumn(name: "OvertimeCapacityHeadcount", schema: "__tenant__", table: "WFP_HiringGaps");

            migrationBuilder.DropColumn(name: "ExpectedAttrition", schema: "__tenant__", table: "WFP_SupplyForecasts");
            migrationBuilder.DropColumn(name: "SkillExpiryCount", schema: "__tenant__", table: "WFP_SupplyForecasts");

            migrationBuilder.DropColumn(name: "DayOfWeekAbsenceRatesJson", schema: "__tenant__", table: "WFP_Projections");
            migrationBuilder.DropColumn(name: "DataPointsCount", schema: "__tenant__", table: "WFP_Projections");
            migrationBuilder.DropColumn(name: "HistoricalAttritionRatePercent", schema: "__tenant__", table: "WFP_Projections");
        }
    }
}
