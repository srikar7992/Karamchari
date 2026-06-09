using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Capability.Migrations
{
    /// <inheritdoc />
    public partial class Phase24CareerReadinessProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Capability_CareerReadinessProjections",
                schema: "__tenant__",
                columns: table => new
                {
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleRequirementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CoveragePercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    GapCount = table.Column<int>(type: "int", nullable: false),
                    CriticalGapCount = table.Column<int>(type: "int", nullable: false),
                    ReadinessScore = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Band = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    LastUpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capability_CareerReadinessProjections", x => new { x.TenantId, x.EmployeeId, x.RoleRequirementId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_CareerReadiness_EmployeeBand",
                schema: "__tenant__",
                table: "Capability_CareerReadinessProjections",
                columns: new[] { "TenantId", "EmployeeId", "Band" });

            migrationBuilder.CreateIndex(
                name: "IX_CareerReadiness_RoleRanking",
                schema: "__tenant__",
                table: "Capability_CareerReadinessProjections",
                columns: new[] { "TenantId", "RoleRequirementId", "Band", "ReadinessScore" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Capability_CareerReadinessProjections",
                schema: "__tenant__");
        }
    }
}
