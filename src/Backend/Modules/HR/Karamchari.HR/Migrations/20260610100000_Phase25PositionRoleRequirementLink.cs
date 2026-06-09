using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.HR.Migrations
{
    /// <inheritdoc />
    public partial class Phase25PositionRoleRequirementLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "RoleSkillRequirementId",
                schema: "__tenant__",
                table: "Positions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Positions_RoleSkillRequirementId",
                schema: "__tenant__",
                table: "Positions",
                column: "RoleSkillRequirementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Positions_RoleSkillRequirementId",
                schema: "__tenant__",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "RoleSkillRequirementId",
                schema: "__tenant__",
                table: "Positions");
        }
    }
}
