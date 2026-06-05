using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Payroll.Migrations
{
    /// <inheritdoc />
    public partial class Phase4cResultImmutabilityHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndTime",
                schema: "__tenant__",
                table: "ShiftPremiumRules");

            migrationBuilder.DropColumn(
                name: "StartTime",
                schema: "__tenant__",
                table: "ShiftPremiumRules");

            migrationBuilder.DropColumn(
                name: "DailyMultiplier",
                schema: "__tenant__",
                table: "OvertimePolicies");

            migrationBuilder.DropColumn(
                name: "DailyThresholdHours",
                schema: "__tenant__",
                table: "OvertimePolicies");

            migrationBuilder.DropColumn(
                name: "WeeklyMultiplier",
                schema: "__tenant__",
                table: "OvertimePolicies");

            migrationBuilder.DropColumn(
                name: "WeeklyThresholdHours",
                schema: "__tenant__",
                table: "OvertimePolicies");

            migrationBuilder.AddColumn<bool>(
                name: "AppliesToPublicHolidays",
                schema: "__tenant__",
                table: "ShiftPremiumRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AppliesToWeekends",
                schema: "__tenant__",
                table: "ShiftPremiumRules",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PremiumType",
                schema: "__tenant__",
                table: "ShiftPremiumRules",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "WindowEnd",
                schema: "__tenant__",
                table: "ShiftPremiumRules",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "WindowStart",
                schema: "__tenant__",
                table: "ShiftPremiumRules",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rules",
                schema: "__tenant__",
                table: "OvertimePolicies",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmployeePayrollResults",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PeriodName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false),
                    BasePay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OvertimePay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    HolidayPay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ShiftPremium = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AllowancePay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BonusPay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    GrossPay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ProvidentFund = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EmployeeStateInsurance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ProfessionalTax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxDeductedAtSource = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OtherDeductions = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalDeductions = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RetroAdjustments = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ManualAdjustments = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NetPay = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SnapshotId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CalculatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeePayrollResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollApprovals",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Comments = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    TotalGrossApproved = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalNetApproved = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EmployeeCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollApprovals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollPublications",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PayrollRunId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PublishedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    BankFileId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    BankFileReference = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    TotalAmountDispatched = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EmployeeCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollPublications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePayrollResults_PayrollRunId_EmployeeId",
                schema: "__tenant__",
                table: "EmployeePayrollResults",
                columns: new[] { "PayrollRunId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePayrollResults_Status",
                schema: "__tenant__",
                table: "EmployeePayrollResults",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeePayrollResults_TenantId_EmployeeId",
                schema: "__tenant__",
                table: "EmployeePayrollResults",
                columns: new[] { "TenantId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollApprovals_PayrollRunId",
                schema: "__tenant__",
                table: "PayrollApprovals",
                column: "PayrollRunId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPublications_PayrollRunId",
                schema: "__tenant__",
                table: "PayrollPublications",
                column: "PayrollRunId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeePayrollResults",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "PayrollApprovals",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "PayrollPublications",
                schema: "__tenant__");

            migrationBuilder.DropColumn(
                name: "AppliesToPublicHolidays",
                schema: "__tenant__",
                table: "ShiftPremiumRules");

            migrationBuilder.DropColumn(
                name: "AppliesToWeekends",
                schema: "__tenant__",
                table: "ShiftPremiumRules");

            migrationBuilder.DropColumn(
                name: "PremiumType",
                schema: "__tenant__",
                table: "ShiftPremiumRules");

            migrationBuilder.DropColumn(
                name: "WindowEnd",
                schema: "__tenant__",
                table: "ShiftPremiumRules");

            migrationBuilder.DropColumn(
                name: "WindowStart",
                schema: "__tenant__",
                table: "ShiftPremiumRules");

            migrationBuilder.DropColumn(
                name: "Rules",
                schema: "__tenant__",
                table: "OvertimePolicies");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "EndTime",
                schema: "__tenant__",
                table: "ShiftPremiumRules",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "StartTime",
                schema: "__tenant__",
                table: "ShiftPremiumRules",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(0, 0, 0));

            migrationBuilder.AddColumn<decimal>(
                name: "DailyMultiplier",
                schema: "__tenant__",
                table: "OvertimePolicies",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "DailyThresholdHours",
                schema: "__tenant__",
                table: "OvertimePolicies",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WeeklyMultiplier",
                schema: "__tenant__",
                table: "OvertimePolicies",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WeeklyThresholdHours",
                schema: "__tenant__",
                table: "OvertimePolicies",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
