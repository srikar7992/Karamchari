# ADR 0008 — Notification Orchestration Platform

- **Status:** Accepted
- **Date:** 2026-05-08
- **Deciders:** Solo founder

## Context

The performance platform generates high notification volume:
- Review deadlines and overdue submissions
- Goal approval requests and status changes
- Feedback request delivery and reminders
- Calibration panel actions
- Promotion approvals and stage transitions
- KPI anomaly alerts
- Growth plan task reminders
- SLA breach escalations

Without a centralized notification platform, each workflow saga would emit notifications
directly, leading to: duplicate reminders, no rate limiting, no quiet-hours support,
no user preference management, and no observability.

## Decision

Create a dedicated `Karamchari.Notifications` bounded context that owns all notification
orchestration. All other BCs publish integration events; the Notifications BC consumes
them and manages delivery.

**Architecture layers:**

```
[Source BC saga/consumer]
    → publishes NotificationTriggerIntegrationEvent
    → [Notifications BC consumer]
        → evaluates user preferences + quiet hours + rate limits
        → writes NotificationMessage aggregate (idempotent, outbox-safe)
        → NotificationMessage.Dispatch() raises NotificationDispatched domain event
        → DeliveryDispatcher routes to channel adapters (email, push, in-app)
        → DeliveryAttempt recorded with result + retry policy
```

**Key components:**

| Component | Responsibility |
|---|---|
| `NotificationTemplate` | Tenant-brandable, localizable template with variable slots |
| `UserNotificationPreference` | Per-user channel preferences, quiet hours, digest schedule |
| `NotificationMessage` | Aggregate representing one notification instance; owns delivery attempts |
| `NotificationDeliveryAttempt` | Append-only record of delivery attempts with result |
| `DigestScheduler` | Groups low-priority notifications into daily/weekly digests |
| `RateLimitPolicy` | Per-category per-user limits to prevent storm |

## Operational Safeguards

- **Idempotency key:** `TenantId:TriggerEventId:Category:RecipientId`. Duplicate events
  produce no second notification.
- **Rate limiting:** category-scoped leaky bucket per user per hour.
- **Quiet hours:** delivery deferred to next active window based on user timezone.
- **Notification storm prevention:** if a single event would trigger >N notifications
  (e.g., cycle completed notifying 500 employees), the Notifications BC fans out via
  a background job, not inline processing.
- **Dead letter handling:** failed deliveries after max retries are flagged in the
  `NotificationMessage` aggregate status and alert the platform operations team.

## Channels

Phase 1: In-app (stored in `NotificationsDbContext`) + email (Azure Communication Services).
Phase 2: Push (Azure Notification Hubs), Slack/Teams webhook (configurable per tenant).

## Channel Adapter Contract

```csharp
interface INotificationChannelAdapter
{
    NotificationChannel Channel { get; }
    Task<DeliveryResult> DeliverAsync(
        NotificationDeliveryPayload payload,
        CancellationToken ct);
}
```

Each adapter is registered per channel. The Notifications BC dispatches via the correct
adapter based on the channel. Adapters are replaceable without touching domain logic.

## Consequences

- All source BCs must publish `NotificationTriggerIntegrationEvent` (or per-event
  subtype) via the MassTransit outbox. They must NOT call notification services directly.
- Notification templates live in the `Notifications` BC database. HR admins can edit
  them via the HR workspace UI.
- User preferences are owned by the Notifications BC. They are populated via an HR
  consumer that seeds defaults when a new employee is onboarded.
- The Notifications BC adds `NotificationsDbContext` with its own EF migration history.
  MassTransit outbox tables are shared (`dbo`) as with all other BCs.
