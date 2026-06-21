using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Benefits.Migrations
{
    /// <inheritdoc />
    public partial class AddBenefitsSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "__tenant__");

            migrationBuilder.CreateTable(
                name: "Benefits_DeductionRecords",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PayPeriod = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    EmployeeDeduction = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EmployerContribution = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Benefits_DeductionRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Benefits_Enrollments",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TierType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsWaived = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Benefits_Enrollments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Benefits_EnrollmentWindows",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Benefits_EnrollmentWindows", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Benefits_LifeEventRequests",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Benefits_LifeEventRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Benefits_Plans",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Carrier = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Benefits_Plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Benefits_Beneficiaries",
                schema: "__tenant__",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AllocationPercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Benefits_Beneficiaries", x => new { x.EnrollmentId, x.Name, x.Relationship });
                    table.ForeignKey(
                        name: "FK_Benefits_Beneficiaries_Benefits_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalSchema: "__tenant__",
                        principalTable: "Benefits_Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Benefits_Dependents",
                schema: "__tenant__",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Relationship = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EnrollmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    IsCovered = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Benefits_Dependents", x => new { x.EnrollmentId, x.Name, x.Relationship });
                    table.ForeignKey(
                        name: "FK_Benefits_Dependents_Benefits_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalSchema: "__tenant__",
                        principalTable: "Benefits_Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Benefits_EligibilityRules",
                schema: "__tenant__",
                columns: table => new
                {
                    RuleType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BenefitPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Benefits_EligibilityRules", x => new { x.BenefitPlanId, x.RuleType });
                    table.ForeignKey(
                        name: "FK_Benefits_EligibilityRules_Benefits_Plans_BenefitPlanId",
                        column: x => x.BenefitPlanId,
                        principalSchema: "__tenant__",
                        principalTable: "Benefits_Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Benefits_PlanTiers",
                schema: "__tenant__",
                columns: table => new
                {
                    TierType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BenefitPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MonthlyCost = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EmployerContribution = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Benefits_PlanTiers", x => new { x.BenefitPlanId, x.TierType });
                    table.ForeignKey(
                        name: "FK_Benefits_PlanTiers_Benefits_Plans_BenefitPlanId",
                        column: x => x.BenefitPlanId,
                        principalSchema: "__tenant__",
                        principalTable: "Benefits_Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Benefits_DeductionRecords_TenantId_EmployeeId_PlanId_PayPeriod",
                schema: "__tenant__",
                table: "Benefits_DeductionRecords",
                columns: new[] { "TenantId", "EmployeeId", "PlanId", "PayPeriod" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Benefits_Enrollments_TenantId_EmployeeId_PlanId",
                schema: "__tenant__",
                table: "Benefits_Enrollments",
                columns: new[] { "TenantId", "EmployeeId", "PlanId" });

            migrationBuilder.CreateIndex(
                name: "IX_Benefits_EnrollmentWindows_TenantId_Status",
                schema: "__tenant__",
                table: "Benefits_EnrollmentWindows",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Benefits_LifeEventRequests_TenantId_EmployeeId_Status",
                schema: "__tenant__",
                table: "Benefits_LifeEventRequests",
                columns: new[] { "TenantId", "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Benefits_Plans_TenantId_Type_Status",
                schema: "__tenant__",
                table: "Benefits_Plans",
                columns: new[] { "TenantId", "Type", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Benefits_Beneficiaries",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Benefits_DeductionRecords",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Benefits_Dependents",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Benefits_EligibilityRules",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Benefits_EnrollmentWindows",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Benefits_LifeEventRequests",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Benefits_PlanTiers",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Benefits_Enrollments",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Benefits_Plans",
                schema: "__tenant__");
        }
    }
}
