using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Governance.Migrations
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
                name: "Governance_OperationalIncidents",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DeclaredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AffectedComponent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RootCauseSummary = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActionItemsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Governance_OperationalIncidents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Governance_SchemaDefinitions",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ContractName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    JsonSchema = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompatibilityRule = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OwnerModule = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeprecatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ArchivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Governance_SchemaDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Governance_ServiceLevelObjectives",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ComponentName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Tier = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetSuccessRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    EvaluationWindowDays = table.Column<int>(type: "int", nullable: false),
                    CurrentSuccessRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ErrorBudgetRemainingPercent = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    LastEvaluatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Governance_ServiceLevelObjectives", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Governance_OperationalIncidents_TenantId_Status",
                schema: "__tenant__",
                table: "Governance_OperationalIncidents",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Governance_SchemaDefinitions_TenantId_ContractName_Version",
                schema: "__tenant__",
                table: "Governance_SchemaDefinitions",
                columns: new[] { "TenantId", "ContractName", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Governance_ServiceLevelObjectives_TenantId_ComponentName",
                schema: "__tenant__",
                table: "Governance_ServiceLevelObjectives",
                columns: new[] { "TenantId", "ComponentName" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Governance_OperationalIncidents",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Governance_SchemaDefinitions",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Governance_ServiceLevelObjectives",
                schema: "__tenant__");
        }
    }
}
