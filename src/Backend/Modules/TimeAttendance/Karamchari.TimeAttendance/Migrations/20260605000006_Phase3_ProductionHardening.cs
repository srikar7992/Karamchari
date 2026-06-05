using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.TimeAttendance.Migrations
{
    /// <inheritdoc />
    public partial class Phase3ProductionHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── AttendanceSession: split-shift support ────────────────────────────────────────────────
            // Old unique index (TenantId, EmployeeId, WorkDate) prevented employees from having
            // two sessions on the same day (split shifts, overtime after a completed shift).
            // New design: unique per assignment, not per calendar day.

            migrationBuilder.DropIndex(
                name: "IX_Workforce_AttendanceSessions_TenantId_EmployeeId_WorkDate",
                schema: "__tenant__",
                table: "Workforce_AttendanceSessions");

            migrationBuilder.AddColumn<Guid>(
                name: "ShiftAssignmentId",
                schema: "__tenant__",
                table: "Workforce_AttendanceSessions",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty);

            // Non-unique index for date-range queries (manager dashboard, finalization job)
            migrationBuilder.CreateIndex(
                name: "IX_Workforce_AttendanceSessions_TenantId_EmployeeId_WorkDate",
                schema: "__tenant__",
                table: "Workforce_AttendanceSessions",
                columns: new[] { "TenantId", "EmployeeId", "WorkDate" });

            // Filtered unique index: one session per shift assignment.
            // Walk-in sessions (ShiftAssignmentId = Guid.Empty) are exempt — multiple walk-ins on same day allowed.
            migrationBuilder.CreateIndex(
                name: "IX_Workforce_AttendanceSessions_TenantId_EmployeeId_ShiftAssignmentId",
                schema: "__tenant__",
                table: "Workforce_AttendanceSessions",
                columns: new[] { "TenantId", "EmployeeId", "ShiftAssignmentId" },
                unique: true,
                filter: "[ShiftAssignmentId] != '00000000-0000-0000-0000-000000000000'");

            // ── AttendanceAuditLog: ChangedFields for dispute resolution ──────────────────────────────
            // Status-only audit trail is insufficient when fields like ActualStart change.
            // ChangedFields stores JSON {Before:{...}, After:{...}} for regularization events.
            // Nullable — system events (check-in, no-show, finalization) leave it null.

            migrationBuilder.AddColumn<string>(
                name: "ChangedFields",
                schema: "__tenant__",
                table: "Attendance_AuditLog",
                type: "nvarchar(4096)",
                maxLength: 4096,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Workforce_AttendanceSessions_TenantId_EmployeeId_ShiftAssignmentId",
                schema: "__tenant__",
                table: "Workforce_AttendanceSessions");

            migrationBuilder.DropIndex(
                name: "IX_Workforce_AttendanceSessions_TenantId_EmployeeId_WorkDate",
                schema: "__tenant__",
                table: "Workforce_AttendanceSessions");

            migrationBuilder.DropColumn(
                name: "ShiftAssignmentId",
                schema: "__tenant__",
                table: "Workforce_AttendanceSessions");

            migrationBuilder.DropColumn(
                name: "ChangedFields",
                schema: "__tenant__",
                table: "Attendance_AuditLog");

            // Restore original unique index
            migrationBuilder.CreateIndex(
                name: "IX_Workforce_AttendanceSessions_TenantId_EmployeeId_WorkDate",
                schema: "__tenant__",
                table: "Workforce_AttendanceSessions",
                columns: new[] { "TenantId", "EmployeeId", "WorkDate" },
                unique: true);
        }
    }
}
