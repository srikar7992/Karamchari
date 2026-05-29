# ADR 0002 — Row-Level Security as the database-layer failsafe

- **Status:** Accepted
- **Date:** 2026-05-02
- **Deciders:** Solo founder
- **Supersedes:** none
- **Related:** ADR 0001 (Multi-tenancy)

## Context

ADR 0001 established that Karamchari uses a shared database with a separated schema per tenant, that schema rewriting happens at the EF Core command interceptor layer, and that connection strings are never per-tenant. That model leans on application-layer correctness — a single bug in the schema rewriter, an unguarded raw SQL call, or a misconfigured migration could cross tenant boundaries.

We need a database-layer enforcement that is independent of any application code path.

## Decision

Every tenant table is protected by SQL Server **Row-Level Security** (RLS). RLS is the failsafe; the schema rewriter and `TenantStampingInterceptor` are the application-layer twins that produce clearer errors and faster failures.

### Predicate function — single, shared, no escape hatch

Predicate function lives in a dedicated `security` schema (not `dbo`) so it is unambiguously infrastructure and outside any tenant rewrite path:

```sql
CREATE FUNCTION [security].[fn_tenant_access](@TenantId NVARCHAR(64))
RETURNS TABLE
WITH SCHEMABINDING
AS
RETURN
    SELECT 1 AS fn_result
    WHERE @TenantId IS NOT NULL
      AND @TenantId = CAST(SESSION_CONTEXT(N'TenantId') AS NVARCHAR(64));
```

There is **no** `IS_ROLEMEMBER('db_owner') = 1` admin escape. Any code that legitimately needs to read or write across tenants — the outbox relay, an admin reporting job — must iterate tenants and set `SESSION_CONTEXT(N'TenantId')` for each one before its DB work. This forces every cross-tenant operation to be explicit and auditable.

`SESSION_CONTEXT` is set by `RlsSessionContextInterceptor` on every connection open with `@read_only = 1`, so transient code on the same connection cannot tamper with the value.

### Per-tenant policy — FILTER + BLOCK predicates on every table

Each tenant gets one security policy, named `security.TenantPolicy_<tenantId>`, applied to every tenant-owned table in that tenant's schema. Per table we add five predicates:

| Kind   | Lifecycle      | Why                                                       |
| ------ | -------------- | --------------------------------------------------------- |
| FILTER | (every read)   | Hide rows whose `TenantId` doesn't match the session.     |
| BLOCK  | AFTER  INSERT  | Reject inserts whose row's `TenantId` doesn't match.      |
| BLOCK  | BEFORE UPDATE  | The current row must belong to the active tenant.         |
| BLOCK  | AFTER  UPDATE  | The updated row must still belong to the active tenant.  |
| BLOCK  | BEFORE DELETE  | The current row must belong to the active tenant.         |

Together these mean: even raw ADO.NET that bypasses every interceptor we wrote cannot read, insert, update, or delete data belonging to another tenant.

### Tenant table registry — a single source of truth

A new tenant table that doesn't get added to the registry won't be covered by RLS. To prevent that being a quiet failure, every bounded context's `AddKaramchari{Context}` extension calls `services.RegisterTenantTable("...")` for every table it owns. The registry is a singleton populated at startup and consumed by `RlsScriptGenerator` at provisioning time. Adding a table to the registry is treated like a security commit.

### Provisioning workflow

1. **Database bootstrap (once per database deploy).** `RlsScriptGenerator.BuildBootstrapScripts()` returns:
   - `00_security_schema.sql` — creates the `security` schema if missing.
   - `01_predicate_function.sql` — creates `[security].[fn_tenant_access]`.

   Both scripts are idempotent and safe to re-run on every release.

2. **Tenant provisioning (once per new tenant).** A future `Karamchari.Provisioning` service runs, in this order:
   1. `CREATE SCHEMA [tenant_<id>]`.
   2. Apply EF migrations with the placeholder schema rewritten to the tenant schema (so the migration's `__tenant__.Employees` becomes `tenant_<id>.Employees`).
   3. Run `RlsScriptGenerator.BuildTenantPolicyScript(tenant)` — generates and applies the per-tenant security policy covering every registered table.

3. **Application runtime.** `RlsSessionContextInterceptor` sets `SESSION_CONTEXT(N'TenantId', @id, @read_only = 1)` on every connection open. Every query and write transparently respects RLS without any per-call awareness from application code.

### Why `MassTransit` outbox tables are pinned to `dbo`

MassTransit's transactional-outbox tables (`InboxState`, `OutboxMessage`, `OutboxState`) are infrastructure shared across tenants, not tenant-owned data. The relay process drains them globally and would otherwise need to re-establish session context for every tenant on every poll. We pin them to `dbo` in `HRDbContext.OnDomainModelCreating`, which keeps them outside the schema-rewrite path (the rewriter only matches `__tenant__`). The serialized message payloads themselves carry the tenant context that consumers re-establish before doing any DB work.

## Alternatives considered

| Option                                                            | Why rejected                                                                 |
| ----------------------------------------------------------------- | ---------------------------------------------------------------------------- |
| Rely on schema rewriting + `TenantStampingInterceptor` only       | Application-layer enforcement only; one bypass = one breach.                 |
| RLS on every column rather than via `TenantId` and SESSION_CONTEXT| Far more DDL; doesn't add isolation; `SESSION_CONTEXT` is the standard idiom.|
| Admin-role escape hatch in the predicate                          | Removes the forcing function for explicit cross-tenant operations; one accidental `db_owner` grant = silent leak. |
| Per-tenant outbox queues for MassTransit                          | Forces a relay-per-tenant model, breaks `IPublishEndpoint`'s scope, no isolation gain since payloads carry tenant context anyway. |

## Consequences

**Positive**

- Cross-tenant reads and writes are blocked at the database layer regardless of application bugs.
- The forcing function for cross-tenant operations is explicit and auditable (must call `sp_set_session_context` per tenant).
- The registry pattern makes "did we cover this table with RLS?" a build-time question, not a runtime accident.

**Negative / accepted risks**

- The relay process must scope itself per tenant when draining the outbox. We accept this — it makes cross-tenant work visible in logs.
- `TenantStampingInterceptor` and the RLS BLOCK predicate are two enforcement layers for the same invariant, which means two places to reason about. Mitigation: the interceptor produces clearer errors; RLS is the safety net. Both must agree.
- New tables require both an EF configuration and a `RegisterTenantTable` call. Mitigation: code review checklist + future analyzer that flags any `EntityTypeConfiguration` for an `ITenantOwned` type without a corresponding `RegisterTenantTable` call.
