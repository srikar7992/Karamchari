# Karamchari.TimeAttendance

Time, attendance, and leave — the largest module: punch ingestion with biometric devices and geo zones, shift definitions and workforce scheduling, attendance sessions/records/anomalies/violations/scores, regularization, the full leave domain (types, policies with versions, requests, balances, accruals, encashment, approvals with delegation and SLA escalation, carry-forward, comp-off, blackout periods, forecasts), timesheets, statutory registers, retention/purge, legal holds, investigation cases, Bradford factor, burnout risk, workforce availability/demand/supply/capacity analytics, absence cases with return-to-work assessments, and bulk operations.

Domain documentation: [docs/domains/TimeAttendance.md](../../../../../docs/domains/TimeAttendance.md)

## Domain ownership
Everything between a punch event and an approved timesheet/leave outcome. Payroll consumes the outcomes; Forecasting and Intelligence consume the analytics events.

## Events published
Defined in `Karamchari.TimeAttendance.Contracts`:

- `TimesheetApprovedIntegrationEvent`
- `AttendancePeriodFinalizedIntegrationEvent`, `AttendanceRecordFinalizedIntegrationEvent`
- `BiometricEnrollmentCompletedIntegrationEvent`
- `GeoFraudDetectedIntegrationEvent` (published from `Services/PunchIngestionService.cs`)
- `ConsecutiveAbsenceEscalatedIntegrationEvent` (`Services/ConsecutiveAbsenceMonitor.cs`)
- `AttendanceExceptionRaisedIntegrationEvent` (`Services/AttendanceProcessingEngine.cs`)
- `RegularizationApprovedIntegrationEvent`, `RegularizationRejectedIntegrationEvent`

Shared leave/shift events (`LeaveRequestApprovedIntegrationEvent`, `LeaveCancelledIntegrationEvent`, `ShiftSwapApprovedIntegrationEvent`, ...) are defined in `Karamchari.Core.Contracts`.

## Events consumed
`WorkflowCompletedIntegrationEvent`, `TenantProvisionedIntegrationEvent`, `TimesheetApproved`, `TimesheetApprovedIntegrationEvent` (analytics), `ShiftUnassigned`, `ScheduledShiftCreated`, `InvoiceIssuedIntegrationEvent`, `PaymentReceivedIntegrationEvent` (billing analytics) — see `Consumers/`.

## Database tables
Source of truth: `Persistence/TimeAttendanceDbContext.cs` and `Migrations/`. 70 sets. Highlights:

`PunchEvent`, `BiometricTemplate`, `BiometricDevice`, `GeoZone`, `ShiftDefinition`, `WorkforceSchedule`, `AttendanceSession`, `AttendanceRecord`, `AttendanceAnomaly`, `AttendancePolicy`, `AttendanceViolation`, `RegularizationRequest`, `Timesheet`, `LeaveType`, `LeavePolicy`, `LeaveRequest`, `LeaveBalance`, `LeaveAccrualSchedule`, `LeaveEncashment`, `LeaveApproval`, `ApprovalDelegation`, `CompOffGrant`, `LeaveCarryForward`, `BradfordFactorScore`, `BurnoutRiskScore`, `StatutoryRegisterEntry`, `LegalHold`, `RetentionPolicy`, `PurgeJob`, `WorkforceAvailabilityIndex`, `WorkforceDemand`, `WorkforceSupply`, `CapacityGap`, `CoverageRisk`, `AbsenceCase`, `ReturnToWorkAssessment`, `ProcessedEventLog`, and more — read the DbContext for the full list.

## Project dependencies
- `Karamchari.Core`
- `Karamchari.Core.Contracts`
- `Karamchari.TimeAttendance.Contracts`
- `Karamchari.Billing.Contracts`

## Wiring
Self-registered via `DependencyInjection/TimeAttendanceServiceCollectionExtensions.cs`, called from the API host.

## Testing
```powershell
dotnet test tests/Backend/Karamchari.TimeAttendance.Tests/Karamchari.TimeAttendance.Tests.csproj
```
