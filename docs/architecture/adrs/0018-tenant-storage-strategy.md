# ADR 0018 — Tenant storage strategy: scaling path beyond a single shared database

- **Status:** Proposed (pending architecture review)
- **Date:** 2026-06-21
- **Deciders:** Platform architecture
- **Supersedes:** none — **extends** [ADR 0001](0001-multi-tenancy-model.md) and [ADR 0002](0002-row-level-security.md)
- **Driver:** Certification finding **C-1** (`CERTIFICATION.md`) — single shared SQL database with schema-per-tenant is a hard scalability ceiling and an all-tenant single point of failure.

## Context

ADR 0001 chose **shared database, separated schema per tenant** with a single EF model and a placeholder-schema command interceptor, and explicitly anticipated *"a database (or an elastic pool of databases)"* and *"connection string fetched once via Managed Identity"*.

The certification audit found the **deployed reality has drifted from ADR 0001**:

| ADR 0001 intent | Deployed reality (`infrastructure/bicep/modules/sql.bicep`) |
|---|---|
| Elastic pool of databases | **One** `Standard` (cap 10) single database |
| Managed Identity connection | SQL admin login + password |
| Healthy pool behaviour at thousands of tenants | All tenants in one DB; firewall open to all Azure IPs |

Concrete ceilings of the current single-DB model (each tenant ≈ 177 tables + cloned indexes + RLS predicates, per `TenantProvisioningService`):

| Tenants | Tables (~177/tenant) | Outcome |
|---:|---:|---|
| 100 | 17,700 | Fine |
| 1,000 | 177,000 | Metadata pressure, slow `sys.*`, plan-cache bloat, multi-hour backup/restore, long provisioning DDL |
| 10,000 | 1.77M | Beyond practical single-DB limits |
| 100,000 | 17.7M | Infeasible |

Plus: whole-DB backup/restore (cannot restore one tenant), no read-replica routing, single write primary, single failure domain for 100% of tenants.

## Decision drivers

- Scale to **100k+ tenants / millions of employees** without architectural collapse.
- Preserve the proven isolation model (4 boundaries, RLS fail-closed, 560-test suite) — do **not** weaken tenant isolation.
- Per-tenant restore (single-tenant PITR) and blast-radius containment.
- Keep the modular monolith deployable; avoid an N-compiled-model explosion (the reason ADR 0001 used a placeholder schema).
- Incremental migration — **no big-bang rewrite.**

## Options considered

### Option A — Keep shared database + schema-per-tenant (status quo)
- **Pros:** zero migration; current code works; unified PITR.
- **Cons:** all ceilings above; single SPOF; whole-DB restore; metadata explosion. **Fails the C-1 scale and blast-radius requirements.**

### Option B — Database-per-tenant
- **Pros:** strongest isolation; per-tenant restore; per-tenant scaling/region; blast radius = one tenant.
- **Cons:** thousands of databases to manage; cost/overhead for tiny tenants; cross-tenant platform queries (billing, ops) become fan-out; connection-pool fragmentation if done naively. Overkill for the long tail of small tenants.

### Option C — Hybrid: pooled shared databases for small tenants, dedicated databases for large tenants  ✅ **RECOMMENDED**
- Small/medium tenants → **schema-per-tenant inside Azure SQL Elastic Pools**, many pooled databases, a bounded number of tenant schemas per database.
- Large/regulated tenants → **dedicated database** (optionally dedicated region).
- A **Tenant Placement / shard-map directory** maps `tenantId → { databaseConnection, region, tier }`.
- **Pros:** scales to 100k+ (add pools/databases horizontally); per-tenant or per-pool restore; cost-efficient for the long tail; isolates noisy/large tenants; aligns with ADR 0001's stated elastic-pool intent. Most enterprise HCM SaaS converges here.
- **Cons:** requires a placement service + connection routing + rebalancing tooling; provisioning and migrations must run per database.

## Decision

Adopt **Option C (hybrid)**. Target end-state:

1. **Tenant directory database** (`platform_directory`) holds the shard map: `Tenants(TenantId, SchemaName, DatabaseKey, Region, Tier, Status)` and `Databases(DatabaseKey, ConnectionRef, PoolName, MaxTenants, Region)`. `ConnectionRef` points at a Key Vault secret / Managed Identity target — **never** a per-tenant connection string (ADR 0001 §2 preserved). (Field names illustrative; finalised in Phase 1.)
2. **Connection routing replaces fixed connection string.** `ITenantProvider` resolution feeds a new `ITenantConnectionResolver` that returns the tenant's database connection from the directory (cached). The existing `__tenant__` placeholder-schema interceptor stays for within-database schema isolation; RLS stays as failsafe (ADR 0002 preserved). Model count stays 1.
3. **Provisioning** evolves from `CREATE SCHEMA` + clone to: pick/allocate a pool database → create tenant schema there (or `CREATE DATABASE` from a dacpac template for dedicated tenants) → apply indexes + RLS → write directory row. Retire the `dbo` template tables (orphan-row hazard, finding M-2) in favour of a dacpac/bacpac template.
4. **Placement policy:** new tenants default to a pool with free capacity in their region; tenants exceeding size/throughput thresholds are migrated to a dedicated database by a (later) rebalancer.
5. **Infrastructure:** `sql.bicep` → Elastic Pool resource(s), zone-redundant tier, Managed Identity auth, private endpoint, no public firewall (reconciles drift; also addresses C-3 / H-3).

## Migration plan (incremental — no big-bang)

1. **Phase 0 (now):** Accept this ADR. Reconcile `sql.bicep` to elastic pool + zone-redundant + Managed Identity + private endpoint (also C-3/H-3). No app code change yet; current single DB becomes "pool database #1".
2. **Phase 1:** Introduce `platform_directory` + `ITenantConnectionResolver`, returning the single existing connection for all tenants (behaviour-preserving). Ship behind a flag; verify isolation suite still green.
3. **Phase 2:** Stand up a second pool database; route *new* tenants there via placement. Provisioning writes directory rows. Prove a tenant lands in DB #2 and stays isolated.
4. **Phase 3:** Per-tenant/per-database backup + **single-tenant restore drill** (closes part of C-3).
5. **Phase 4:** Dedicated-database path for large tenants + rebalancer/migrator tooling.

Do **not** begin Phase 1 code until this ADR is **Accepted** by architecture review.

## Consequences

- **Positive:** removes the C-1 ceiling and SPOF; enables per-tenant restore and regional placement; aligns deployment with ADR 0001 intent.
- **Negative / cost:** new directory + routing + rebalancing components; migrations and provisioning run per database; cross-tenant platform reporting must fan out or move to a warehouse (tracked under the reporting/OLAP item in `CERTIFICATION.md` P2).
- **Risk controls:** every phase re-runs the 560-test `TenantIsolationCertification` suite; RLS fail-closed remains the failsafe throughout.

## Verification

- Phase 1: isolation suite green with resolver returning the legacy connection.
- Phase 2: provision 1,000 synthetic tenants across ≥2 pool databases; assert isolation + flat provisioning latency; no cross-database leakage.
- Phase 3: single-tenant restore produces correct row counts without touching other tenants.
