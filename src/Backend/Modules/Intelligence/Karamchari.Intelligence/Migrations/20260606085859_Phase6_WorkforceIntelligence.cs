using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Intelligence.Migrations
{
    /// <inheritdoc />
    public partial class Phase6_WorkforceIntelligence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Intel_AttritionScores",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Confidence = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ComponentScoresJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intel_AttritionScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Intel_BurnoutScores",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Confidence = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ComponentScoresJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intel_BurnoutScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Intel_ManagerScores",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    AttendanceDimension = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    BurnoutDimension = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    AttritionDimension = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    LeaveHealthDimension = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    StabilityDimension = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    OvertimeDimension = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    DirectReportCount = table.Column<int>(type: "int", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intel_ManagerScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Intel_WorkforceHealthScores",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SiteCode = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    AttendanceHealth = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    BurnoutHealth = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    StaffingHealth = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    LeaveHealth = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    PayrollHealth = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    StabilityHealth = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    EmployeeCount = table.Column<int>(type: "int", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intel_WorkforceHealthScores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Intel_WorkforceRecommendations",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rationale = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    TriggerScore = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ResolvedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intel_WorkforceRecommendations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Intel_WorkforceSignals",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignalType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SignalDate = table.Column<DateOnly>(type: "date", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intel_WorkforceSignals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Intel_AttritionScores_TenantId_EmployeeId",
                schema: "__tenant__",
                table: "Intel_AttritionScores",
                columns: new[] { "TenantId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Intel_BurnoutScores_TenantId_EmployeeId",
                schema: "__tenant__",
                table: "Intel_BurnoutScores",
                columns: new[] { "TenantId", "EmployeeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Intel_ManagerScores_TenantId_ManagerId",
                schema: "__tenant__",
                table: "Intel_ManagerScores",
                columns: new[] { "TenantId", "ManagerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Intel_WorkforceHealthScores_TenantId_SiteCode",
                schema: "__tenant__",
                table: "Intel_WorkforceHealthScores",
                columns: new[] { "TenantId", "SiteCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Intel_WorkforceRecommendations_TenantId_EmployeeId",
                schema: "__tenant__",
                table: "Intel_WorkforceRecommendations",
                columns: new[] { "TenantId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Intel_WorkforceRecommendations_TenantId_EmployeeId_Type_ResolvedAt",
                schema: "__tenant__",
                table: "Intel_WorkforceRecommendations",
                columns: new[] { "TenantId", "EmployeeId", "Type", "ResolvedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Intel_WorkforceSignals_TenantId_EmployeeId",
                schema: "__tenant__",
                table: "Intel_WorkforceSignals",
                columns: new[] { "TenantId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Intel_WorkforceSignals_TenantId_EmployeeId_SignalType_SignalDate",
                schema: "__tenant__",
                table: "Intel_WorkforceSignals",
                columns: new[] { "TenantId", "EmployeeId", "SignalType", "SignalDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Intel_AttritionScores",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Intel_BurnoutScores",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Intel_ManagerScores",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Intel_WorkforceHealthScores",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Intel_WorkforceRecommendations",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Intel_WorkforceSignals",
                schema: "__tenant__");
        }
    }
}
