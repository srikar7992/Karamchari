using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.TimeAttendance.Migrations
{
    /// <inheritdoc />
    public partial class Phase2RosteringEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roster_Rosters",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roster_Rosters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roster_CoverageRules",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    MinimumStaff = table.Column<int>(type: "int", nullable: false),
                    MaximumStaff = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roster_CoverageRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roster_EmployeeSkills",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Level = table.Column<int>(type: "int", nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roster_EmployeeSkills", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roster_ShiftSwaps",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RequestedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedWithEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceShiftAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetShiftAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ApprovedByEmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    RequestedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roster_ShiftSwaps", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roster_Shifts",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RosterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequiredHeadcount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roster_Shifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roster_Shifts_Roster_Rosters_RosterId",
                        column: x => x.RosterId,
                        principalTable: "Roster_Rosters",
                        principalSchema: "__tenant__",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Roster_CoverageSkillMix",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CoverageRuleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MinimumLevel = table.Column<int>(type: "int", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roster_CoverageSkillMix", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roster_CoverageSkillMix_Roster_CoverageRules_CoverageRuleId",
                        column: x => x.CoverageRuleId,
                        principalTable: "Roster_CoverageRules",
                        principalSchema: "__tenant__",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Roster_ShiftSkillRequirements",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RosterShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SkillName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    MinimumLevel = table.Column<int>(type: "int", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roster_ShiftSkillRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roster_ShiftSkillRequirements_Roster_Shifts_RosterShiftId",
                        column: x => x.RosterShiftId,
                        principalTable: "Roster_Shifts",
                        principalSchema: "__tenant__",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Roster_ShiftAssignments",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RosterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RosterShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AssignedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roster_ShiftAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Roster_ShiftAssignments_Roster_Rosters_RosterId",
                        column: x => x.RosterId,
                        principalTable: "Roster_Rosters",
                        principalSchema: "__tenant__",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Roster_ShiftAssignments_Roster_Shifts_RosterShiftId",
                        column: x => x.RosterShiftId,
                        principalTable: "Roster_Shifts",
                        principalSchema: "__tenant__",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Roster_ScheduleAlerts",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RosterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    DetectedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roster_ScheduleAlerts", x => x.Id);
                });

            // Indexes — Roster_Rosters
            migrationBuilder.CreateIndex(
                name: "IX_Roster_Rosters_TenantId_StartDate_EndDate",
                schema: "__tenant__",
                table: "Roster_Rosters",
                columns: ["TenantId", "StartDate", "EndDate"]);

            migrationBuilder.CreateIndex(
                name: "IX_Roster_Rosters_TenantId_Status",
                schema: "__tenant__",
                table: "Roster_Rosters",
                columns: ["TenantId", "Status"]);

            // Indexes — Roster_Shifts
            migrationBuilder.CreateIndex(
                name: "IX_Roster_Shifts_RosterId_WorkDate",
                schema: "__tenant__",
                table: "Roster_Shifts",
                columns: ["RosterId", "WorkDate"]);

            migrationBuilder.CreateIndex(
                name: "IX_Roster_Shifts_TenantId_LocationId_WorkDate",
                schema: "__tenant__",
                table: "Roster_Shifts",
                columns: ["TenantId", "LocationId", "WorkDate"]);

            // Indexes — Roster_ShiftAssignments
            migrationBuilder.CreateIndex(
                name: "IX_Roster_ShiftAssignments_RosterShiftId_EmployeeId",
                schema: "__tenant__",
                table: "Roster_ShiftAssignments",
                columns: ["RosterShiftId", "EmployeeId"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roster_ShiftAssignments_RosterId_WorkDate",
                schema: "__tenant__",
                table: "Roster_ShiftAssignments",
                columns: ["RosterId", "WorkDate"]);

            migrationBuilder.CreateIndex(
                name: "IX_Roster_ShiftAssignments_EmployeeId_WorkDate",
                schema: "__tenant__",
                table: "Roster_ShiftAssignments",
                columns: ["EmployeeId", "WorkDate"]);

            migrationBuilder.CreateIndex(
                name: "IX_Roster_ShiftAssignments_RosterId",
                schema: "__tenant__",
                table: "Roster_ShiftAssignments",
                column: "RosterId");

            migrationBuilder.CreateIndex(
                name: "IX_Roster_ShiftAssignments_RosterShiftId",
                schema: "__tenant__",
                table: "Roster_ShiftAssignments",
                column: "RosterShiftId");

            // Indexes — Roster_CoverageRules
            migrationBuilder.CreateIndex(
                name: "IX_Roster_CoverageRules_TenantId_LocationId_IsActive",
                schema: "__tenant__",
                table: "Roster_CoverageRules",
                columns: ["TenantId", "LocationId", "IsActive"]);

            migrationBuilder.CreateIndex(
                name: "IX_Roster_CoverageSkillMix_CoverageRuleId",
                schema: "__tenant__",
                table: "Roster_CoverageSkillMix",
                column: "CoverageRuleId");

            // Indexes — Roster_EmployeeSkills
            migrationBuilder.CreateIndex(
                name: "IX_Roster_EmployeeSkills_TenantId_EmployeeId_SkillId_IsActive",
                schema: "__tenant__",
                table: "Roster_EmployeeSkills",
                columns: ["TenantId", "EmployeeId", "SkillId", "IsActive"]);

            migrationBuilder.CreateIndex(
                name: "IX_Roster_EmployeeSkills_TenantId_SkillId_ExpiryDate",
                schema: "__tenant__",
                table: "Roster_EmployeeSkills",
                columns: ["TenantId", "SkillId", "ExpiryDate"]);

            // Indexes — Roster_ShiftSwaps
            migrationBuilder.CreateIndex(
                name: "IX_Roster_ShiftSwaps_TenantId_RequestedByEmployeeId_Status",
                schema: "__tenant__",
                table: "Roster_ShiftSwaps",
                columns: ["TenantId", "RequestedByEmployeeId", "Status"]);

            migrationBuilder.CreateIndex(
                name: "IX_Roster_ShiftSwaps_TenantId_Status_RequestedAtUtc",
                schema: "__tenant__",
                table: "Roster_ShiftSwaps",
                columns: ["TenantId", "Status", "RequestedAtUtc"]);

            // Indexes — Roster_ScheduleAlerts
            migrationBuilder.CreateIndex(
                name: "IX_Roster_ScheduleAlerts_TenantId_RosterId_Type_IsResolved",
                schema: "__tenant__",
                table: "Roster_ScheduleAlerts",
                columns: ["TenantId", "RosterId", "Type", "IsResolved"]);

            migrationBuilder.CreateIndex(
                name: "IX_Roster_ScheduleAlerts_TenantId_EmployeeId_IsResolved",
                schema: "__tenant__",
                table: "Roster_ScheduleAlerts",
                columns: ["TenantId", "EmployeeId", "IsResolved"]);

            // Indexes — Roster_ShiftSkillRequirements
            migrationBuilder.CreateIndex(
                name: "IX_Roster_ShiftSkillRequirements_RosterShiftId",
                schema: "__tenant__",
                table: "Roster_ShiftSkillRequirements",
                column: "RosterShiftId");

            migrationBuilder.CreateTable(
                name: "Roster_EmployeeAvailability",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    From = table.Column<TimeOnly>(type: "time", nullable: false),
                    To = table.Column<TimeOnly>(type: "time", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roster_EmployeeAvailability", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Roster_EmployeeAvailability_TenantId_EmployeeId_IsActive",
                schema: "__tenant__",
                table: "Roster_EmployeeAvailability",
                columns: ["TenantId", "EmployeeId", "IsActive"]);

            migrationBuilder.CreateIndex(
                name: "IX_Roster_EmployeeAvailability_TenantId_EmployeeId_DayOfWeek",
                schema: "__tenant__",
                table: "Roster_EmployeeAvailability",
                columns: ["TenantId", "EmployeeId", "DayOfWeek"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Roster_ShiftAssignments", schema: "__tenant__");
            migrationBuilder.DropTable(name: "Roster_ShiftSkillRequirements", schema: "__tenant__");
            migrationBuilder.DropTable(name: "Roster_ScheduleAlerts", schema: "__tenant__");
            migrationBuilder.DropTable(name: "Roster_ShiftSwaps", schema: "__tenant__");
            migrationBuilder.DropTable(name: "Roster_CoverageSkillMix", schema: "__tenant__");
            migrationBuilder.DropTable(name: "Roster_EmployeeSkills", schema: "__tenant__");
            migrationBuilder.DropTable(name: "Roster_EmployeeAvailability", schema: "__tenant__");
            migrationBuilder.DropTable(name: "Roster_Shifts", schema: "__tenant__");
            migrationBuilder.DropTable(name: "Roster_CoverageRules", schema: "__tenant__");
            migrationBuilder.DropTable(name: "Roster_Rosters", schema: "__tenant__");
        }
    }
}
