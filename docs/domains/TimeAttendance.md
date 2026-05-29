# TimeAttendance Domain

## Extracted Knowledge

| Required Item | Evidence-backed Content |
|---|---|
| Purpose | Manages attendance, rosters/schedules, holidays, leave, leave balances, timesheets, workforce analytics, and processed event logs. Evidence: `src/Backend/Karamchari.TimeAttendance/Persistence/TimeAttendanceDbContext.cs:33`. |
| Business Objectives | UNKNOWN beyond attendance capture, leave processing, roster scheduling, and timesheet approval exposed by endpoints and events. |
| Core Concepts | Shift definition, workforce schedule, attendance session/record/anomaly/policy, holiday calendar, leave policy/request/balance, timesheet, project metrics, processed event log. |
| Aggregates / Entities | DbSets in `TimeAttendanceDbContext`: `ShiftDefinition`, `WorkforceSchedule`, `AttendanceSession`, `AttendanceRecord`, `AttendanceAnomaly`, `AttendancePolicy`, `HolidayCalendar`, `LeavePolicy`, `LeaveRequest`, `LeaveBalance`, `LeaveBalanceReadModel`, `Timesheet`, `ProjectMetrics`, `ProcessedEventLog`. |
| Value Objects | Time entries and leave balance entries exist. Evidence: `src/Backend/Karamchari.TimeAttendance/Domain/Timesheets/TimeEntry.cs`, `src/Backend/Karamchari.TimeAttendance/Domain/Leaves/LeaveBalanceEntryType.cs`. |
| State Machines | `AttendanceStatus`, `AttendanceSource`, `AnomalyStatus`, `TimeEntryStatus`, `TimesheetStatus`, `LeaveRequestStatus`, `BalanceStatus`, compliance enums. Evidence: `src/Backend/Karamchari.TimeAttendance/Domain/**`. |
| Events | `TimesheetApprovedIntegrationEvent`, `RawPunchReceivedEvent`, `LiveAttendanceUpdatedEventV1`; local `TimesheetApproved`. Evidence: `src/Backend/Karamchari.TimeAttendance.Contracts/TimesheetApprovedIntegrationEvent.cs`, `src/Backend/Karamchari.Core.Contracts/IntegrationEvents/RawPunchReceivedEvent.cs`. |
| Commands | Leave request endpoint and attendance check-in/check-out endpoints. Evidence: `src/Backend/Karamchari.Api/BFF/Attendance/LeaveEndpoints.cs:24`, `src/Backend/Karamchari.Api/BFF/Attendance/AttendanceEndpoints.cs:30`. |
| Queries | Live sessions, anomalies, shifts, schedules, holidays, leave balances, current timesheet, leave by id/my leaves. Evidence: `src/Backend/Karamchari.Api/BFF/Attendance/*.cs`. |
| Business Rules / Invariants / Validation | Leave balance `Consume` exists and workflow completion consumer consumes approved leave days. Full policy math is UNKNOWN. Evidence: `src/Backend/Karamchari.TimeAttendance/Domain/Leaves/LeaveBalance.cs:74`, `src/Backend/Karamchari.TimeAttendance/Consumers/WorkflowCompletedConsumer.cs:60`. |
| Calculation Rules | Project metrics and capacity provider exist; full formulas UNKNOWN. Evidence: `src/Backend/Karamchari.TimeAttendance/Services/ICapacityProvider.cs`, `src/Backend/Karamchari.TimeAttendance/Domain/Analytics/ProjectMetrics.cs`. |
| Ownership Rules | Tenant-scoped via shared tenant infrastructure; employee/manager approval ownership UNKNOWN. |
| Dependencies | Workflow completion, billing invoice/payment events, tenant provisioning, MassTransit outbox. Evidence: consumers in `src/Backend/Karamchari.TimeAttendance/Consumers`. |
| External Integrations | UNKNOWN beyond MassTransit, SQL, and shared infrastructure. |
| Examples | `POST /api/v1/workforce/attendance/check-in`, `POST /api/v1/workforce/attendance/check-out`, `POST /api/v1/leaves/request`, `GET /api/v1/time/timesheets/current`. |
| Failure Scenarios | Attendance anomalies are modeled and resolvable. Evidence: `src/Backend/Karamchari.Api/BFF/Attendance/AttendanceEndpoints.cs:33`, `src/Backend/Karamchari.Api/BFF/Attendance/AttendanceEndpoints.cs:34`. |
| Known Limitations | Tests exist under `src/Backend/Karamchari.TimeAttendance.Tests`; mutation and integration risk coverage are not certified. |
