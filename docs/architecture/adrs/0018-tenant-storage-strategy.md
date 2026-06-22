# ADR 0018 — Tenant storage strategy: scaling path beyond a single shared database

- **Status:** Accepted (architecture review complete, 2026-06-22) — implementation pending
- **Date:** 2026-06-21 (accepted 2026-06-22)
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

## Placement authority

Tenant placement is **decided by software, not by humans or tribal knowledge.** Three roles, cleanly separated:

| Role | Component | Responsibility |
|---|---|---|
| **Authority** | Placement Service (in the provisioning path) | Evaluates placement rules; chooses target database/pool for a new tenant and for any move. |
| **Source of truth** | `platform_directory` | Holds the canonical `tenantId → { databaseKey, region, tier }` map. Nothing routes off anything else. |
| **Enforcement** | Rebalancer (Phase 4) | Periodically re-evaluates live tenants against thresholds and executes moves via the migration runbook below. |

Placement rules are **data, not code** — a `PlacementRules` row-set in `platform_directory`, evaluated at provision time and by the rebalancer:

| Signal | Threshold → dedicated database |
|---|---|
| Contract tier | `Enterprise` / `Regulated` flag → dedicated at birth |
| Employee count | > 25,000 |
| Schema size | > 50 GB |
| Sustained write throughput | > configured share of pool DTU/vCore |
| Data residency | region with no eligible pool → dedicated |

**Audit requirement.** Every placement decision (initial allocation and every move) writes an **immutable** `PlacementAudit` record so the question "why was this tenant moved?" is answered with evidence, not inference:

| Field | Meaning |
|---|---|
| `TenantId` | tenant affected |
| `OldPlacement` | `{databaseKey, pool, region, tier}` before (null for initial) |
| `NewPlacement` | `{databaseKey, pool, region, tier}` after |
| `ReasonCode` | enum: `InitialProvision`, `SizeThreshold`, `ThroughputThreshold`, `ResidencyRequirement`, `TierUpgrade`, `ManualOverride` |
| `MetricsSnapshot` | the metric values that triggered the decision |
| `DecisionTimestamp` | UTC, ISO-8601 |
| `DecisionSource` | `PlacementService` \| `Rebalancer` \| `Operator:<id>` |

Records are append-only; no update or delete path.

## Placement exit criteria (dedicated → shared)

The reverse path is **explicitly prohibited.** Once a tenant is promoted to a dedicated database it **remains dedicated** for its lifetime.

Rationale: a tenant reaches dedicated status only by exceeding size/throughput thresholds or by a contractual `Enterprise`/`Regulated` flag. Demoting back into a shared pool would (a) re-introduce the noisy-neighbour and blast-radius risk the promotion removed, (b) require a second online migration with its own cutover risk for no scale benefit, and (c) for regulated tenants, may violate the isolation guarantee they are paying for. The only situation that would shrink a tenant below threshold is off-boarding, which is a deletion, not a demotion.

If a future business case ever requires demotion, it must arrive as a **superseding ADR** defining thresholds, migration process, and approval requirements — operators must not invent a demotion path ad hoc.

## Online migration runbook (shared → dedicated)

A tenant moves with **per-tenant** downtime only — no maintenance window for the platform, no impact on other tenants in the source pool. Six gated steps:

1. **Provision** — create the empty dedicated database from the dacpac template; apply schema + indexes + RLS. Directory row `Status = Migrating`.
2. **Seed** — bulk-copy the tenant's schema into the target (single-schema BACPAC, or `bcp` / Azure Data Factory). Record a baseline watermark (rowversion / CDC LSN). Source stays live and writable.
3. **Sync** — CDC / replication tails source-schema changes into the target until replication lag ≈ 0.
4. **Cutover** — write-freeze **for this `tenantId` only** (scoped via `ITenantConnectionResolver`): drain in-flight requests, flush the final CDC delta, flip `Tenants.DatabaseKey` → target, invalidate the resolver cache, write `PlacementAudit`. Other tenants never see a freeze.
5. **Validate** — row-count + checksum per table (source vs target); run the isolation smoke subset; smoke read/write against the target. **Cutover commit is gated on validation passing.**
6. **Rollback** — until validation passes and a grace window elapses, the source schema is left intact; rollback = flip `DatabaseKey` back to source and drop the target. Only after the grace window does the source schema get dropped.

