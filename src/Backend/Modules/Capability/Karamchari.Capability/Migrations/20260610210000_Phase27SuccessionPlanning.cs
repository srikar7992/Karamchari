using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Capability.Migrations;

/// <inheritdoc />
public partial class Phase27SuccessionPlanning : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Capability_CriticalPositions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                PositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RoleRequirementId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Criticality = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                DeactivatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Capability_CriticalPositions", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_CriticalPosition_Tenant_Position",
            table: "Capability_CriticalPositions",
            columns: new[] { "TenantId", "PositionId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_CriticalPosition_RoleRequirement",
            table: "Capability_CriticalPositions",
            columns: new[] { "TenantId", "RoleRequirementId" });

        migrationBuilder.CreateTable(
            name: "Capability_SuccessorCandidateProjections",
            columns: table => new
            {
                TenantId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                PositionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ReadinessScore = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: false),
                Band = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                Rank = table.Column<int>(type: "int", nullable: false),
                LastUpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Capability_SuccessorCandidateProjections",
                    x => new { x.TenantId, x.PositionId, x.EmployeeId });
            });

        migrationBuilder.CreateIndex(
            name: "IX_SuccessorCandidate_PositionRanking",
            table: "Capability_SuccessorCandidateProjections",
            columns: new[] { "TenantId", "PositionId", "Band", "Rank" });

        migrationBuilder.CreateIndex(
            name: "IX_SuccessorCandidate_EmployeePools",
            table: "Capability_SuccessorCandidateProjections",
            columns: new[] { "TenantId", "EmployeeId" });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Capability_SuccessorCandidateProjections");
        migrationBuilder.DropTable(name: "Capability_CriticalPositions");
    }
}
