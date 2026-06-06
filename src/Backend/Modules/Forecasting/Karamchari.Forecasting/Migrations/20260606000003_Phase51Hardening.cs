using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Forecasting.Migrations
{
    /// <inheritdoc />
    public partial class Phase51Hardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Expand DayOfWeekAbsenceRatesJson for composite DayOfWeek+Month keys ─
            migrationBuilder.AlterColumn<string>(
                name: "DayOfWeekAbsenceRatesJson",
                schema: "__tenant__",
                table: "WFP_Projections",
                type: "nvarchar(2048)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(256)");

            // ── WFP_SkillSubstitutions ────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "WFP_SkillSubstitutions",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SkillCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SubstituteSkillCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CapacityFraction = table.Column<decimal>(type: "decimal(4,3)", precision: 4, scale: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WFP_SkillSubstitutions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WFP_SkillSubstitutions_TenantId_SkillCode_SubstituteSkillCode",
                schema: "__tenant__",
                table: "WFP_SkillSubstitutions",
                columns: new[] { "TenantId", "SkillCode", "SubstituteSkillCode" },
                unique: true);

            // ── WFP_OvertimePolicies ──────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "WFP_OvertimePolicies",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LocationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StandardWeeklyHours = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MaxLegalWeeklyHours = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WFP_OvertimePolicies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WFP_OvertimePolicies_TenantId_LocationCode",
                schema: "__tenant__",
                table: "WFP_OvertimePolicies",
                columns: new[] { "TenantId", "LocationCode" },
                unique: true);

            // ── WFP_ForecastAccuracies ────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "WFP_ForecastAccuracies",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LocationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SkillCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ForecastDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Horizon = table.Column<int>(type: "int", nullable: false),
                    PredictedHeadcount = table.Column<int>(type: "int", nullable: false),
                    ActualHeadcount = table.Column<int>(type: "int", nullable: false),
                    AbsoluteError = table.Column<int>(type: "int", nullable: false),
                    AbsolutePercentError = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WFP_ForecastAccuracies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WFP_ForecastAccuracies_TenantId_LocationCode_SkillCode_ForecastDate_Horizon",
                schema: "__tenant__",
                table: "WFP_ForecastAccuracies",
                columns: new[] { "TenantId", "LocationCode", "SkillCode", "ForecastDate", "Horizon" });

            migrationBuilder.CreateIndex(
                name: "IX_WFP_ForecastAccuracies_TenantId_ForecastDate",
                schema: "__tenant__",
                table: "WFP_ForecastAccuracies",
                columns: new[] { "TenantId", "ForecastDate" });

            // ── WFP_RecomputeQueue ────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "WFP_RecomputeQueue",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LocationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SkillCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    QueuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WFP_RecomputeQueue", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WFP_RecomputeQueue_TenantId_LocationCode_SkillCode",
                schema: "__tenant__",
                table: "WFP_RecomputeQueue",
                columns: new[] { "TenantId", "LocationCode", "SkillCode" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "WFP_RecomputeQueue", schema: "__tenant__");
            migrationBuilder.DropTable(name: "WFP_ForecastAccuracies", schema: "__tenant__");
            migrationBuilder.DropTable(name: "WFP_OvertimePolicies", schema: "__tenant__");
            migrationBuilder.DropTable(name: "WFP_SkillSubstitutions", schema: "__tenant__");

            migrationBuilder.AlterColumn<string>(
                name: "DayOfWeekAbsenceRatesJson",
                schema: "__tenant__",
                table: "WFP_Projections",
                type: "nvarchar(256)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(2048)");
        }
    }
}
