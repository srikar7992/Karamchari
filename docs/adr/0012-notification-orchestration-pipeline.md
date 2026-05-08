# ADR-0012 — Notification Orchestration Pipeline

**Status:** Accepted  
**Date:** 2026-05-08  
**Author:** Srikar

---

## Context

Performance workflows generate notifications across multiple categories:
review assignments, calibration sessions, promotion approvals, goal cycles,
and feedback requests. Without orchestration these would be scattered across
individual consumers as direct "send email" calls — impossible to govern,
prone to storms, and not user-preference-aware.

Enterprise notification systems must handle:
- Thousands of employees per review cycle
- Reminder and escalation sequences
- Quiet hours and digest batching
- Duplicate event replay without duplicate delivery
- Partial-channel failure without losing in-app delivery
- Real-time streaming (unread count badge updates)

---

## Decision

**Implement a three-layer notification pipeline:**

```
Domain Event
  → MassTransit Consumer (intent emission only)
  → INotificationOrchestrator (dedupe, prefs, template, fanout)
    → INotificationChannelAdapter[] (per-channel delivery)
  → NotificationsDbContext (append-only DeliveryAttempts)
  → INotificationPushService (SignalR push after save)
```

### Layer 1 — Intent Emission (Consumers)

Each consumer converts one integration event into one `NotificationIntent`
and calls `INotificationOrchestrator.OrchestrateAsync`. Consumers contain
no delivery logic. They are idempotent via `ProcessedEventLog` (uses the
event's `EventId` / correlation ID as idempotency key before calling the
orchestrator).

### Layer 2 — Orchestration (`NotificationOrchestrator`)

Single transactional unit per intent:

1. **Dedupe check** — query `NotificationMessages` for existing row with
   matching `IdempotencyKey`. Return early if found.
2. **Preference resolution** — load `UserNotificationPreference` for
   `(TenantId, RecipientEmployeeId, Category)`. Fall back to defaults
   if no row exists.
3. **Quiet hours check** — if current time falls in quiet window and
   `DigestEnabled = false`, schedule delivery after quiet-hours-end.
4. **Digest routing** — if `DigestEnabled = true` for the category, call
   `QueueForDigest()` on the message; skip immediate channel fanout.
5. **Template resolution** — load `NotificationTemplate` by
   `(TenantId, Code, Channel, Locale)`. Fall back to `en-US` if locale
   template absent. Throw `InvalidOperationException` if no template found
   (seed templates must be present at deployment).
6. **Rendering** — `ITemplateRenderer.Render(template, variables)` uses
   Regex `{{VarName}}` substitution. Never executes code; purely string
   replacement. Missing variables rendered as empty string (logged as warning).
7. **Save** — `NotificationMessages.Add(message)` + `SaveChangesAsync`.
   MassTransit outbox ensures the save is atomic with the consumer ack.
8. **Channel fanout** — call each enabled `INotificationChannelAdapter`.
   Each adapter records its own `DeliveryAttempt`. Email failure does NOT
   roll back the in-app delivery.
9. **Real-time push** — call `INotificationPushService.PushAsync` after
   all adapters succeed. Uses SignalR group `{tenantId}:{employeeId}`.

### Layer 3 — Channel Adapters

- `InAppChannelAdapter` — marks message `Delivered` immediately (stored in DB).
- `EmailChannelAdapter` — stub in Phase 1; logs + marks delivered. Will wire
  to Azure Communication Services in Phase 2.
- Channel adapters are registered as `IEnumerable<INotificationChannelAdapter>`
  in DI; the orchestrator iterates all adapters whose `Channel` matches user
  preference enablement.

### Idempotency Key Format

```
{TenantId}:{TriggerEventId}:{IntentType}:{RecipientEmployeeId}
```

Replay of the same event produces the same key → duplicate blocked at step 1.

### SignalR Hub

- Hub class: `NotificationHub` in `Karamchari.Notifications.RealTime`.
- Group per user: `{tenantId}:{employeeId}` — tenant-isolated.
- Methods pushed: `UnreadCountUpdated(int count)`, `NotificationReceived(dto)`.
- Client reconnect: re-joins group on `OnConnectedAsync`; groups are ephemeral
  so no replay risk.

---

## Alternatives Considered

| Option | Rejected Reason |
|--------|----------------|
| Consumer calls email SDK directly | Ungoverneable; no dedupe; no preferences |
| Separate notification microservice | Violates modular-monolith constraint (ADR-0001) |
| Polling for new notifications | SignalR group push is simpler and lower-latency |
| Persisting digest queue in-memory | Not durable; lost on restart |

---

## Consequences

**Good:**
- Single orchestration point for all notification governance
- Replay-safe by design (idempotency key check)
- Channel failure isolated (partial delivery tracked, not total rollback)
- User preferences enforced before any delivery attempt
- Real-time UX without polling

**Accepted risks:**
- Template must be seeded before first event arrives; missing template throws
- Quiet-hours delivery scheduling is best-effort (no separate scheduler yet —
  deferred notifications are visible in DB but not auto-retried until Phase 2
  background reminder service)
- Digest engine (batching, grouping, timezone scheduling) is Phase 2
