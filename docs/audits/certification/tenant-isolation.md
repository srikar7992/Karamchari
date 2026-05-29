# Phase 6 — Multi-Tenant Isolation Certification

**Result: ✅ PASS at data/RLS layer (strong); ⚠️ message-bus isolation GAP; ⛔ HTTP cross-tenant test blocked by auth.**

## Tenancy model (verified in DB)
- Schema-per-tenant: `tenant_dev`, `tenant_acme`, `tenant_contoso` — **136 tables each**.
- **Row-Level Security is live:**
  ```sql
  SELECT name, is_enabled, is_schema_bound FROM sys.security_policies;
  -- TenantPolicy_dev      1 1
  -- TenantPolicy_acme     1 1
  -- TenantPolicy_contoso  1 1
  SELECT COUNT(*) FROM sys.security_predicates;   -- 1680
  -- predicate function: security.fn_tenant_access (SCHEMABINDING)
  ```
  All three policies are **enabled and schema-bound**, with 1,680 filter/block predicates wired to `security.fn_tenant_access`.

## In-process certification suite
`Karamchari.TenantIsolationCertification` — **610 tests, 0 failed** (`evidence/test-run.log`). This is the strongest single body of evidence in the platform and covers cross-tenant read/write/search isolation, RLS session context, and tenant-scoped queries across modules.

## Cache isolation (Redis)
`TenantCacheGuard` enforces tenant-namespaced keys and throws `CachePoisoningDetectionException` on any get/set whose key tenant ≠ current tenant. Verified by code review + Core unit tests. See `redis.md`.

## ⚠️ Message-bus isolation GAP (HIGH)
`MassTransitExtensions.cs`: the **RabbitMQ** transport branch (the one actually used locally/prod) configures only `cfg.Host(...)` + retry. It **does not apply** `TenantConsumeFilter` / `TenantPublishFilter` / `TenantSendFilter`, whereas the **InMemory** and **AzureServiceBus** branches do. Consequence: messages published/consumed over RabbitMQ are **not guaranteed to carry or enforce tenant context** → potential cross-tenant message processing. Tenant isolation is enforced in DB and cache but **not on the RabbitMQ bus**.

## ⛔ Blocked: live HTTP cross-tenant probe
"Create Employee A in tenant A; verify tenant B cannot access" via HTTP is blocked by the auth defect (no tokens obtainable). The DB-level RLS + 610 isolation tests provide strong compensating evidence, but the through-the-API cross-tenant test for Update/Delete/Search/Bulk must be re-run after auth is fixed.

## Verdict
Data-layer isolation (RLS) and cache isolation are **excellent and verified**. Message-bus tenant filtering on RabbitMQ is a **HIGH gap**. Live HTTP cross-tenant verification is pending the auth fix.
