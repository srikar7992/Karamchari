using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Intelligence.Migrations
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
                name: "Intelligence_MetricDefinitions",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Owner = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CurrentVersion = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Calculation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsDeprecated = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intelligence_MetricDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Intelligence_Signals",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SignalType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubjectId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Confidence = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LineageData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intelligence_Signals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Strategy_ExecutiveInsights",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Narrative = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Impact = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContributingSignalIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AggregateConfidence = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                    AcknowledgedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Strategy_ExecutiveInsights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Strategy_OrgHealthSignals",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OrgUnitId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    OverallHealthScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    BurnoutRisk = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StaffingStress = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Confidence = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EvaluatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Strategy_OrgHealthSignals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Strategy_StrategicScenarios",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ScenarioParametersJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsConfidential = table.Column<bool>(type: "bit", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Strategy_StrategicScenarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Strategy_WorkforceRiskSignals",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SubjectId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Category = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Confidence = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DetectedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Strategy_WorkforceRiskSignals", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Intelligence_MetricDefinitions_TenantId_Name_CurrentVersion",
                schema: "__tenant__",
                table: "Intelligence_MetricDefinitions",
                columns: new[] { "TenantId", "Name", "CurrentVersion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Intelligence_Signals_TenantId_SignalType_SubjectId",
                schema: "__tenant__",
                table: "Intelligence_Signals",
                columns: new[] { "TenantId", "SignalType", "SubjectId" });

            migrationBuilder.CreateIndex(
                name: "IX_Strategy_ExecutiveInsights_TenantId_Category",
                schema: "__tenant__",
                table: "Strategy_ExecutiveInsights",
                columns: new[] { "TenantId", "Category" });

            migrationBuilder.CreateIndex(
                name: "IX_Strategy_OrgHealthSignals_TenantId_OrgUnitId",
                schema: "__tenant__",
                table: "Strategy_OrgHealthSignals",
                columns: new[] { "TenantId", "OrgUnitId" });

            migrationBuilder.CreateIndex(
                name: "IX_Strategy_WorkforceRiskSignals_TenantId_Category_SubjectId",
                schema: "__tenant__",
                table: "Strategy_WorkforceRiskSignals",
                columns: new[] { "TenantId", "Category", "SubjectId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Intelligence_MetricDefinitions",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Intelligence_Signals",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Strategy_ExecutiveInsights",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Strategy_OrgHealthSignals",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Strategy_StrategicScenarios",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Strategy_WorkforceRiskSignals",
                schema: "__tenant__");
        }
    }
}
