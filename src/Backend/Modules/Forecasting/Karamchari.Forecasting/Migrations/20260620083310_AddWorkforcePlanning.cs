using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Forecasting.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkforcePlanning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Forecasting_HeadcountVariances",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ScenarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalYear = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    PlannedHeadcount = table.Column<int>(type: "int", nullable: false),
                    ActualHeadcount = table.Column<int>(type: "int", nullable: false),
                    ComputedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Forecasting_HeadcountVariances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Forecasting_Scenarios",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastProjectedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Forecasting_Scenarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Forecasting_HeadcountPlans",
                schema: "__tenant__",
                columns: table => new
                {
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FiscalYear = table.Column<int>(type: "int", nullable: false),
                    ForecastScenarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlannedHeadcount = table.Column<int>(type: "int", nullable: false),
                    ApprovedHeadcount = table.Column<int>(type: "int", nullable: false),
                    TotalBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AvgSalary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Forecasting_HeadcountPlans", x => new { x.ForecastScenarioId, x.DepartmentId, x.FiscalYear });
                    table.ForeignKey(
                        name: "FK_Forecasting_HeadcountPlans_Forecasting_Scenarios_ForecastScenarioId",
                        column: x => x.ForecastScenarioId,
                        principalSchema: "__tenant__",
                        principalTable: "Forecasting_Scenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Forecasting_ScenarioAssumptions",
                schema: "__tenant__",
                columns: table => new
                {
                    Type = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ForecastScenarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Forecasting_ScenarioAssumptions", x => new { x.ForecastScenarioId, x.Type });
                    table.ForeignKey(
                        name: "FK_Forecasting_ScenarioAssumptions_Forecasting_Scenarios_ForecastScenarioId",
                        column: x => x.ForecastScenarioId,
                        principalSchema: "__tenant__",
                        principalTable: "Forecasting_Scenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Forecasting_ScenarioProjections",
                schema: "__tenant__",
                columns: table => new
                {
                    Month = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ForecastScenarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectedHeadcount = table.Column<int>(type: "int", nullable: false),
                    ProjectedPayroll = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ProjectedHires = table.Column<int>(type: "int", nullable: false),
                    ProjectedAttrition = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Forecasting_ScenarioProjections", x => new { x.ForecastScenarioId, x.Month });
                    table.ForeignKey(
                        name: "FK_Forecasting_ScenarioProjections_Forecasting_Scenarios_ForecastScenarioId",
                        column: x => x.ForecastScenarioId,
                        principalSchema: "__tenant__",
                        principalTable: "Forecasting_Scenarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Forecasting_HeadcountVariances_TenantId_ScenarioId_FiscalYear_Month",
                schema: "__tenant__",
                table: "Forecasting_HeadcountVariances",
                columns: new[] { "TenantId", "ScenarioId", "FiscalYear", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_Forecasting_Scenarios_TenantId_Type_Status",
                schema: "__tenant__",
                table: "Forecasting_Scenarios",
                columns: new[] { "TenantId", "Type", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Forecasting_HeadcountPlans",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Forecasting_HeadcountVariances",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Forecasting_ScenarioAssumptions",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Forecasting_ScenarioProjections",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Forecasting_Scenarios",
                schema: "__tenant__");
        }
    }
}
