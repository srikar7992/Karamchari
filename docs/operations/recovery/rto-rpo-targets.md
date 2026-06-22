# RTO / RPO Targets & Recovery Certification

**Status:** Targets proposed (pending business sign-off) — recovery **not yet proven**
**Date:** 2026-06-21
**Driver:** Certification finding **C-3** (`CERTIFICATION.md`) — RTO/RPO undefined; backup/restore never executed.
**Related:** [ADR 0018 — Tenant Storage Strategy](../../architecture/adrs/0018-tenant-storage-strategy.md), [replay-recovery.md](replay-recovery.md)

> **A target is not a capability.** No recovery tier may be marked *Certified* until a timed backup → destroy → restore → validate drill has been executed and recorded in the log at the bottom of this file.

## 1. Definitions

- **RPO (Recovery Point Objective):** maximum acceptable data loss, measured in time. Driven by backup/replication frequency.
- **RTO (Recovery Time Objective):** maximum acceptable time to restore service after an incident.

## 2. Targets by data tier

Tiers map to the recovery tiers in `docs/architecture/database-registry.md`.

| Data domain | Tier | RPO target | RTO target | Backup mechanism (target state) |
|---|---|---:|---:|---|
| Payroll, Billing, FinancialOps, Compensation | Tier 1 (Critical) | **15 min** | **2 hr** | Azure SQL PITR (continuous) + geo failover group + LTR |
| HR, Identity | Tier 1 (Critical) | **15 min** | **2 hr** | PITR + geo failover group |
| PSA, Governance (regulatory) | Tier 1 (Critical) | **15 min** | **2 hr** | PITR + LTR (7–10 yr retention) |
| TimeAttendance, Performance, Recruitment, Capability, Benefits, Workflow | Tier 2 (High) | **1 hr** | **4 hr** | PITR |
| Analytics, Intelligence, Forecasting, Notifications (projections/read models) | Tier 3 (Low) | **24 hr** | **24 hr** | Rebuildable from source events (`IProjectionRebuilder`) — no separate backup required |

Notes:
- Projection/read-model tiers have a relaxed RPO because they are **derived** state: they can be rebuilt by replaying source events via the projection checkpoints, not restored from backup.
- Messaging: outbox guarantees **RPO = 0** for committed-but-unpublished events (durable in DB with the aggregate). Broker/worker restart RPO=0 is already verified (prior chaos cert).

## 3. Topology required to meet these targets (C-3 remediation)

Current `infrastructure/bicep/modules/sql.bicep` cannot meet any of the above (single `Standard` DB, no zone redundancy, no geo replica, untested restore). Required:

1. **Zone-redundant** database tier (Business Critical, or zone-redundant General Purpose) — survives an AZ failure within RTO.
2. **Auto-failover group** with a geo-secondary in a paired region — meets regional RTO; RPO bounded by async replication lag (typically < 5 s, well within 15 min).
3. **PITR** enabled (default 7 days) + **Long-Term Retention** for Tier 1 regulatory data.
4. **Private endpoint + Managed Identity** (also closes H-3); remove the `0.0.0.0` firewall rule.

See the updated `sql.bicep` for the IaC expression of items 1–4.

## 4. Recovery drill procedure (must be executed to certify)

Run against a non-production environment first, then a production-equivalent.

1. **Record baseline:** capture row counts for a representative table per Tier-1 domain (e.g. `Payroll.PayrollRuns`, `HR.Employees`).
2. **Backup:** confirm PITR restore point / take an explicit copy.
3. **Destroy:** delete the database (or simulate regional loss by triggering failover-group failover).
4. **Restore:** PITR-restore to a new database (or confirm the geo-secondary took over).
5. **Validate:** re-run the row-count queries; assert they match baseline (RPO check). Run the `TenantIsolationCertification` smoke subset to confirm RLS/schema intact.
6. **Record:** wall-clock time from step 3 → step 5 success (RTO measurement) and any data delta (RPO measurement).

Per-tenant restore (single tenant) becomes possible only after ADR 0018 Phase 3 (database-per-tenant for that tenant or pool-level restore).

### 4a. Per-tenant restore SLA (ADR 0018 capability)

Per-tenant point-in-time restore is a **product capability**, not just an internal backup detail — it is the strongest enterprise advantage of the hybrid storage model (ADR 0018). For `Regulated`-tier tenants it is a contractual deliverable.

| Tenant placement | Mechanism | Per-tenant RPO target | Per-tenant RTO target | Blast radius |
|---|---|---:|---:|---|
| Dedicated database | Native Azure SQL PITR on that database | **15 min** | **2 hr** | Zero other tenants affected |
| Pooled (shared DB) | Restore pool DB to side copy → extract single schema (BACPAC/`bcp`) → re-import into live pool | **15 min** | **4 hr** | Other schemas in pool untouched |

> Not certified until the Phase 3 single-tenant restore drill (below) is executed and logged. The single-shared-DB status quo **cannot** do per-tenant restore at all (whole-DB restore only) — this is a net-new capability the hybrid model unlocks.

## 5. SLOs / error budget (proposed)

| SLO | Target |
|---|---|
| API availability (monthly) | 99.9% (≈ 43 min/month budget) |
| API p99 latency (read) | < 300 ms |
| API p99 latency (write) | < 800 ms |
| Successful deploy rate | ≥ 95% (rollback counts as failure) |

Error-budget policy: when the monthly availability budget is exhausted, feature deploys pause and reliability work takes priority until the budget recovers.

## 6. Certification status

| Tier | Targets set | Topology in place | Restore drilled | Status |
|---|---|---|---|---|
| Tier 1 | ✅ | ❌ (bicep updated, not deployed) | ❌ | **NOT CERTIFIED** |
| Tier 2 | ✅ | ❌ | ❌ | **NOT CERTIFIED** |
| Tier 3 | ✅ | n/a (rebuildable) | rebuild not drilled | **NOT CERTIFIED** |

## 7. Drill log

_(append one row per executed drill; empty until the first drill runs)_

| Date | Environment | Tier | Scenario | RTO measured | RPO measured | Issues | Result |
|---|---|---|---|---|---|---|---|
| — | — | — | — | — | — | — | — |
