using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Workflow.Migrations
{
    /// <inheritdoc />
    public partial class Phase12WorkflowConditions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConditionsJson",
                schema: "__tenant__",
                table: "Workflow_Definitions",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConditionsJson",
                schema: "__tenant__",
                table: "Workflow_Definitions");
        }
    }
}
