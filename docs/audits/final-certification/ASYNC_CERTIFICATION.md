# ASYNC MESSAGING CERTIFICATION (Phase 6)

**Program:** Final Platform Certification & Human Handoff
**Date:** 2026-05-30 · **Auditor stance:** hostile / independent — no inherited certification.
**Method:** runtime evidence against freshly built+deployed containers (API `3d47a2d7…`, Worker `d90fea0e…`).

> **Classification of the pre-existing `docs/incidents/D1/FINAL_D1_CERTIFICATION.md` ("CLOSED — Lead Engineer: Gemini CLI"): was `NOT VERIFIED` at the start of this audit.** Its headline DB proof claimed `tenant_* = 10, dbo = 0`; the *actual* database at audit time was `dbo = 7 (TenantId=system), tenant_* = 0`, and the running containers (`982fa33a…` worker) predated the fix source by hours — i.e. the "certified" code was **never deployed**. This audit independently re-built, re-deployed, and re-verified from scratch. **Result of independent re-verification: D1 is genuinely fixed.** The earlier certification reached the right conclusion about the *code* but was not backed by a deployed, evidenced runtime.

---

## Result: D1 (async tenant-isolation defect) — **VERIFIED FIXED / CLOSED**

The original defect: an `EmployeeOnboarded` event published by tenant `dev` produced a
`PayrollProfiles` row in the **`dbo`** template schema stamped `TenantId=system`, never in `tenant_dev`.
Root cause (certified in `D1_ROOT_CAUSE_REPORT.md`): the tenant header was **never attached to the
message** because the bus-level publish filters did not run on the EF-outbox publish path → headerless
wire message → consumer fell back to `system`/`dbo`.

### The fix (verified by inspection AND runtime)
The **generic** `TenantPublishFilter<T>` / `TenantSendFilter<T>` are now registered
(`cfg.UsePublishFilter(typeof(TenantPublishFilter<>), context)`), which — unlike the non-generic filter —
**runs during EF Outbox message capture (in-request, where `TenantExecutionContext.Current` is set by
`TenantAuthorizationMiddleware`)**. The filter stamps `MT-Tenant-Id`, a full `MT-Execution-Envelope`, and
an HMAC-SHA256 `MT-Context-Signature`. The header is persisted by the outbox and survives delivery. The
Worker's `TenantConsumeFilter<T>` extracts + validates the signature and establishes the tenant scope
before the consumer runs.

---

## Chain-of-custody re-verification (every hop, runtime evidence)

| Hop | Before (root-cause report) | After (this audit) | Evidence |
|---|---|---|---|
| Publish → wire (RabbitMQ) | headers = `['MT-Activity-Id','publishId']` — **no tenant** | headers include **`MT-Tenant-Id=dev`**, `MT-Context-Signature`, `MT-Execution-Envelope={"TenantId":"dev","SchemaName":"tenant_dev",…}`, `MT-Source-Service=Karamchari.Api` | RabbitMQ mgmt API peek (`ack_requeue_true`) on parked msg, worker stopped — `scripts/runtime/d1-wire-proof.sh` |
| Worker consume | fell back to `system` | established `tenant_dev` from header | no `_error`/`_skipped` queues; row landed correctly |
| Persistence | `dbo.PayrollProfiles`, `TenantId=system` | **`tenant_dev.PayrollProfiles`, `TenantId=dev`** | SQL below |

### Multi-tenant concurrent isolation (3 tenants onboarded simultaneously)

`scripts/runtime/d1-e2e-proof.sh` — onboarded `acme`, `contoso`, and a 2nd `dev` employee concurrently
(+ 1 `dev` parked from the wire-proof). Physical row counts by schema (`sys.partitions`):

| Schema | PayrollProfiles before → after | Employees |
|---|---|---|
| `tenant_dev` | 0 → **2** | 7 |
| `tenant_acme` | 0 → **1** | 1 |
| `tenant_contoso` | 0 → **1** | 1 |
| `dbo` | 7 → **7** (no new rows; all `TenantId=system`) | **0** |

Row-level stamp (with `SESSION_CONTEXT('TenantId')` set per tenant; RLS fail-closed otherwise hides rows):

```
tenant_dev      : dev      EF38C4B4-…  (== wire-proof msg employee id)   dev  F892A5F9-…
tenant_acme     : acme     292A5E18-…
tenant_contoso  : contoso  D2EFD75A-…
```

Each async side-effect is (1) in the correct `tenant_*` schema, (2) stamped with the correct `TenantId`
(never `system`), (3) traceable to the exact employee onboarded under that tenant's JWT. **Zero new rows
in `dbo`. Zero cross-tenant rows. Zero poison/error messages.**

### Closure criteria (from D1_ROOT_CAUSE_REPORT.md) — all met
- [x] `dev`/`acme`/`contoso` create `PayrollProfiles` exclusively in own `tenant_*` schema — VERIFIED
- [x] none in `dbo`/`system` — VERIFIED (dbo unchanged)
- [x] across HTTP → outbox → RabbitMQ → Worker → persistence — VERIFIED (wire header + DB landing)
- [x] concurrent multi-tenant — VERIFIED (3 tenants at once)
- [x] worker restart / durable queue — VERIFIED (worker stopped during wire-proof; parked message survived and was consumed into `tenant_dev` on restart; cold start ~24s)

---

## Tier 2/3 metadata & integrity (VERIFIED present on wire)
`MT-Correlation-Id`, `MT-Conversation-Id`, `MT-Request-Id`, `MT-Source-Service`, `MT-Timestamp`,
`MT-Context-Version`, `MT-Execution-Source` all observed on the wire message, plus the HMAC
`MT-Context-Signature`. The consume filter runs in **Enforce** mode (default): a message missing
`MT-Tenant-Id` or a valid signature is rejected (`MalformedTenantMessageException`), not silently written.

## NOT VERIFIED this pass (scope honesty)
- **Other event types / consumers**: only the `EmployeeOnboarded → PayrollProfile` path (the original D1
  repro) was exercised end-to-end. The fix is infrastructure-level (generic filters apply to all
  publish/consume), so it *should* cover all async boundaries, but each consumer was not individually run.
- **Tampering rejection (signature) at runtime**: the code path exists and is unit-tested; a live
  modified-header rejection was not executed this pass → see SECURITY/CHAOS phases.
- **Duplicate-delivery / replay** and **broker-restart event flow**: deferred to CHAOS_CERTIFICATION.
- **Outbox crash-mid-publish replay**: not executed.

## Data-remediation follow-up (not a new defect)
`dbo.PayrollProfiles` retains **7 orphaned rows stamped `TenantId=system`** from the pre-fix era. These
are stale leaked data, harmless to isolation going forward but should be purged/migrated. Tracked as a
cleanup item, not a blocker.

## Verdict
**Phase 6 Async Messaging: D1 CLOSED — VERIFIED FIXED at runtime.** The platform's most severe open
tenant-isolation defect is resolved and independently re-certified with wire-level + database-level
evidence. Remaining async items above are NOT VERIFIED (lower severity) and routed to later phases.
