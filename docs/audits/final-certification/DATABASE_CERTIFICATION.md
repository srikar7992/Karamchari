# DATABASE CERTIFICATION (Phase 5)

**Date:** 2026-05-30 · **Method:** live SQL Server inspection + runtime isolation proof.

## Schema & multi-tenancy — VERIFIED
- **Schema-per-tenant**, live: `dbo` (template) + `tenant_dev`, `tenant_acme`, `tenant_contoso` (3 tenants).
- **Schema interceptor** rewrites `[__tenant__]` → tenant schema at runtime — proven by D1 test (dev/acme/
  contoso writes each landed in their own `tenant_*` schema; **`dbo` received 0 new rows**).
- **Row-Level Security fail-closed — VERIFIED live:** `sys.security_policies = 3`, **`sys.security_predicates = 1,680`**.
  A `sa` SELECT against `tenant_*` tables returned **0 rows without `SESSION_CONTEXT('TenantId')`** set —
  RLS denies by default. With the correct session context, only that tenant's rows are visible.

## Migrations / contexts — VERIFIED present
- **19 DbContexts**, **29 EF migration classes**. Migrations are applied automatically at container startup
  (API/Worker logs show schema present and queries succeeding). All tenant schemas carry the full table set
  (PayrollProfiles, Employees, InboxState, OutboxMessage/State, etc.).
- MassTransit transactional outbox tables (`OutboxMessage`, `OutboxState`, `InboxState`) present and active
  (Worker actively polls `dbo.InboxState`; outbox metrics emitted).

## Integrity
- Tenant rows correctly stamped with their `TenantId` (dev/acme/contoso) — never `system` post-fix.
- FKs/constraints/decimals: addressed in prior workstreams (WS7/WS8); not re-audited exhaustively this pass.

## NOT VERIFIED (Phase 5 explicit asks)
- **Backup / restore capability**: not executed → NOT VERIFIED.
- **Corruption tests / rollback tests / restore tests**: not executed → NOT VERIFIED.
- Index coverage / query-plan review under load: not performed.
- **Pre-existing data-cleanup item:** `dbo.PayrollProfiles` holds **7 orphaned `TenantId=system` rows** from
  the pre-fix era — stale leaked data; should be purged. Not a live isolation risk (no new leakage).

## Verdict: **Phase 5 — VERIFIED for schema ownership, tenant isolation, RLS, and migrations.**
Backup/restore/corruption/rollback testing is NOT VERIFIED and is a DR-phase prerequisite.
