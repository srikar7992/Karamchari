using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.PSA.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "__tenant__");

            migrationBuilder.CreateTable(
                name: "PSA_Clients",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Gstin = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    BillingAddress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PSA_Clients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PSA_EmployeeCostSnapshots",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MonthlyCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CostPerHour = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    StandardMonthlyHours = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PSA_EmployeeCostSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PSA_Invoices",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    InvoiceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CutoffDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CgstRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SgstRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IgstRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    IsFinalized = table.Column<bool>(type: "bit", nullable: false),
                    PdfPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Lines = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PSA_Invoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PSA_ProfitLedger",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimesheetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Revenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BillableRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CostPerHour = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PSA_ProfitLedger", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PSA_ProjectMonthlyMetrics",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    TotalRevenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BillableHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LastUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PSA_ProjectMonthlyMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PSA_ProjectResources",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillableRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsBillable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PSA_ProjectResources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PSA_Projects",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    BillingType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PSA_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PSA_UnbilledRevenue",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TimesheetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillableHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    BillableRate = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsInvoiced = table.Column<bool>(type: "bit", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PSA_UnbilledRevenue", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PSA_Clients_TenantId",
                schema: "__tenant__",
                table: "PSA_Clients",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PSA_EmployeeCostSnapshots_EmployeeId_Year_Month",
                schema: "__tenant__",
                table: "PSA_EmployeeCostSnapshots",
                columns: new[] { "EmployeeId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PSA_Invoices_ClientId_InvoiceDate",
                schema: "__tenant__",
                table: "PSA_Invoices",
                columns: new[] { "ClientId", "InvoiceDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PSA_ProfitLedger_ProjectId_WorkDate",
                schema: "__tenant__",
                table: "PSA_ProfitLedger",
                columns: new[] { "ProjectId", "WorkDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PSA_ProfitLedger_TimesheetId_EmployeeId_ProjectId_WorkDate",
                schema: "__tenant__",
                table: "PSA_ProfitLedger",
                columns: new[] { "TimesheetId", "EmployeeId", "ProjectId", "WorkDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PSA_ProjectMonthlyMetrics_ProjectId_Year_Month",
                schema: "__tenant__",
                table: "PSA_ProjectMonthlyMetrics",
                columns: new[] { "ProjectId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PSA_ProjectMonthlyMetrics_Year_Month",
                schema: "__tenant__",
                table: "PSA_ProjectMonthlyMetrics",
                columns: new[] { "Year", "Month" });

            migrationBuilder.CreateIndex(
                name: "IX_PSA_ProjectResources_EmployeeId_ProjectId_EffectiveFrom",
                schema: "__tenant__",
                table: "PSA_ProjectResources",
                columns: new[] { "EmployeeId", "ProjectId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_PSA_ProjectResources_ProjectId",
                schema: "__tenant__",
                table: "PSA_ProjectResources",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_PSA_Projects_ClientId",
                schema: "__tenant__",
                table: "PSA_Projects",
                column: "ClientId");

            migrationBuilder.CreateIndex(
                name: "IX_PSA_Projects_TenantId_IsActive",
                schema: "__tenant__",
                table: "PSA_Projects",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_PSA_UnbilledRevenue_ProjectId_IsInvoiced",
                schema: "__tenant__",
                table: "PSA_UnbilledRevenue",
                columns: new[] { "ProjectId", "IsInvoiced" });

            migrationBuilder.CreateIndex(
                name: "IX_PSA_UnbilledRevenue_TimesheetId_EmployeeId_ProjectId_WorkDate",
                schema: "__tenant__",
                table: "PSA_UnbilledRevenue",
                columns: new[] { "TimesheetId", "EmployeeId", "ProjectId", "WorkDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PSA_Clients",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "PSA_EmployeeCostSnapshots",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "PSA_Invoices",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "PSA_ProfitLedger",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "PSA_ProjectMonthlyMetrics",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "PSA_ProjectResources",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "PSA_Projects",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "PSA_UnbilledRevenue",
                schema: "__tenant__");
        }
    }
}
