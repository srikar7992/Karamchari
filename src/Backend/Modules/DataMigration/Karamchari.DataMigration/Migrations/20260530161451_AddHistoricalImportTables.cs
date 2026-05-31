using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.DataMigration.Migrations
{
    /// <inheritdoc />
    public partial class AddHistoricalImportTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Migration_HistoricalPayrollSummaries",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ImportJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    GrossPay = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Deductions = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    NetPay = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Migration_HistoricalPayrollSummaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Migration_HistoricalSalaryRecords",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ImportJobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ComponentName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ImportedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Migration_HistoricalSalaryRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Migration_HistoricalPayrollSummaries_TenantId_EmployeeNumber_Year_Month",
                schema: "__tenant__",
                table: "Migration_HistoricalPayrollSummaries",
                columns: new[] { "TenantId", "EmployeeNumber", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Migration_HistoricalPayrollSummaries_TenantId_ImportJobId",
                schema: "__tenant__",
                table: "Migration_HistoricalPayrollSummaries",
                columns: new[] { "TenantId", "ImportJobId" });

            migrationBuilder.CreateIndex(
                name: "IX_Migration_HistoricalSalaryRecords_TenantId_EmployeeNumber",
                schema: "__tenant__",
                table: "Migration_HistoricalSalaryRecords",
                columns: new[] { "TenantId", "EmployeeNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Migration_HistoricalSalaryRecords_TenantId_ImportJobId",
                schema: "__tenant__",
                table: "Migration_HistoricalSalaryRecords",
                columns: new[] { "TenantId", "ImportJobId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Migration_HistoricalPayrollSummaries",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Migration_HistoricalSalaryRecords",
                schema: "__tenant__");
        }
    }
}
