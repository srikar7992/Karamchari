using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Capability.Migrations
{
    /// <inheritdoc />
    public partial class Phase23EmployeeSkillGapProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Capability_EmployeeSkillGapProjections",
                schema: "__tenant__",
                columns: table => new
                {
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleRequirementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RequiredLevel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CurrentLevel = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    LevelsBehind = table.Column<int>(type: "int", nullable: false),
                    IsMissing = table.Column<bool>(type: "bit", nullable: false),
                    LastUpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capability_EmployeeSkillGapProjections", x => new { x.TenantId, x.EmployeeId, x.RoleRequirementId, x.SkillId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_SkillGap_EmployeeGaps",
                schema: "__tenant__",
                table: "Capability_EmployeeSkillGapProjections",
                columns: new[] { "TenantId", "EmployeeId" });

            migrationBuilder.CreateIndex(
                name: "IX_SkillGap_RoleSkillGap",
                schema: "__tenant__",
                table: "Capability_EmployeeSkillGapProjections",
                columns: new[] { "TenantId", "RoleRequirementId", "SkillId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Capability_EmployeeSkillGapProjections",
                schema: "__tenant__");
        }
    }
}