**Cutover SLO (certification gate):** the step-4 per-tenant write-freeze has **target < 60 s, hard maximum 5 min.** A migration whose freeze exceeds the maximum is a failed migration and must roll back. This makes "migration succeeded" objective, not subjective.

**Pre-production gate:** `MigrationDryRunCompleted` — a full provision→seed→sync→cutover→validate→rollback cycle against synthetic tenants in non-production — **must** be recorded before the first production tenant is migrated (Phase 4 entry condition).

## Analytics sourcing (physically distributed tenants)

Once tenants span pooled databases, dedicated databases, and regions, **cross-tenant analytics MUST NOT query operational databases.** Querying operational stores cross-DB / cross-region couples reporting to the sharding topology, fans out unboundedly, and loads OLTP primaries.

**Decision — analytics is sourced from the event stream into a warehouse, not from operational DBs:**

```
Operational DBs  →  Outbox / CDC  →  Event stream  →  Analytics warehouse (tenantId-partitioned)
```

- The **event stream is the source of truth for analytics.** The platform already emits integration events (Recruitment, onboarding saga, etc.) via the transactional outbox.
- The **warehouse** (Synapse / Fabric / Snowflake-class) holds cross-tenant reporting data, partitioned by `tenantId`.
- **Operational databases serve single-tenant transactional reads only** — never the cross-tenant reporting source.
- Region-distributed tenants: stream ships to one warehouse region where residency permits, otherwise per-region warehouse + federated query for global rollups.

This removes a major objection to physical tenant distribution (C-1) and decouples OLAP entirely from the storage topology. Supersedes the earlier "fan-out or warehouse later" placeholder and closes the reporting/OLAP item tracked under `CERTIFICATION.md` P2.

## Per-tenant restore (product capability, not just a backup detail)

Per-tenant point-in-time restore is the **strongest enterprise advantage of the hybrid model** — bigger than raw scale. It must be treated as a first-class, SLA-backed capability, not an implementation footnote.

- **Dedicated-database tenant** → native Azure SQL PITR: restore one tenant to a point in time with **zero impact on any other tenant.**
- **Pooled tenant** → restore the pool database to a *side* copy, extract the single tenant schema (BACPAC / `bcp`), re-import into the live pool database. Other schemas in the pool are never touched.

The current single-shared-DB model **cannot** do this at all (whole-DB restore only) — this is a net-new capability the hybrid model unlocks.

**SLA:** per-tenant restore RTO/RPO is a stated target tied to tenant tier. See `docs/operations/recovery/rto-rpo-targets.md` (per-tenant restore row). For `Regulated` tenants it is a contractual deliverable. This capability is surfaced in four places: this ADR, the recovery strategy, commercial tiering, and certification evidence.

## Consequences

- **Positive:** removes the C-1 ceiling and SPOF; enables per-tenant restore and regional placement; aligns deployment with ADR 0001 intent.
- **Negative / cost:** new directory + routing + rebalancing components; migrations and provisioning run per database; cross-tenant platform reporting moves to an event-stream-fed warehouse (see Analytics sourcing above) — operational DBs are no longer a reporting source.
- **Risk controls:** every phase re-runs the 560-test `TenantIsolationCertification` suite; RLS fail-closed remains the failsafe throughout.

## Verification

- Phase 1: isolation suite green with resolver returning the legacy connection.
- Phase 2: provision 1,000 synthetic tenants across ≥2 pool databases; assert isolation + flat provisioning latency; no cross-database leakage.
- Phase 3: single-tenant restore produces correct row counts without touching other tenants.
- Phase 4: `MigrationDryRunCompleted` recorded (full online-migration cycle on synthetic tenants); cutover write-freeze measured < 60 s (hard max 5 min); other tenants in the source pool observe no freeze; `PlacementAudit` row written for the move.
