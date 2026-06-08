using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Capability.Migrations
{
    /// <inheritdoc />
    public partial class Phase19LearningIntegration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TargetSkillId",
                schema: "__tenant__",
                table: "Capability_LearningEnrollments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Capability_ModuleSkillTargets",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GrantedLevel = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capability_ModuleSkillTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Capability_ModuleSkillTargets_Capability_LearningModules_ModuleId",
                        column: x => x.ModuleId,
                        principalSchema: "__tenant__",
                        principalTable: "Capability_LearningModules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Capability_ModuleSkillTargets_ModuleId_SkillId",
                schema: "__tenant__",
                table: "Capability_ModuleSkillTargets",
                columns: new[] { "ModuleId", "SkillId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Capability_ModuleSkillTargets",
                schema: "__tenant__");

            migrationBuilder.DropColumn(
                name: "TargetSkillId",
                schema: "__tenant__",
                table: "Capability_LearningEnrollments");
        }
    }
}
