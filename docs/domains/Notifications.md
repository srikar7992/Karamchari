# Notifications Domain

## Extracted Knowledge

| Required Item | Evidence-backed Content |
|---|---|
| Purpose | Manages notification templates, user preferences, messages, digest batches, delivery attempts, email/in-app channels, realtime push, rendering, orchestration, and digest aggregation. Evidence: `src/Backend/Karamchari.Notifications/Persistence/NotificationsDbContext.cs:27`. |
| Business Objectives | UNKNOWN beyond delivering event-driven notifications and digests. |
| Core Concepts | Template, preference, message, digest batch, delivery attempt, channel, category, status. |
| Aggregates / Entities | `NotificationTemplate`, `UserNotificationPreference`, `NotificationMessage`, `DigestBatch`. |
| Value Objects | `NotificationIntent`, `EmailMessage`, delivery result/provider result. Evidence: `src/Backend/Karamchari.Notifications/Orchestration/NotificationIntent.cs`, `src/Backend/Karamchari.Notifications/Email/*.cs`. |
| State Machines | `NotificationStatus`, `NotificationChannel`, `NotificationCategory`, `DigestBatchStatus`, `DeliveryResult`, `DigestFrequency`. Evidence: `src/Backend/Karamchari.Notifications/Domain/*.cs`. |
| Events | Consumes performance and payroll events including calibration finalized, feedback request, goal approval, goal cycle activated, FnF, disbursement, arrear, reimbursement, salary revision. Evidence: `src/Backend/Karamchari.Notifications/Consumers/*.cs`. |
| Commands | Notification inbox endpoints expose read side; send/orchestration commands are event-driven and internal. |
| Queries | Inbox and unread count. Evidence: `src/Backend/Karamchari.Api/BFF/Notifications/NotificationCenterEndpoints.cs:21`. |
| Business Rules / Invariants / Validation | Orchestrator, template renderer, digest aggregator exist; full delivery policy UNKNOWN. |
| Calculation Rules | Digest aggregation exists; schedule/frequency exact behavior UNKNOWN. |
| Ownership Rules | User preferences are per user/tenant; authority UNKNOWN. |
| Dependencies | Performance, Payroll, SignalR, email provider abstraction, MassTransit. Evidence: consumers, `NotificationHub`, `IEmailProvider`. |
| External Integrations | Email provider abstraction and `NullEmailProvider` exist; production email provider UNKNOWN. Evidence: `src/Backend/Karamchari.Notifications/Email/IEmailProvider.cs`, `src/Backend/Karamchari.Notifications/Email/NullEmailProvider.cs`. |
| Examples | `GET /api/v1/notifications`, `GET /api/v1/notifications/unread-count`. |
| Failure Scenarios | Delivery attempts and delivery result modeled; retry/escalation UNKNOWN. |
| Known Limitations | No dedicated notification tests found. |
