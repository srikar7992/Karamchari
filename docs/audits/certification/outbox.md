# Phase 9 — Outbox Certification

**Result: ⚠️ PARTIAL.** Outbox infrastructure present and relay running; live transactional dispatch not exercised (auth-blocked).

## Infrastructure present (DB-verified)
```
dbo.OutboxState            (MassTransit transactional outbox)
dbo.OutboxMessage          (MassTransit)
dbo.InboxState             (MassTransit inbox / idempotent consume)
dbo.OutboxProcessingState  (custom relay state)
dbo.OutboxDeadLetter       (custom DLQ)
```
- `OutboxRelayDbContext` migrated; `OutboxRelayService` runs on a 5s interval (BatchSize=100, MaxRetries=5, circuit breaker configured in `OutboxRelay` settings).
- Relay polling observed live:
  ```
  SELECT TOP 1 * FROM [dbo].OutboxState WITH (UPDLOCK, ROWLOCK, READPAST) ORDER BY Created
  ```
  (UPDLOCK/ROWLOCK/READPAST = correct competing-consumer pattern for multi-instance safety.)

## EF outbox registered for the requested modules
`AddEntityFrameworkOutbox<TDbContext>` is registered for **15 contexts** including **Billing**, **Forecasting**, **Workflow** (the three named in the phase), plus HR, Payroll, FinancialOps, PSA, etc. `UseBusOutbox()` enabled in dev.

## Live transactional flow (requested)
| Check | Status | Notes |
|---|---|---|
| Transaction commits | ⛔ Blocked | No business write could be triggered (auth `302`) |
| Outbox row created | ⛔ Not reached | `OutboxMessage`/`OutboxState` = 0 rows |
| Dispatcher executes | ✅ (idle) | Relay polls correctly; nothing to dispatch |
| Consumer receives | ⛔ Not verifiable | Worker not running (see `rabbitmq.md`) |
| No duplicate delivery | ✅ (design) | `InboxState` + MassTransit dedup + `UseMessageRetry` |
| Simulate failures | ✅ (tests) | `FinancialChaosTests` 5/5 passing; circuit breaker + DLQ configured |

## Verdict
The transactional outbox pattern is **correctly architected and partially exercised** (relay loop, dedup, DLQ, circuit breaker). Full commit→outbox-row→dispatch→consume→dedup chain is **not certifiable live** until auth is fixed and the Worker is run.
