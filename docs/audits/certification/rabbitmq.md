# Phase 8 — RabbitMQ Certification

**Result: ⚠️ PARTIAL.** Broker connectivity ✅; end-to-end publish→consume→notification **not verifiable** with the API alone, plus two configuration findings.

## What works
- API connects to RabbitMQ at startup: `Bus started: rabbitmq://localhost/`, `Connected: guest@localhost:5672/`.
- Health: `Messaging:RabbitMQ` = `Healthy`, `masstransit-bus` = `Healthy (Ready)`.
- MassTransit **EntityFramework outbox** is registered for **15 DbContexts** (`AddEntityFrameworkOutbox<...>`, with `UseBusOutbox()` in dev).

## Findings
- **HIGH — RabbitMQ branch skips tenant filters.** In `MassTransitExtensions.cs`, the `UsingRabbitMq` branch sets only `cfg.Host()` + `UseMessageRetry`. It omits `TenantConsumeFilter`/`TenantPublishFilter`/`TenantSendFilter` that the InMemory and AzureServiceBus branches apply. Tenant context is not enforced on the bus over RabbitMQ. (Also tracked in `tenant-isolation.md`.)
- **MEDIUM — No receive endpoints in API.** The API host does not call `ConfigureEndpoints(context)`. RabbitMQ shows **0 application exchanges and 0 queues** (only the bus's temporary reply queue). Consumers are intended to run in `Karamchari.Worker`, which was **not running** during certification.

## PayrollProcessedEvent end-to-end (requested)
| Step | Status | Evidence |
|---|---|---|
| Trigger a payroll run (publishes event) | ⛔ Blocked | `POST /api/payroll/runs` requires auth → `302` |
| Outbox record created | ⛔ Not reached | `dbo.OutboxMessage` / `dbo.OutboxState` = 0 rows (no business txn ran) |
| Message published to broker | ⛔ Not reached | — |
| Consumer executed | ⛔ Not verifiable | Worker not running; API declares no consumer endpoints |
| Notification created | ⛔ Not reached | — |
| Logs generated | Partial | OutboxRelay polling logs present (`SELECT ... FROM OutboxState WITH (UPDLOCK,ROWLOCK,READPAST)`) every 5s |

## In-process compensating evidence
- `Karamchari.FinancialChaosTests` — 5 passed (messaging/resilience scenarios).
- `Karamchari.Core.IntegrationTests` — 3 passed.
- Outbox dispatcher logic exercised by tests (see `outbox.md`).

## Verdict
Broker is reachable and the outbox relay runs, but **end-to-end event flow cannot be certified** without (a) the auth fix to trigger a real business event and (b) the Worker running to consume it. Two real config findings (tenant filters, receive endpoints) must be resolved.
