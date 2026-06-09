using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Capability.Migrations
{
    /// <inheritdoc />
    public partial class Phase25InternalMobilityProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Capability_MobilityVacancies",
                schema: "__tenant__",
                columns: table => new
                {
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    VacancyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleRequirementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OpenedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capability_MobilityVacancies", x => new { x.TenantId, x.VacancyId });
                });

            migrationBuilder.CreateTable(
                name: "Capability_InternalMobilityProjections",
                schema: "__tenant__",
                columns: table => new
                {
                    TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VacancyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleRequirementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CoveragePercent = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    ReadinessScore = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                    Band = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    MissingSkillCount = table.Column<int>(type: "int", nullable: false),
                    CriticalGapCount = table.Column<int>(type: "int", nullable: false),
                    Eligible = table.Column<bool>(type: "bit", nullable: false),
                    LastUpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Capability_InternalMobilityProjections", x => new { x.TenantId, x.EmployeeId, x.VacancyId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_MobilityVacancy_RoleRequirement",
                schema: "__tenant__",
                table: "Capability_MobilityVacancies",
                columns: new[] { "TenantId", "RoleRequirementId" });

            migrationBuilder.CreateIndex(
                name: "IX_Mobility_VacancyRanking",
                schema: "__tenant__",
                table: "Capability_InternalMobilityProjections",
                columns: new[] { "TenantId", "VacancyId", "Band", "ReadinessScore" });

            migrationBuilder.CreateIndex(
                name: "IX_Mobility_EmployeeOpportunities",
                schema: "__tenant__",
                table: "Capability_InternalMobilityProjections",
                columns: new[] { "TenantId", "EmployeeId", "Eligible", "ReadinessScore" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Capability_InternalMobilityProjections",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Capability_MobilityVacancies",
                schema: "__tenant__");
        }
    }
}
