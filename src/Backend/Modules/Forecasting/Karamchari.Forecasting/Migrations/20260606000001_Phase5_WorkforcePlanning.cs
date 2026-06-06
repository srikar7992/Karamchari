using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Forecasting.Migrations
{
    /// <inheritdoc />
    public partial class Phase5WorkforcePlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WFP_EmployeeIndex",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LocationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SkillCodes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WFP_EmployeeIndex", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WFP_EmployeeIndex_TenantId",
                schema: "__tenant__",
                table: "WFP_EmployeeIndex",
                column: "TenantId");

            migrationBuilder.CreateTable(
                name: "WFP_Projections",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LocationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SkillCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TotalEmployees = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    HistoricalAbsenteeismRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    AttendanceReliabilityScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false, defaultValue: 0.95m),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WFP_Projections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WFP_Projections_TenantId_LocationCode_SkillCode",
                schema: "__tenant__",
                table: "WFP_Projections",
                columns: new[] { "TenantId", "LocationCode", "SkillCode" },
                unique: true);

            migrationBuilder.CreateTable(
                name: "WFP_ApprovedLeaves",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SkillCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsCancelled = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WFP_ApprovedLeaves", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WFP_ApprovedLeaves_RequestId",
                schema: "__tenant__",
                table: "WFP_ApprovedLeaves",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_WFP_ApprovedLeaves_EmployeeId",
                schema: "__tenant__",
                table: "WFP_ApprovedLeaves",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WFP_ApprovedLeaves_TenantId_LocationCode_SkillCode_StartDate_EndDate",
                schema: "__tenant__",
                table: "WFP_ApprovedLeaves",
                columns: new[] { "TenantId", "LocationCode", "SkillCode", "StartDate", "EndDate" });

            migrationBuilder.CreateTable(
                name: "WFP_DemandForecasts",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ForecastDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LocationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SkillCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RequiredHours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    RequiredHeadcount = table.Column<int>(type: "int", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Confidence = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Horizon = table.Column<int>(type: "int", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WFP_DemandForecasts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WFP_DemandForecasts_TenantId_LocationCode_SkillCode_ForecastDate_Horizon",
                schema: "__tenant__",
                table: "WFP_DemandForecasts",
                columns: new[] { "TenantId", "LocationCode", "SkillCode", "ForecastDate", "Horizon" });

            migrationBuilder.CreateTable(
                name: "WFP_SupplyForecasts",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ForecastDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LocationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SkillCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TotalHeadcount = table.Column<int>(type: "int", nullable: false),
                    ApprovedLeaveCount = table.Column<int>(type: "int", nullable: false),
                    ExpectedAbsent = table.Column<int>(type: "int", nullable: false),
                    ExpectedHeadcount = table.Column<int>(type: "int", nullable: false),
                    ExpectedHours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    ReliabilityScore = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    Horizon = table.Column<int>(type: "int", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WFP_SupplyForecasts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WFP_SupplyForecasts_TenantId_LocationCode_SkillCode_ForecastDate_Horizon",
                schema: "__tenant__",
                table: "WFP_SupplyForecasts",
                columns: new[] { "TenantId", "LocationCode", "SkillCode", "ForecastDate", "Horizon" });

            migrationBuilder.CreateTable(
                name: "WFP_CoverageRisks",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RiskDate = table.Column<DateOnly>(type: "date", nullable: false),
                    LocationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SkillCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DemandHeadcount = table.Column<int>(type: "int", nullable: false),
                    SupplyHeadcount = table.Column<int>(type: "int", nullable: false),
                    Gap = table.Column<int>(type: "int", nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Horizon = table.Column<int>(type: "int", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WFP_CoverageRisks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WFP_CoverageRisks_TenantId_LocationCode_SkillCode_RiskDate_Horizon",
                schema: "__tenant__",
                table: "WFP_CoverageRisks",
                columns: new[] { "TenantId", "LocationCode", "SkillCode", "RiskDate", "Horizon" });

            migrationBuilder.CreateIndex(
                name: "IX_WFP_CoverageRisks_TenantId_RiskLevel",
                schema: "__tenant__",
                table: "WFP_CoverageRisks",
                columns: new[] { "TenantId", "RiskLevel" });

            migrationBuilder.CreateTable(
                name: "WFP_HiringGaps",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PlanningPeriod = table.Column<DateOnly>(type: "date", nullable: false),
                    LocationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SkillCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RequiredHeadcount = table.Column<int>(type: "int", nullable: false),
                    AvailableHeadcount = table.Column<int>(type: "int", nullable: false),
                    GapHeadcount = table.Column<int>(type: "int", nullable: false),
                    Horizon = table.Column<int>(type: "int", nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WFP_HiringGaps", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WFP_HiringGaps_TenantId_LocationCode_SkillCode_PlanningPeriod_Horizon",
                schema: "__tenant__",
                table: "WFP_HiringGaps",
                columns: new[] { "TenantId", "LocationCode", "SkillCode", "PlanningPeriod", "Horizon" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "WFP_HiringGaps", schema: "__tenant__");
            migrationBuilder.DropTable(name: "WFP_CoverageRisks", schema: "__tenant__");
            migrationBuilder.DropTable(name: "WFP_SupplyForecasts", schema: "__tenant__");
            migrationBuilder.DropTable(name: "WFP_DemandForecasts", schema: "__tenant__");
            migrationBuilder.DropTable(name: "WFP_ApprovedLeaves", schema: "__tenant__");
            migrationBuilder.DropTable(name: "WFP_Projections", schema: "__tenant__");
            migrationBuilder.DropTable(name: "WFP_EmployeeIndex", schema: "__tenant__");
        }
    }
}
