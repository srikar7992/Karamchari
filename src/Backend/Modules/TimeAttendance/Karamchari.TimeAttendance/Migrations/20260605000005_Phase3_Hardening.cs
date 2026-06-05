using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.TimeAttendance.Migrations
{
    /// <inheritdoc />
    public partial class Phase3Hardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── AttendanceRecord: optimistic concurrency via row version ─────────────────────────────
            // Prevents double check-in race (Request A + B within 100ms both creating a record).
            // EF throws DbUpdateConcurrencyException on conflict; callers should retry once.

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                schema: "__tenant__",
                table: "Workforce_AttendanceRecords",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: Array.Empty<byte>());

            // ── AttendanceShiftPolicy: three-tier geofencing ──────────────────────────────────────────
            // Single radius caused false violations due to GPS noise (20-100m indoors).
            // AllowedRadius: clean. WarningRadius: logged only. ViolationRadius: exception raised.
            // Existing GeofenceRadiusMeters column is repurposed as the primary (allowed) radius.

            migrationBuilder.AddColumn<int>(
                name: "GeofenceWarningRadiusMeters",
                schema: "__tenant__",
                table: "Attendance_ShiftPolicies",
                type: "int",
                nullable: false,
                defaultValue: 200);

            migrationBuilder.AddColumn<int>(
                name: "GeofenceViolationRadiusMeters",
                schema: "__tenant__",
                table: "Attendance_ShiftPolicies",
                type: "int",
                nullable: false,
                defaultValue: 500);

            // Rename GeofenceRadiusMeters → GeofenceAllowedRadiusMeters for clarity.
            migrationBuilder.RenameColumn(
                name: "GeofenceRadiusMeters",
                schema: "__tenant__",
                table: "Attendance_ShiftPolicies",
                newName: "GeofenceAllowedRadiusMeters");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                schema: "__tenant__",
                table: "Workforce_AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "GeofenceWarningRadiusMeters",
                schema: "__tenant__",
                table: "Attendance_ShiftPolicies");

            migrationBuilder.DropColumn(
                name: "GeofenceViolationRadiusMeters",
                schema: "__tenant__",
                table: "Attendance_ShiftPolicies");

            migrationBuilder.RenameColumn(
                name: "GeofenceAllowedRadiusMeters",
                schema: "__tenant__",
                table: "Attendance_ShiftPolicies",
                newName: "GeofenceRadiusMeters");
        }
    }
}
