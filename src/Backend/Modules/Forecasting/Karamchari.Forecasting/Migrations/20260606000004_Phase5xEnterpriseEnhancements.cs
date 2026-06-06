using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Forecasting.Migrations
{
    /// <inheritdoc />
    public partial class Phase5xEnterpriseEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── WFP_EmployeeIndex: add DoB and ContractEndDate for WFP enrichment ──
            migrationBuilder.AddColumn<DateOnly>(
                name: "DateOfBirth",
                schema: "__tenant__",
                table: "WFP_EmployeeIndex",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ContractEndDate",
                schema: "__tenant__",
                table: "WFP_EmployeeIndex",
                type: "date",
                nullable: true);

            // ── WFP_SupplyForecasts: add breakdown columns ────────────────────────
            migrationBuilder.AddColumn<int>(
                name: "FutureHireAddition",
                schema: "__tenant__",
                table: "WFP_SupplyForecasts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ContractExpiryDeduction",
                schema: "__tenant__",
                table: "WFP_SupplyForecasts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RetirementDeduction",
                schema: "__tenant__",
                table: "WFP_SupplyForecasts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            // ── WFP_ForecastAccuracies: add RMSE and bias columns ────────────────
            migrationBuilder.AddColumn<int>(
                name: "SignedError",
                schema: "__tenant__",
                table: "WFP_ForecastAccuracies",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "SquaredError",
                schema: "__tenant__",
                table: "WFP_ForecastAccuracies",
                type: "decimal(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            // ── WFP_FutureHires ───────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "WFP_FutureHires",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OfferId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LocationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SkillCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpectedStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsOnboarded = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WFP_FutureHires", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WFP_FutureHires_OfferId",
                schema: "__tenant__",
                table: "WFP_FutureHires",
                column: "OfferId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WFP_FutureHires_TenantId_LocationCode_SkillCode_ExpectedStartDate_IsOnboarded",
                schema: "__tenant__",
                table: "WFP_FutureHires",
                columns: new[] { "TenantId", "LocationCode", "SkillCode", "ExpectedStartDate", "IsOnboarded" });

            // ── WFP_ContractExpiries ──────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "WFP_ContractExpiries",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LocationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SkillCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ContractEndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsTerminated = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WFP_ContractExpiries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WFP_ContractExpiries_EmployeeId",
                schema: "__tenant__",
                table: "WFP_ContractExpiries",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WFP_ContractExpiries_TenantId_LocationCode_SkillCode_ContractEndDate_IsTerminated",
                schema: "__tenant__",
                table: "WFP_ContractExpiries",
                columns: new[] { "TenantId", "LocationCode", "SkillCode", "ContractEndDate", "IsTerminated" });

            // ── WFP_RetirementRisks ───────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "WFP_RetirementRisks",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LocationCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SkillCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpectedRetirementDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsRetired = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WFP_RetirementRisks", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WFP_RetirementRisks_EmployeeId",
                schema: "__tenant__",
                table: "WFP_RetirementRisks",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WFP_RetirementRisks_TenantId_LocationCode_SkillCode_ExpectedRetirementDate_IsRetired",
                schema: "__tenant__",
                table: "WFP_RetirementRisks",
                columns: new[] { "TenantId", "LocationCode", "SkillCode", "ExpectedRetirementDate", "IsRetired" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "WFP_RetirementRisks", schema: "__tenant__");
            migrationBuilder.DropTable(name: "WFP_ContractExpiries", schema: "__tenant__");
            migrationBuilder.DropTable(name: "WFP_FutureHires", schema: "__tenant__");

            migrationBuilder.DropColumn(name: "SquaredError", schema: "__tenant__", table: "WFP_ForecastAccuracies");
            migrationBuilder.DropColumn(name: "SignedError", schema: "__tenant__", table: "WFP_ForecastAccuracies");

            migrationBuilder.DropColumn(name: "RetirementDeduction", schema: "__tenant__", table: "WFP_SupplyForecasts");
            migrationBuilder.DropColumn(name: "ContractExpiryDeduction", schema: "__tenant__", table: "WFP_SupplyForecasts");
            migrationBuilder.DropColumn(name: "FutureHireAddition", schema: "__tenant__", table: "WFP_SupplyForecasts");

            migrationBuilder.DropColumn(name: "ContractEndDate", schema: "__tenant__", table: "WFP_EmployeeIndex");
            migrationBuilder.DropColumn(name: "DateOfBirth", schema: "__tenant__", table: "WFP_EmployeeIndex");
        }
    }
}
