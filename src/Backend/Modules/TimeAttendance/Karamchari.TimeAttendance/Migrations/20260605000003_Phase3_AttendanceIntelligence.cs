using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.TimeAttendance.Migrations
{
    /// <inheritdoc />
    public partial class Phase3AttendanceIntelligence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── Modify AttendanceRecords: add Phase 3 columns ─────────────────────

            migrationBuilder.AddColumn<Guid>(
                name: "ShiftAssignmentId",
                schema: "__tenant__",
                table: "Workforce_AttendanceRecords",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: Guid.Empty);

            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                schema: "__tenant__",
                table: "Workforce_AttendanceRecords",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpectedStart",
                schema: "__tenant__",
                table: "Workforce_AttendanceRecords",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: DateTimeOffset.MinValue);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpectedEnd",
                schema: "__tenant__",
                table: "Workforce_AttendanceRecords",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: DateTimeOffset.MinValue);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ActualStart",
                schema: "__tenant__",
                table: "Workforce_AttendanceRecords",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ActualEnd",
                schema: "__tenant__",
                table: "Workforce_AttendanceRecords",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "WorkDate",
                schema: "__tenant__",
                table: "Workforce_AttendanceRecords",
                type: "date",
                nullable: false,
                defaultValue: DateOnly.MinValue);

            migrationBuilder.AddColumn<int>(
                name: "TotalWorkedMinutes",
                schema: "__tenant__",
                table: "Workforce_AttendanceRecords",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OvertimeMinutes",
                schema: "__tenant__",
                table: "Workforce_AttendanceRecords",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                schema: "__tenant__",
                table: "Workforce_AttendanceRecords",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: DateTimeOffset.UtcNow);

            // Drop the old (TenantId, EmployeeId, Date) unique index
            migrationBuilder.DropIndex(
                name: "IX_Workforce_AttendanceRecords_TenantId_EmployeeId_Date",
                schema: "__tenant__",
                table: "Workforce_AttendanceRecords");

            // New unique index per (TenantId, EmployeeId, ShiftId)
            migrationBuilder.CreateIndex(
                name: "IX_Workforce_AttendanceRecords_TenantId_EmployeeId_ShiftId",
                schema: "__tenant__",
                table: "Workforce_AttendanceRecords",
                columns: ["TenantId", "EmployeeId", "ShiftId"],
                unique: true,
                filter: "[ShiftId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Workforce_AttendanceRecords_TenantId_EmployeeId_WorkDate",
                schema: "__tenant__",
                table: "Workforce_AttendanceRecords",
                columns: ["TenantId", "EmployeeId", "WorkDate"]);

            migrationBuilder.CreateIndex(
                name: "IX_Workforce_AttendanceRecords_TenantId_Status",
                schema: "__tenant__",
                table: "Workforce_AttendanceRecords",
                columns: ["TenantId", "Status"]);

            // ── Attendance_Exceptions ──────────────────────────────────────────────

            migrationBuilder.CreateTable(
                name: "Attendance_Exceptions",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AttendanceRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExceptionType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AcknowledgedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Resolution = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendance_Exceptions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_Exceptions_TenantId_AttendanceRecordId",
                schema: "__tenant__",
                table: "Attendance_Exceptions",
                columns: ["TenantId", "AttendanceRecordId"]);

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_Exceptions_TenantId_EmployeeId_Status",
                schema: "__tenant__",
                table: "Attendance_Exceptions",
                columns: ["TenantId", "EmployeeId", "Status"]);

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_Exceptions_TenantId_Severity_Status",
                schema: "__tenant__",
                table: "Attendance_Exceptions",
                columns: ["TenantId", "Severity", "Status"]);

            // ── Attendance_Regularizations ────────────────────────────────────────

            migrationBuilder.CreateTable(
                name: "Attendance_Regularizations",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AttendanceRecordId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Explanation = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Evidence = table.Column<string>(type: "nvarchar(4096)", maxLength: 4096, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ApprovedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendance_Regularizations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_Regularizations_TenantId_EmployeeId_Status",
                schema: "__tenant__",
                table: "Attendance_Regularizations",
                columns: ["TenantId", "EmployeeId", "Status"]);

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_Regularizations_TenantId_AttendanceRecordId",
                schema: "__tenant__",
                table: "Attendance_Regularizations",
                columns: ["TenantId", "AttendanceRecordId"]);

            // ── Attendance_Scores ─────────────────────────────────────────────────

            migrationBuilder.CreateTable(
                name: "Attendance_Scores",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReliabilityScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ConsistencyScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ComplianceScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    TotalAssignedShifts = table.Column<int>(type: "int", nullable: false),
                    TotalPresentShifts = table.Column<int>(type: "int", nullable: false),
                    TotalLateArrivals = table.Column<int>(type: "int", nullable: false),
                    TotalNoShows = table.Column<int>(type: "int", nullable: false),
                    TotalMissingPunches = table.Column<int>(type: "int", nullable: false),
                    TotalUnauthorizedOvertime = table.Column<int>(type: "int", nullable: false),
                    TotalLocationViolations = table.Column<int>(type: "int", nullable: false),
                    LastCalculated = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendance_Scores", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_Scores_TenantId_EmployeeId",
                schema: "__tenant__",
                table: "Attendance_Scores",
                columns: ["TenantId", "EmployeeId"],
                unique: true);

            // ── Attendance_ShiftPolicies ──────────────────────────────────────────

            migrationBuilder.CreateTable(
                name: "Attendance_ShiftPolicies",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LateToleranceMinutes = table.Column<int>(type: "int", nullable: false),
                    EarlyExitToleranceMinutes = table.Column<int>(type: "int", nullable: false),
                    MaxMissingPunchesPerMonth = table.Column<int>(type: "int", nullable: false),
                    OvertimeThresholdMinutes = table.Column<int>(type: "int", nullable: false),
                    EnforceGeofencing = table.Column<bool>(type: "bit", nullable: false),
                    GeofenceRadiusMeters = table.Column<int>(type: "int", nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendance_ShiftPolicies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_ShiftPolicies_TenantId_LocationId",
                schema: "__tenant__",
                table: "Attendance_ShiftPolicies",
                columns: ["TenantId", "LocationId"]);

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_ShiftPolicies_TenantId_IsDefault",
                schema: "__tenant__",
                table: "Attendance_ShiftPolicies",
                columns: ["TenantId", "IsDefault"]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Attendance_Exceptions", schema: "__tenant__");
            migrationBuilder.DropTable(name: "Attendance_Regularizations", schema: "__tenant__");
            migrationBuilder.DropTable(name: "Attendance_Scores", schema: "__tenant__");
            migrationBuilder.DropTable(name: "Attendance_ShiftPolicies", schema: "__tenant__");

            // Restore old index
            migrationBuilder.DropIndex(
                name: "IX_Workforce_AttendanceRecords_TenantId_EmployeeId_ShiftId",
                schema: "__tenant__",
                table: "Workforce_AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_Workforce_AttendanceRecords_TenantId_EmployeeId_WorkDate",
                schema: "__tenant__",
                table: "Workforce_AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_Workforce_AttendanceRecords_TenantId_Status",
                schema: "__tenant__",
                table: "Workforce_AttendanceRecords");

            migrationBuilder.CreateIndex(
                name: "IX_Workforce_AttendanceRecords_TenantId_EmployeeId_Date",
                schema: "__tenant__",
                table: "Workforce_AttendanceRecords",
                columns: ["TenantId", "EmployeeId", "Date"],
                unique: true);

            // Drop added columns
            migrationBuilder.DropColumn(name: "ShiftAssignmentId", schema: "__tenant__", table: "Workforce_AttendanceRecords");
            migrationBuilder.DropColumn(name: "SessionId", schema: "__tenant__", table: "Workforce_AttendanceRecords");
            migrationBuilder.DropColumn(name: "ExpectedStart", schema: "__tenant__", table: "Workforce_AttendanceRecords");
            migrationBuilder.DropColumn(name: "ExpectedEnd", schema: "__tenant__", table: "Workforce_AttendanceRecords");
            migrationBuilder.DropColumn(name: "ActualStart", schema: "__tenant__", table: "Workforce_AttendanceRecords");
            migrationBuilder.DropColumn(name: "ActualEnd", schema: "__tenant__", table: "Workforce_AttendanceRecords");
            migrationBuilder.DropColumn(name: "WorkDate", schema: "__tenant__", table: "Workforce_AttendanceRecords");
            migrationBuilder.DropColumn(name: "TotalWorkedMinutes", schema: "__tenant__", table: "Workforce_AttendanceRecords");
            migrationBuilder.DropColumn(name: "OvertimeMinutes", schema: "__tenant__", table: "Workforce_AttendanceRecords");
            migrationBuilder.DropColumn(name: "CreatedAt", schema: "__tenant__", table: "Workforce_AttendanceRecords");
        }
    }
}
