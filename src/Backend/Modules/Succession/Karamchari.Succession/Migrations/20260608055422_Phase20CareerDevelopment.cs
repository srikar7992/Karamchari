using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.Succession.Migrations
{
    /// <inheritdoc />
    public partial class Phase20CareerDevelopment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "__tenant__");

            migrationBuilder.CreateTable(
                name: "Succession_CareerPaths",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Succession_CareerPaths", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Succession_CriticalRoles",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Department = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CurrentIncumbentEmployeeId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rationale = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Risk = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Succession_CriticalRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Succession_Plans",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Succession_Plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Succession_TalentPools",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    TargetJobFamily = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OwnedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Succession_TalentPools", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Succession_CareerSteps",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PathId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    RoleTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    RoleSkillRequirementId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Succession_CareerSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Succession_CareerSteps_Succession_CareerPaths_PathId",
                        column: x => x.PathId,
                        principalSchema: "__tenant__",
                        principalTable: "Succession_CareerPaths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Succession_Candidates",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Readiness = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PerformanceScore = table.Column<int>(type: "int", nullable: false),
                    PotentialScore = table.Column<int>(type: "int", nullable: false),
                    DevelopmentNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DevelopmentGaps = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlanId_fk = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Succession_Candidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Succession_Candidates_Succession_Plans_PlanId_fk",
                        column: x => x.PlanId_fk,
                        principalSchema: "__tenant__",
                        principalTable: "Succession_Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Succession_TalentPoolMembers",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PerformanceScore = table.Column<int>(type: "int", nullable: false),
                    PotentialScore = table.Column<int>(type: "int", nullable: false),
                    EstimatedReadiness = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NominationNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NominatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastReviewedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExitReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PoolId_fk = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Succession_TalentPoolMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Succession_TalentPoolMembers_Succession_TalentPools_PoolId_fk",
                        column: x => x.PoolId_fk,
                        principalSchema: "__tenant__",
                        principalTable: "Succession_TalentPools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Succession_Candidates_PlanId_fk",
                schema: "__tenant__",
                table: "Succession_Candidates",
                column: "PlanId_fk");

            migrationBuilder.CreateIndex(
                name: "IX_Succession_CareerPaths_TenantId_Name",
                schema: "__tenant__",
                table: "Succession_CareerPaths",
                columns: new[] { "TenantId", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_Succession_CareerSteps_PathId_StepOrder",
                schema: "__tenant__",
                table: "Succession_CareerSteps",
                columns: new[] { "PathId", "StepOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Succession_CriticalRoles_TenantId_RoleTitle",
                schema: "__tenant__",
                table: "Succession_CriticalRoles",
                columns: new[] { "TenantId", "RoleTitle" });

            migrationBuilder.CreateIndex(
                name: "IX_Succession_TalentPoolMembers_PoolId_fk",
                schema: "__tenant__",
                table: "Succession_TalentPoolMembers",
                column: "PoolId_fk");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Succession_Candidates",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Succession_CareerSteps",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Succession_CriticalRoles",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Succession_TalentPoolMembers",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Succession_Plans",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Succession_CareerPaths",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Succession_TalentPools",
                schema: "__tenant__");
        }
    }
}
