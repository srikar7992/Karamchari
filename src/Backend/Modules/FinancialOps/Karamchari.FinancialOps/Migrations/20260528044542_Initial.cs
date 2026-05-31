using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.FinancialOps.Migrations
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
                name: "ExpenseClaims",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimNumber = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ClaimedAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ExpenseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PolicyVersion = table.Column<int>(type: "int", nullable: false),
                    TaxTreatment = table.Column<int>(type: "int", nullable: false),
                    PreferredSettlementType = table.Column<int>(type: "int", nullable: false),
                    ReplayMetadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Holds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Receipts = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProjectionVersion = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseClaims", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Financial_EmployeeAdvances",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsRecovered = table.Column<bool>(type: "bit", nullable: false),
                    RecoveredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReplayMetadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Financial_EmployeeAdvances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Financial_EmployeeLoans",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrincipalAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    InterestRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TenureMonths = table.Column<int>(type: "int", nullable: false),
                    RemainingPrincipal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReplayMetadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Financial_EmployeeLoans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Financial_FinalizationSnapshots",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FinalizedTotal = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Checksum = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FinalizedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Financial_FinalizationSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Financial_OperationEvents",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AggregateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AggregateType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FromState = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ToState = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TriggeredBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrelationChain = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MetadataJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Financial_OperationEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancialCompensations",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TargetEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CorrelationChain = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialCompensations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancialOperationalPeriods",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StartsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndsAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CutoffAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    IsClosed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialOperationalPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReimbursementSettlementBatches",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ClaimIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ExternalReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExecutedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    FinalizedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastExplanation = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReimbursementSettlementBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkforceFinancialLedger",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    CorrelationChain = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReplayMetadata = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExecutionSequenceRef = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkforceFinancialLedger", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Financial_EmployeeLoanRepayments",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RepaidAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CorrelationChain = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LoanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Financial_EmployeeLoanRepayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Financial_EmployeeLoanRepayments_Financial_EmployeeLoans_LoanId",
                        column: x => x.LoanId,
                        principalSchema: "__tenant__",
                        principalTable: "Financial_EmployeeLoans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseClaims_ClaimNumber",
                schema: "__tenant__",
                table: "ExpenseClaims",
                column: "ClaimNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseClaims_EmployeeId",
                schema: "__tenant__",
                table: "ExpenseClaims",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseClaims_TenantId",
                schema: "__tenant__",
                table: "ExpenseClaims",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Financial_EmployeeAdvances_EmployeeId",
                schema: "__tenant__",
                table: "Financial_EmployeeAdvances",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Financial_EmployeeAdvances_TenantId",
                schema: "__tenant__",
                table: "Financial_EmployeeAdvances",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Financial_EmployeeLoanRepayments_LoanId",
                schema: "__tenant__",
                table: "Financial_EmployeeLoanRepayments",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_Financial_EmployeeLoans_EmployeeId",
                schema: "__tenant__",
                table: "Financial_EmployeeLoans",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Financial_EmployeeLoans_TenantId",
                schema: "__tenant__",
                table: "Financial_EmployeeLoans",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Financial_FinalizationSnapshots_TenantId_PeriodId",
                schema: "__tenant__",
                table: "Financial_FinalizationSnapshots",
                columns: new[] { "TenantId", "PeriodId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Financial_OperationEvents_AggregateId",
                schema: "__tenant__",
                table: "Financial_OperationEvents",
                column: "AggregateId");

            migrationBuilder.CreateIndex(
                name: "IX_Financial_OperationEvents_TenantId",
                schema: "__tenant__",
                table: "Financial_OperationEvents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialCompensations_TenantId",
                schema: "__tenant__",
                table: "FinancialCompensations",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialOperationalPeriods_TenantId_Name",
                schema: "__tenant__",
                table: "FinancialOperationalPeriods",
                columns: new[] { "TenantId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReimbursementSettlementBatches_TenantId",
                schema: "__tenant__",
                table: "ReimbursementSettlementBatches",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkforceFinancialLedger_EmployeeId",
                schema: "__tenant__",
                table: "WorkforceFinancialLedger",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkforceFinancialLedger_TenantId",
                schema: "__tenant__",
                table: "WorkforceFinancialLedger",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseClaims",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Financial_EmployeeAdvances",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Financial_EmployeeLoanRepayments",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Financial_FinalizationSnapshots",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Financial_OperationEvents",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "FinancialCompensations",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "FinancialOperationalPeriods",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "ReimbursementSettlementBatches",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "WorkforceFinancialLedger",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Financial_EmployeeLoans",
                schema: "__tenant__");
        }
    }
}
