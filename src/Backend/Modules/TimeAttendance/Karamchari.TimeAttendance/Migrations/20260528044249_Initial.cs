using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Karamchari.TimeAttendance.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "__tenant__");

            migrationBuilder.CreateTable(
                name: "Analytics_ProcessedEventLog",
                schema: "__tenant__",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ConsumerName = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Analytics_ProcessedEventLog", x => new { x.EventId, x.ConsumerName });
                });

            migrationBuilder.CreateTable(
                name: "Analytics_ProjectMetrics",
                schema: "__tenant__",
                columns: table => new
                {
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    BillableHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalHours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Revenue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CollectedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LastEventId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LastProcessedOccurredAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastUpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Analytics_ProjectMetrics", x => new { x.TenantId, x.ProjectId, x.Date });
                });

            migrationBuilder.CreateTable(
                name: "HolidayCalendars",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HolidayCalendars", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeaveBalances",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveBalances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeavePolicies",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Rules = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeavePolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ActualDays = table.Column<double>(type: "float", nullable: false),
                    RequestedOnUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projections_LeaveBalances",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AvailableBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ConsumedBalance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    LastUpdatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projections_LeaveBalances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Timesheets",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WeekStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EmployeeTimeZoneId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsRetroactive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    AuditLog = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Entries = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timesheets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Workforce_AttendanceAnomalies",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DetectedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResolutionNote = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workforce_AttendanceAnomalies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Workforce_AttendancePolicies",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workforce_AttendancePolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Workforce_AttendanceRecords",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WorkedHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OvertimeHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsRegularized = table.Column<bool>(type: "bit", nullable: false),
                    FinalizedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workforce_AttendanceRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Workforce_AttendanceSessions",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CheckInTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CheckOutTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TotalWorkHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalBreakHours = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CheckIn_Latitude = table.Column<double>(type: "float", nullable: true),
                    CheckIn_Longitude = table.Column<double>(type: "float", nullable: true),
                    CheckInDeviceId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workforce_AttendanceSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Workforce_Schedules",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workforce_Schedules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Workforce_ShiftDefinitions",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    LateArrivalGraceMinutes = table.Column<int>(type: "int", nullable: false),
                    EarlyDepartureGraceMinutes = table.Column<int>(type: "int", nullable: false),
                    MinimumHoursForFullDay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MinimumHoursForHalfDay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workforce_ShiftDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Holidays",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CalendarId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holidays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Holidays_HolidayCalendars_CalendarId",
                        column: x => x.CalendarId,
                        principalSchema: "__tenant__",
                        principalTable: "HolidayCalendars",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeaveBalanceEntries",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntryType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SourceReference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    BalanceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveBalanceEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveBalanceEntries_LeaveBalances_BalanceId",
                        column: x => x.BalanceId,
                        principalSchema: "__tenant__",
                        principalTable: "LeaveBalances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Workforce_ComplianceRules",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PolicyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Threshold = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workforce_ComplianceRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workforce_ComplianceRules_Workforce_AttendancePolicies_PolicyId",
                        column: x => x.PolicyId,
                        principalSchema: "__tenant__",
                        principalTable: "Workforce_AttendancePolicies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Workforce_AttendanceEvents",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: true),
                    Longitude = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workforce_AttendanceEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workforce_AttendanceEvents_Workforce_AttendanceSessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "__tenant__",
                        principalTable: "Workforce_AttendanceSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Workforce_ShiftAssignments",
                schema: "__tenant__",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EmployeeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ShiftId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Workforce_ShiftAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Workforce_ShiftAssignments_Workforce_Schedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalSchema: "__tenant__",
                        principalTable: "Workforce_Schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_CalendarId",
                schema: "__tenant__",
                table: "Holidays",
                column: "CalendarId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalanceEntries_BalanceId",
                schema: "__tenant__",
                table: "LeaveBalanceEntries",
                column: "BalanceId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalanceEntries_EmployeeId_PolicyId",
                schema: "__tenant__",
                table: "LeaveBalanceEntries",
                columns: ["EmployeeId", "PolicyId"]);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalances_TenantId_EmployeeId_PolicyId",
                schema: "__tenant__",
                table: "LeaveBalances",
                columns: ["TenantId", "EmployeeId", "PolicyId"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Projections_LeaveBalances_TenantId_EmployeeId_PolicyId",
                schema: "__tenant__",
                table: "Projections_LeaveBalances",
                columns: ["TenantId", "EmployeeId", "PolicyId"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workforce_AttendanceAnomalies_TenantId_Status",
                schema: "__tenant__",
                table: "Workforce_AttendanceAnomalies",
                columns: ["TenantId", "Status"]);

            migrationBuilder.CreateIndex(
                name: "IX_Workforce_AttendanceEvents_SessionId",
                schema: "__tenant__",
                table: "Workforce_AttendanceEvents",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_Workforce_AttendanceRecords_TenantId_EmployeeId_Date",
                schema: "__tenant__",
                table: "Workforce_AttendanceRecords",
                columns: ["TenantId", "EmployeeId", "Date"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workforce_AttendanceSessions_TenantId_EmployeeId_WorkDate",
                schema: "__tenant__",
                table: "Workforce_AttendanceSessions",
                columns: ["TenantId", "EmployeeId", "WorkDate"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Workforce_ComplianceRules_PolicyId",
                schema: "__tenant__",
                table: "Workforce_ComplianceRules",
                column: "PolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_Workforce_Schedules_TenantId_StartDate_EndDate",
                schema: "__tenant__",
                table: "Workforce_Schedules",
                columns: ["TenantId", "StartDate", "EndDate"]);

            migrationBuilder.CreateIndex(
                name: "IX_Workforce_ShiftAssignments_EmployeeId_Date",
                schema: "__tenant__",
                table: "Workforce_ShiftAssignments",
                columns: ["EmployeeId", "Date"]);

            migrationBuilder.CreateIndex(
                name: "IX_Workforce_ShiftAssignments_ScheduleId",
                schema: "__tenant__",
                table: "Workforce_ShiftAssignments",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_Workforce_ShiftDefinitions_TenantId_Code",
                schema: "__tenant__",
                table: "Workforce_ShiftDefinitions",
                columns: ["TenantId", "Code"],
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Analytics_ProcessedEventLog",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Analytics_ProjectMetrics",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Holidays",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "LeaveBalanceEntries",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "LeavePolicies",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "LeaveRequests",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Projections_LeaveBalances",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Timesheets",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Workforce_AttendanceAnomalies",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Workforce_AttendanceEvents",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Workforce_AttendanceRecords",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Workforce_ComplianceRules",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Workforce_ShiftAssignments",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Workforce_ShiftDefinitions",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "HolidayCalendars",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "LeaveBalances",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Workforce_AttendanceSessions",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Workforce_AttendancePolicies",
                schema: "__tenant__");

            migrationBuilder.DropTable(
                name: "Workforce_Schedules",
                schema: "__tenant__");
        }
    }
}
