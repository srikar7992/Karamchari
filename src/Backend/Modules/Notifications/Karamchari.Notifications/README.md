# Karamchari.Notifications

Notification delivery: templates, user notification preferences, notification messages, and digest batches. Subscribes to events across Performance, Payroll, and TimeAttendance and turns them into user-facing notifications.

Domain documentation: [docs/domains/Notifications.md](../../../../../docs/domains/Notifications.md)

## Domain ownership
Notification rendering, preference resolution, and delivery state. Source-of-truth business state stays in the publishing modules.

## Events published
None. This module has no Contracts project; it is a pure subscriber.

## Events consumed
Performance events: `ReviewSubmittedIntegrationEvent`, `ReviewAssignedIntegrationEvent`, `PromotionApprovedIntegrationEvent`, `GoalCycleActivatedIntegrationEvent`, `GoalApprovalRequiredIntegrationEvent`, `FeedbackRequestCreatedIntegrationEvent`, `EmployeeCalibrationFinalizedIntegrationEvent`

Payroll events (`Consumers/PayrollNotificationConsumer.cs`): `FnFSettlementApprovedIntegrationEvent`, `FnFSettlementDisbursedIntegrationEvent`, `DisbursementBatchCompletedIntegrationEvent`, `DisbursementBatchFailedIntegrationEvent`, `ArrearCalculationApprovedIntegrationEvent`, `ReimbursementApprovedIntegrationEvent`, `SalaryRevisionApprovedIntegrationEvent`

Attendance events (`Consumers/AttendanceNotificationConsumer.cs`): `GeoFraudDetectedIntegrationEvent`, `ConsecutiveAbsenceEscalatedIntegrationEvent`, `AttendanceExceptionRaisedIntegrationEvent`, `RegularizationApprovedIntegrationEvent`, `RegularizationRejectedIntegrationEvent`

## Database tables
Source of truth: `Persistence/NotificationsDbContext.cs` and `Migrations/`.

- `NotificationTemplate`
- `UserNotificationPreference`
- `NotificationMessage`
- `DigestBatch`

## Project dependencies
- `Karamchari.Core`
- `Karamchari.Core.Contracts`
- `Karamchari.Performance.Contracts`
- `Karamchari.Payroll.Contracts`
- `Karamchari.TimeAttendance.Contracts`

## Wiring
Self-registered via `DependencyInjection/NotificationsServiceCollectionExtensions.cs`, called from the API host.

## Testing
```powershell
dotnet test tests/Backend/Karamchari.Notifications.Tests/Karamchari.Notifications.Tests.csproj
```
