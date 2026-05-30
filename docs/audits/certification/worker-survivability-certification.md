# Worker Survivability & Messaging Chaos Certification

**Date:** 2026-05-30 · **Method:** real chaos against the running containerized Worker + RabbitMQ + SQL.
Stance: prove behavior under failure, not on the happy path.

---

## Defects found (this pass)

### D1 — Async tenant context lost in the Worker (HIGH — isolation + correctness) — FIXED
**Symptom:** an `EmployeeOnboarded` event published by tenant `dev` was consumed and produced a
`PayrollProfiles` row stamped **`TenantId = system`** in the **`dbo` template schema**, not in
`tenant_dev`. Confirmed by SQL: `dbo.PayrollProfiles = 2` (both rows `TenantId=system`), while
`tenant_dev.PayrollProfiles = 0`.

**Impact:**
- *Correctness:* the tenant's own schema never receives the side effect (e.g. payroll for dev
  employees would not find their profile).
- *Isolation:* all tenants' async-generated rows pile into the shared `dbo` template under `system` —
  a cross-tenant co-mingling of data created via the async path.

**Root cause:** the Worker's MassTransit configuration (`WorkerServiceCollectionExtensions.cs`)
configured `UsingRabbitMq` with only `ConfigureEndpoints` — it did **not** apply
`UseConsumeFilter<TenantConsumeFilter>`. The API applies it; the Worker did not. Without it, the
consumer runs with no ambient `TenantExecutionContext`, so `ITenantProvider` returns the `system`/`dbo`
fallback and the schema interceptor rewrites `[__tenant__]` → `dbo`.

**Fix (applied):** wired `UseConsumeFilter<TenantConsumeFilter>` + `UsePublishFilter<TenantPublishFilter>`
+ `UseSendFilter<TenantSendFilter>` + `UseMessageRetry` into the Worker bus config (both RabbitMQ and
in-memory transports), mirroring the API. The consume filter calls
`new TenantExecutionContext(envelope).Establish()`, restoring tenant scope before consumers run so the
schema/RLS interceptors target the originating tenant. Builds 0/0.

**Runtime re-verification:** see "AFTER" below (Worker image rebuilt + redeployed).

### D2 — `IdempotentRequests` table never provisioned (MEDIUM) — OPEN
**Symptom:** the Worker logs, every cleanup cycle:
`Microsoft.Data.SqlClient.SqlException … Invalid object name 'dbo.IdempotentRequests'` →
`Error occurred while cleaning up idempotent requests.` Confirmed: `OBJECT_ID('dbo.IdempotentRequests')`
and `tenant_dev.IdempotentRequests` are both **null** (table does not exist).

**Impact:** the `IdempotencyCleanupWorker` errors continuously, and idempotency dedup that relies on this
table is non-functional. (MassTransit's own `InboxState` dedup IS present and functioning — see below — so
consumer-level duplicate suppression still has a layer, but the bespoke `IdempotentRequests` mechanism is
dead.) **Not yet fixed** — requires adding the table to provisioning/migrations or making the cleanup
worker tolerate its absence. Tracked as open.

---

## Scenarios executed

| Scenario | Result | Evidence |
|---|---|---|
| **Durable queue while consumer down** | ✅ PASS | Worker stopped; `EmployeeOnboarded` created → RabbitMQ queue `messages=1, consumers=0`; no loss; `PayrollProfiles` unchanged. |
| **Worker restart recovery** | ✅ PASS (slow) | After `docker start`, worker reconnected (`consumers=1`) and drained the queue (`messages=0`). Reconnect/consume took **~20–27 s** (cold start: module init + migrations) — functional but not instant. |
| **Side effect after recovery** | ⚠️ wrong schema (D1) | Event consumed but row landed in `dbo`/`system` — see D1. Re-verified AFTER fix below. |
| **InboxState dedup present** | ✅ | Worker actively polls `dbo.InboxState` (MassTransit inbox) — duplicate-delivery suppression layer exists. |
| Poison message → DLQ (`_error`/`_skipped`) | ⏳ NOT YET EXECUTED | No `_error` queues observed during this pass; explicit poison-message injection pending. |
| RabbitMQ restart recovery | ⏳ PARTIAL | Earlier failure-injection showed bus health recovers ~10–17 s after broker restart; dedicated post-restart event-flow check pending. |
| Outbox replay | ⏳ NOT PROVEN | EF outbox is registered for 14 contexts; explicit crash-mid-publish replay not executed. |

---

## AFTER (fix re-verification) — D1 STILL OPEN

Two correct, necessary fixes were applied and **deployed** (Worker image rebuilt to `982fa33a595c`,
verified running), then the onboarding event was repeated:

1. **Wire `TenantConsumeFilter` (+publish/send/retry) into the Worker bus** (`WorkerServiceCollectionExtensions.cs`).
2. **`HttpTenantProvider` honours ambient `TenantExecutionContext.Current`** before the `system`/`dbo`
   fallback (`HttpTenantProvider.cs`) — so consumer-established tenant scope reaches the schema interceptor.

**Result: the defect PERSISTS.** A new dev-tenant employee ("Tenant Routed") still produced a
`dbo.PayrollProfiles` row stamped `TenantId=system`; `tenant_dev.PayrollProfiles` remained **0**.
Worker SQL interceptor logs show **every** rewrite (including the consume window) targeting schema `dbo`.

### Precise remaining root cause (third layer)
The two fixes only help **if** the consumed message carries the tenant header and the consume filter engages.
Evidence indicates it does not: the consume completed (row inserted) **without** the filter rejecting a
missing-tenant message and **without** establishing `tenant_dev`. The most probable remaining cause is on
the **publish side** — the `EmployeeOnboarded` domain event is published through a path that does **not**
run `TenantPublishFilter` (the domain-event dispatcher / `IPublishEndpoint` bypassing the bus publish
filter, or the EF-outbox relay republishing without the original tenant header), so the message reaches
the Worker **without `TenantMessageHeaderKeys.TenantId`** and the consumer falls back to `system`.

**Next step to close D1 (NOT yet done):** inspect a live `EmployeeOnboarded` message's headers on RabbitMQ
to confirm the tenant header is absent; then ensure the publish path stamps it (route the dispatcher
through `TenantPublishFilter`, or set the header at dispatcher/outbox enqueue time).

---

## Verdict
- Durable delivery + restart recovery: **PASS** (recovery correct but ~20–27 s cold — ops note).
- **Async tenant isolation (D1): CONFIRMED OPEN / HIGH.** Two necessary fixes landed (consume-filter
  wiring + ambient-context honour); a third (publish-side tenant header) remains. **NOT closed** — runtime
  still writes async data to `dbo`/`system`.
- Idempotency table (D2): **OPEN** (`IdempotentRequests` missing).
- Poison/DLQ, outbox replay, broker-restart event flow: **NOT PROVEN** — not yet executed.

**Worker Survivability verdict: NOT CERTIFIED** — a HIGH async tenant-isolation defect is open and reproduced at runtime.
