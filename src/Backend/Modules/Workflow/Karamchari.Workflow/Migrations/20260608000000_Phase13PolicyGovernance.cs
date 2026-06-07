using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Workflow.Migrations
{
    /// <inheritdoc />
    public partial class Phase13PolicyGovernance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 13: Policy governance lifecycle + nested expression engine

            // ExpressionJson: stores ConditionGroup JSON (OR/NOT/nested); null means use legacy ConditionsJson
            migrationBuilder.AddColumn<string>(
                name: "ExpressionJson",
                schema: "__tenant__",
                table: "Workflow_Definitions",
                type: "nvarchar(max)",
                nullable: true);

            // Status: policy lifecycle (Draft/InReview/Approved/Published/Deprecated)
            // Existing rows default to Published so routing behaviour is unchanged.
            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "__tenant__",
                table: "Workflow_Definitions",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Published");

            // Effective date window (inclusive bounds; null = no restriction)
            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveFrom",
                schema: "__tenant__",
                table: "Workflow_Definitions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EffectiveTo",
                schema: "__tenant__",
                table: "Workflow_Definitions",
                type: "datetime2",
                nullable: true);

            // Index for governance dashboard queries (filter by Status)
            migrationBuilder.CreateIndex(
                name: "IX_Workflow_Definitions_TenantId_Status",
                schema: "__tenant__",
                table: "Workflow_Definitions",
                columns: new[] { "TenantId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Workflow_Definitions_TenantId_Status",
                schema: "__tenant__",
                table: "Workflow_Definitions");

            migrationBuilder.DropColumn(
                name: "ExpressionJson",
                schema: "__tenant__",
                table: "Workflow_Definitions");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "__tenant__",
                table: "Workflow_Definitions");

            migrationBuilder.DropColumn(
                name: "EffectiveFrom",
                schema: "__tenant__",
                table: "Workflow_Definitions");

            migrationBuilder.DropColumn(
                name: "EffectiveTo",
                schema: "__tenant__",
                table: "Workflow_Definitions");
        }
    }
}
