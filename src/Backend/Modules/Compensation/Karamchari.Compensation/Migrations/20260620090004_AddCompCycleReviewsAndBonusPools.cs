using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Compensation.Migrations
{
    /// <inheritdoc />
    public partial class AddCompCycleReviewsAndBonusPools : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BonusPools",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PoolName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TotalBudget = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BonusPools", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompensationCycles",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NominationOpenDate = table.Column<DateOnly>(type: "date", nullable: false),
                    NominationCloseDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    MeritMatrixId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompensationCycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeCompReviews",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CurrentCTC = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RecommendedCTC = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IncrementPercent = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: true),
                    ManagerJustification = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CurrencyCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeCompReviews", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BonusPools_Tenant_Cycle",
                schema: "__tenant__",
                table: "BonusPools",
                columns: new[] { "TenantId", "CycleId" });

            migrationBuilder.CreateIndex(
                name: "IX_CompCycles_Tenant_Year",
                schema: "__tenant__",
                table: "CompensationCycles",
                columns: new[] { "TenantId", "Year" });

            migrationBuilder.CreateIndex(
                name: "UIX_EmployeeCompReviews_Cycle_Employee",
                schema: "__tenant__",
                table: "EmployeeCompReviews",
                columns: new[] { "TenantId", "CycleId", "EmployeeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BonusPools",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "CompensationCycles",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "EmployeeCompReviews",
                schema: "__tenant__");
        }
    }
}
