using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Intelligence.Migrations
{
    /// <inheritdoc />
    public partial class Phase63_StructuralIntelligence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Intel_DependencyScores ───────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Intel_DependencyScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    RiskLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CriticalShiftComponent = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    CrossTrainingComponent = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    UniqueRoleComponent = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    ApprovalComponent = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intel_DependencyScores", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Intel_DependencyScores_TenantId_EmployeeId",
                table: "Intel_DependencyScores",
                columns: new[] { "TenantId", "EmployeeId" },
                unique: true);

            // ── Intel_ScheduleQualityScores ──────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Intel_ScheduleQualityScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QualityScore = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    PoorScheduleRiskLevel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RotationStressComponent = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    RestViolationComponent = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    WeekendClusterComponent = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    SplitShiftComponent = table.Column<decimal>(type: "decimal(5,1)", precision: 5, scale: 1, nullable: false),
                    ComputedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intel_ScheduleQualityScores", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Intel_ScheduleQualityScores_TenantId_EmployeeId",
                table: "Intel_ScheduleQualityScores",
                columns: new[] { "TenantId", "EmployeeId" },
                unique: true);

            // ── Intel_EmployeeSegments ───────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "Intel_EmployeeSegments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Segment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AssignedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Intel_EmployeeSegments", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Intel_EmployeeSegments_TenantId_EmployeeId",
                table: "Intel_EmployeeSegments",
                columns: new[] { "TenantId", "EmployeeId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Intel_EmployeeSegments");
            migrationBuilder.DropTable(name: "Intel_ScheduleQualityScores");
            migrationBuilder.DropTable(name: "Intel_DependencyScores");
        }
    }
}
