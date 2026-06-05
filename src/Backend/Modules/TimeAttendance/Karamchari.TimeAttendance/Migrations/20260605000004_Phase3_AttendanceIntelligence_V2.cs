using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.TimeAttendance.Migrations
{
    /// <inheritdoc />
    public partial class Phase3AttendanceIntelligenceV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── ShiftAssignmentCache: lightweight roster lookup (replaces eager Pending record creation) ─

            migrationBuilder.CreateTable(
                name: "Attendance_ShiftAssignmentCache",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftAssignmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpectedStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpectedEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_Attendance_ShiftAssignmentCache", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_ShiftAssignmentCache_TenantId_EmployeeId_ShiftId",
                schema: "__tenant__",
                table: "Attendance_ShiftAssignmentCache",
                columns: new[] { "TenantId", "EmployeeId", "ShiftId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_ShiftAssignmentCache_TenantId_EmployeeId_WorkDate",
                schema: "__tenant__",
                table: "Attendance_ShiftAssignmentCache",
                columns: new[] { "TenantId", "EmployeeId", "WorkDate" });

            // ── AttendanceAuditLog: immutable audit trail for all status changes ──────────────────────

            migrationBuilder.CreateTable(
                name: "Attendance_AuditLog",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AttendanceRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PreviousStatus = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    NewStatus = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ChangedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ChangedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                },
                constraints: table => table.PrimaryKey("PK_Attendance_AuditLog", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_AuditLog_TenantId_AttendanceRecordId_ChangedAt",
                schema: "__tenant__",
                table: "Attendance_AuditLog",
                columns: new[] { "TenantId", "AttendanceRecordId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_AuditLog_TenantId_EmployeeId_ChangedAt",
                schema: "__tenant__",
                table: "Attendance_AuditLog",
                columns: new[] { "TenantId", "EmployeeId", "ChangedAt" });

            // ── AttendanceShiftPolicies: add severity threshold columns ───────────────────────────────

            migrationBuilder.AddColumn<int>(
                name: "LateArrivalMediumThresholdMinutes",
                schema: "__tenant__",
                table: "Attendance_ShiftPolicies",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "LateArrivalHighThresholdMinutes",
                schema: "__tenant__",
                table: "Attendance_ShiftPolicies",
                type: "int",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<int>(
                name: "EarlyExitMediumThresholdMinutes",
                schema: "__tenant__",
                table: "Attendance_ShiftPolicies",
                type: "int",
                nullable: false,
                defaultValue: 10);

            migrationBuilder.AddColumn<int>(
                name: "EarlyExitHighThresholdMinutes",
                schema: "__tenant__",
                table: "Attendance_ShiftPolicies",
                type: "int",
                nullable: false,
                defaultValue: 30);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attendance_ShiftAssignmentCache",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Attendance_AuditLog",
                schema: "__tenant__");

            migrationBuilder.DropColumn(
                name: "LateArrivalMediumThresholdMinutes",
                schema: "__tenant__",
                table: "Attendance_ShiftPolicies");

            migrationBuilder.DropColumn(
                name: "LateArrivalHighThresholdMinutes",
                schema: "__tenant__",
                table: "Attendance_ShiftPolicies");

            migrationBuilder.DropColumn(
                name: "EarlyExitMediumThresholdMinutes",
                schema: "__tenant__",
                table: "Attendance_ShiftPolicies");

            migrationBuilder.DropColumn(
                name: "EarlyExitHighThresholdMinutes",
                schema: "__tenant__",
                table: "Attendance_ShiftPolicies");
        }
    }
}
