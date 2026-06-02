using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Billing.Migrations
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
                name: "Billing_BillableEntries",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TimesheetEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsVoided = table.Column<bool>(type: "bit", nullable: false),
                    IsBilled = table.Column<bool>(type: "bit", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Billing_BillableEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Billing_CollectionCases",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OutstandingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DaysOutstanding = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CurrentStage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReminderCount = table.Column<int>(type: "int", nullable: false),
                    LastActionAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Billing_CollectionCases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Billing_CollectionPolicies",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstReminderDays = table.Column<int>(type: "int", nullable: false),
                    SecondReminderDays = table.Column<int>(type: "int", nullable: false),
                    EscalationDays = table.Column<int>(type: "int", nullable: false),
                    CriticalDays = table.Column<int>(type: "int", nullable: false),
                    AutoEscalate = table.Column<bool>(type: "bit", nullable: false),
                    EscalationEmailCC = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Billing_CollectionPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Billing_Contracts",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BillingType = table.Column<int>(type: "int", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    BillingCycle = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Billing_Contracts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Billing_EmployeeRoles",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Billing_EmployeeRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Billing_Invoices",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    FinalizedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FinalizedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Billing_Invoices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Billing_ProcessedEventLog",
                schema: "__tenant__",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsumerName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Billing_ProcessedEventLog", x => new { x.EventId, x.ConsumerName });
                });

            migrationBuilder.CreateTable(
                name: "Billing_RateCards",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ContractId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Billing_RateCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Billing_RateCards_Billing_Contracts_ContractId",
                        column: x => x.ContractId,
                        principalSchema: "__tenant__",
                        principalTable: "Billing_Contracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Billing_InvoiceLines",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(9,2)", precision: 9, scale: 2, nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Billing_InvoiceLines", x => new { x.InvoiceId, x.Id });
                    table.ForeignKey(
                        name: "FK_Billing_InvoiceLines_Billing_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "__tenant__",
                        principalTable: "Billing_Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Billing_Payments",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaidAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Billing_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Billing_Payments_Billing_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalSchema: "__tenant__",
                        principalTable: "Billing_Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Billing_BillableEntries_TenantId_ProjectId_WorkDate",
                schema: "__tenant__",
                table: "Billing_BillableEntries",
                columns: new[] { "TenantId", "ProjectId", "WorkDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Billing_BillableEntries_TimesheetEntryId",
                schema: "__tenant__",
                table: "Billing_BillableEntries",
                column: "TimesheetEntryId",
                unique: true,
                filter: "[IsVoided] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Billing_CollectionCases_TenantId_InvoiceId",
                schema: "__tenant__",
                table: "Billing_CollectionCases",
                columns: new[] { "TenantId", "InvoiceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Billing_CollectionCases_TenantId_Status_DaysOutstanding",
                schema: "__tenant__",
                table: "Billing_CollectionCases",
                columns: new[] { "TenantId", "Status", "DaysOutstanding" });

            migrationBuilder.CreateIndex(
                name: "IX_Billing_CollectionPolicies_TenantId",
                schema: "__tenant__",
                table: "Billing_CollectionPolicies",
                column: "TenantId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Billing_Contracts_TenantId_ProjectId",
                schema: "__tenant__",
                table: "Billing_Contracts",
                columns: new[] { "TenantId", "ProjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_Billing_EmployeeRoles_TenantId_EmployeeId_ProjectId_EffectiveFrom",
                schema: "__tenant__",
                table: "Billing_EmployeeRoles",
                columns: new[] { "TenantId", "EmployeeId", "ProjectId", "EffectiveFrom" });

            migrationBuilder.CreateIndex(
                name: "IX_Billing_Payments_InvoiceId",
                schema: "__tenant__",
                table: "Billing_Payments",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Billing_ProcessedEventLog_EventId_ConsumerName",
                schema: "__tenant__",
                table: "Billing_ProcessedEventLog",
                columns: new[] { "EventId", "ConsumerName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Billing_RateCards_ContractId_RoleId_EffectiveFrom",
                schema: "__tenant__",
                table: "Billing_RateCards",
                columns: new[] { "ContractId", "RoleId", "EffectiveFrom" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Billing_BillableEntries",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Billing_CollectionCases",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Billing_CollectionPolicies",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Billing_EmployeeRoles",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Billing_InvoiceLines",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Billing_Payments",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Billing_ProcessedEventLog",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Billing_RateCards",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Billing_Invoices",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Billing_Contracts",
                schema: "__tenant__");
        }
    }
}
