# ADR 0001 — Multi-tenancy: shared database, separated schema, single EF model

- **Status:** Accepted
- **Date:** 2026-05-02
- **Deciders:** Solo founder

## Context

Karamchari is a multi-tenant B2B SaaS EMS targeting IT firms, schools, and hospitals. Tenants are isolated organizations whose data must never bleed across boundaries. We need an isolation model that:

- scales to thousands of tenants without operational explosion
- gives strong tenant boundaries by default
- has a defense-in-depth failsafe for the inevitable bug
- keeps connection-pool behaviour healthy in Azure SQL Elastic Pools
- works with a single deployable Modular Monolith

## Decision

### 1. Isolation model: shared database, **separated schema** per tenant

Tables for tenant `acme` live in schema `tenant_acme`; tables for `globex` in `tenant_globex`. All tenants share a database (or an elastic pool of databases), and the schema is the tenant discriminator.

### 2. Connection strings: one per database / elastic pool — **never** per tenant

A per-tenant connection string in a shared-DB model gives no real isolation (same DB, same login underneath), explodes Key Vault secret count, and fragments connection pools. The connection string is fetched once via Managed Identity and shared across tenants.

### 3. Tenant resolution: JWT primary; subdomain and trusted header are corroboration only

`ITenantProvider` is request-scoped and reads the tenant from the validated JWT (`tenant_id` claim). If a subdomain or trusted `X-Tenant-Id` header is present, the provider asserts they agree with the JWT. Disagreement, missing claim, or a header on a non-trusted edge produces `TenantResolutionException` (mapped to 401/403). There is **no default tenant fallback** — ever.

### 4. Schema switching: single EF model + `DbCommandInterceptor` rewrite

EF Core caches one compiled model per `IModelCacheKeyFactory` key. Including `TenantId` in the cache key produces *N* compiled models for *N* tenants — memory and warmup costs grow linearly with tenant count, which is unacceptable for a SaaS targeting thousands of tenants.

Instead:

1. The base `KaramchariDbContext` declares all tables under a placeholder schema `__tenant__` via a model convention.
2. `TenantSchemaCommandInterceptor` (`IDbCommandInterceptor`) rewrites the placeholder to the active tenant's schema in `ReadingCommand` / `NonQueryExecuting` / `ScalarExecuting` hooks, immediately before execution.
3. The interceptor refuses to execute if `ITenantProvider` cannot resolve a tenant, and validates the tenant schema name against a strict regex (`^tenant_[a-z0-9_]{1,50}$`) before substitution.

Model count remains exactly 1. Tenant context is enforced at the moment of execution, not at model build time.

### 5. RLS as failsafe

`RlsSessionContextInterceptor` (`DbConnectionInterceptor`) sets `SESSION_CONTEXT(N'TenantId', @id)` on every connection open. SQL Server Row-Level Security policies on every tenant table compare the row's `TenantId` column against this session value. Even if schema rewriting failed, a cross-tenant read or write would be blocked by RLS.

### 6. Tenant-scoped Outbox

Domain events are harvested by `DomainEventOutboxInterceptor` (`SaveChangesInterceptor`) inside the same transaction as the aggregate writes. The outbox lives in the tenant's own schema, so the same schema rewriting applies.

## Alternatives considered

| Option                                              | Why rejected                                                                 |
| --------------------------------------------------- | ---------------------------------------------------------------------------- |
| Database-per-tenant                                 | Operationally heavy at scale; expensive on Azure SQL; overkill for our trust model |
| Single schema, `TenantId` column on every row only  | Easy to forget the filter; one bug = a leak; weaker boundary by default      |
| `IModelCacheKeyFactory` keyed by tenant             | Model explosion; memory grows linearly with tenant count                     |
| SQL `default_schema` per login                      | Requires per-tenant DB principals; doesn't scale; ties us to SQL Server login config |
| Header-only or subdomain-only tenant resolution     | Spoofable; doesn't survive cross-service hops; weak under Zero Trust         |

## Consequences

**Positive**

- Single compiled EF model — flat memory regardless of tenant count.
- Strong-by-default isolation: separate schemas + RLS as failsafe.
- One connection string to manage; healthy pool behaviour.
- Tenant resolution is verifiable end-to-end via signed JWT.

**Negative / accepted risks**

- Schema name rewriting is "string-replace adjacent" and must be guarded carefully (placeholder uniqueness, schema name regex, fail-fast on missing tenant). Mitigation: targeted placeholder, comprehensive interceptor unit tests, RLS as defense in depth.
- Adding a tenant requires running schema-creation migrations against the shared DB. Mitigation: provisioning saga that runs idempotent migration scripts under a privileged identity.
- Cross-tenant reporting (e.g., admin analytics) requires deliberately escaping the tenant context. Mitigation: dedicated `IAdminTenantProvider` used only by explicitly-authorized admin endpoints, with audit logging.
