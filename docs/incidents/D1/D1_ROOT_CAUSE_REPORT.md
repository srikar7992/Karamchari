# D1 — Async Tenant Isolation Defect — Root Cause Report

**Severity:** HIGH · **Classification:** Multi-Tenant Boundary Violation · **Status:** ROOT CAUSE IDENTIFIED
**Method:** forensic chain-of-custody (observe, don't guess). No production code changed during this investigation.

## Incident
A `dev`-tenant employee-onboarding event is consumed successfully by the Worker, but the resulting
`PayrollProfiles` row is written to **`dbo` (template schema) with `TenantId = system`** instead of
`tenant_dev`. Reproduced repeatedly (employees "Async Probe", "Survivor One", "Tenant Fixed",
"Tenant Routed", "Forensic Probe" — all in `dbo`/`system`; `tenant_dev.PayrollProfiles = 0`).

## Chain of custody — where TenantId survives and where it dies

| Hop | Observation | Tenant present? | Evidence |
|---|---|---|---|
| 1 HTTP request | JWT tenant claim `dev`; resolves correctly | ✅ | HTTP authz tests (security-audit) |
| 2 Command (`EmployeeService.OnboardEmployeeAsync`) | `tenantProvider.GetCurrentTenantId()` = `dev`; passed into the event payload | ✅ | `EmployeeService.cs:29,41` |
| 3 Publish call | `IPublishEndpoint.Publish(EmployeeOnboardedIntegrationEvent)` — **routed through the MassTransit EF outbox** (by design; see method XML doc) | ✅ (in scope) | `EmployeeService.cs:39`, doc lines 20–25 |
| 5 Publish pipeline | **`TenantPublishFilter` NEVER executes** — `injected-count=0` AND `no-context-warn-count=0` in API logs. `TenantSendFilter` also `0`. | ❌ **LOST HERE** | API container logs |
| 6 **RabbitMQ message** | Headers = `['MT-Activity-Id','publishId']` only. **No `TenantId`, no `ExecutionEnvelope`, no correlationId.** | ❌ absent | RabbitMQ mgmt API `get` on queue `EmployeeOnboarded` |
| 8 Worker consume filter | Receives message with no tenant header → cannot establish `tenant_dev` | ❌ | worker logs: all schema rewrites = `dbo` |
| 9 Consumer execution | Runs under `system` fallback (no ambient tenant) | ❌ | — |
| 10/11 Persistence | Schema interceptor rewrites `[__tenant__]` → `dbo`; row stamped `TenantId=system` | ❌ | `dbo.PayrollProfiles` rows = `system` |

**Exact loss point: Hop 5 (publish).** The tenant header is never attached to the message.

## Root cause (certified): **C — Tenant lost during publish**

`EmployeeService` publishes the integration event through the **MassTransit Entity Framework outbox**
(the method is explicitly designed for transactional outbox ordering). The platform attaches tenant
context via **bus-level** filters — `cfg.UsePublishFilter<TenantPublishFilter>(context)` and
`cfg.UseSendFilter<TenantSendFilter>(context)` on the bus factory. **These bus-pipeline filters are not
applied to the EF outbox's own delivery pipeline.** Proven: with a real publish, neither filter logged a
single line (not even the `null`-context warning), and the wire message carried no tenant headers.

Consequently:
- The message is emitted **without `ExecutionContextHeaders.TenantId`**.
- The Worker's `TenantConsumeFilter` has nothing to extract → no ambient `TenantExecutionContext`.
- `HttpTenantProvider` (no HTTP context in the Worker) returns the `system`/`dbo` fallback.
- The schema interceptor writes to `dbo`; the row is stamped `TenantId=system`.

### Why the two earlier fixes did not close it (correct but insufficient)
1. *Wire `TenantConsumeFilter` into the Worker* — necessary so the consumer **uses** a tenant header,
   but there is no header to use.
2. *`HttpTenantProvider` honours ambient `TenantExecutionContext`* — necessary so the consumer's DB
   writes follow the established tenant, but the context is never established because (1)'s input is missing.

Both are prerequisites for the real fix; neither addresses the publish-side header loss. They are retained.

## Remediation options (NOT yet implemented — pending decision)
Tenant propagation must be infrastructure-level and outbox/transport-agnostic (no per-event/per-module
logic). Candidate approaches, in order of preference:

- **A. Attach tenant headers at publish time so the outbox persists them.** The EF outbox stores the
  message headers present when it captures the message (in-request, where `TenantExecutionContext.Current`
  = `dev`). A publish-time mechanism that runs **before** outbox capture — e.g. an `IPublishObserver` /
  message-send-topology header initializer, or applying the tenant filter to the outbox's publish path —
  ensures the header is stored and survives delivery. (Bus publish filters run at delivery, after capture,
  which is why they miss.)
- **B. Configure the outbox delivery pipeline with the tenant filters** (if the MassTransit version exposes
  outbox pipeline configuration).

The correct option must be **verified at runtime**, not by inspection (this defect already defeated two
inspection-correct fixes).

## Closure criteria (unchanged)
D1 is CLOSED only when, with runtime evidence: tenant `dev`/`acme`/`contoso` each create
`PayrollProfiles` **exclusively** in their own `tenant_*` schema (none in `dbo`/`system`), across
HTTP → outbox → RabbitMQ → Worker → persistence, including concurrent multi-tenant, duplicate delivery,
and worker/broker restart.

## Current verdict: **CLOSED — VERIFIED FIXED** (2026-05-30, independent re-verification)

The publish-side fix (generic `TenantPublishFilter<T>` running at EF-outbox capture) was built into fresh
containers (API `3d47a2d7…`, Worker `d90fea0e…`), deployed, and re-verified at runtime against all closure
criteria above:
- Wire: the `EmployeeOnboarded` RabbitMQ message now carries `MT-Tenant-Id=dev` + signed
  `MT-Execution-Envelope` (was `['MT-Activity-Id','publishId']` only — the exact loss point).
- DB: concurrent `dev`/`acme`/`contoso` onboards landed `PayrollProfiles` in `tenant_dev`(2)/`tenant_acme`(1)/`tenant_contoso`(1),
  each stamped with its own `TenantId`; **`dbo` unchanged at 7 (old `system` leak), 0 new rows**; no
  `_error`/`_skipped` queues.
- Worker restart + durable queue: parked message survived worker stop/start and consumed into `tenant_dev`.

Full evidence: `docs/audits/final-certification/ASYNC_CERTIFICATION.md`
(repro: `scripts/runtime/d1-wire-proof.sh`, `scripts/runtime/d1-e2e-proof.sh`).

Remaining lower-severity async items (duplicate-delivery, broker-restart event flow, outbox crash replay)
are routed to CHAOS_CERTIFICATION. Data-cleanup of the 7 historical `dbo`/`system` rows is a follow-up,
not a blocker.
