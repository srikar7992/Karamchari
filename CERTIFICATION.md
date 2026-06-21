# Karamchari — Reliability, Scalability & Distributed Systems Certification

**Audit type:** Certification-grade architectural audit (panel: Principal Distributed Systems Architect, Staff Platform Engineer, SRE, Database Architect, Security Architect, Enterprise Auditor).
**Date:** 2026-06-21
**Branch / HEAD:** `main` @ `d3256b2`
**Scope:** Whole platform (51 production projects, 1,884 `.cs` files, 19 DbContexts).
**Mandate:** Determine whether Karamchari can be certified as a Workday/SAP SuccessFactors/Oracle HCM/ServiceNow-class distributed system. Trust nothing; verify everything.

> **This file is the continuous-certification ledger.** Re-run the audit after every major sprint. Every new feature must prove it did not weaken consistency, resilience, scalability, or tenant isolation. Update the scores table and the finding register each pass; never delete history — append a new dated section.

---

## VERDICT

**NOT CERTIFIED for Workday-class scale. CONDITIONALLY CERTIFIED for single-region, low-tenant-count (≤ ~500 tenants) enterprise deployment once P0 items close.**

The platform's *domain logic, tenant isolation model, and message-delivery correctness are genuinely strong and largely evidence-backed.* The blockers to a Workday-class verdict are **architectural and operational, not domain**:

1. A **single shared SQL database with schema-per-tenant** is a hard scalability ceiling and an all-tenant single point of failure. (Critical, C-1)
2. **No real CD, no HA database topology, no multi-region** — production deployability and recovery are unproven. (Critical, C-2 / C-3)
3. A **silent in-memory transport fallback** can vaporize all cross-instance messaging in a misconfigured production. (Critical, C-4)

Domain correctness does not compensate for an architecture that cannot be deployed redundantly or scaled horizontally at the data tier.

**Maturity trajectory (as of 2026-06-21, after remediation passes 1–2):** *Provisionally Certified Architecture — Pending Operational Validation.* C-4 and H-1 are permanently closed. The remaining critical blockers have shifted from **architectural uncertainty** to **evidence problems**: prove deployment works (C-2), prove recovery works (C-3), prove hardening works (H-3) — all gated on a first Azure access window. C-1 is an accepted-direction program pending ADR-0018 review.

---

## Scorecard

| Dimension | Score /100 | One-line basis |
|---|---:|---|
| Reliability | 58 | Strong outbox/inbox correctness; undermined by single-DB SPOF and unproven recovery. |
| Scalability | 32 | Schema-per-tenant + single DB + global outbox poll table cap out well below Workday scale. |
| Resilience | 45 | Circuit breakers, durable queues, RLS fail-closed exist; no HA topology, no region failover, DR undrilled. |
| Operability | 40 | Rich telemetry wired; alerting-to-human, runbook drills, dashboards unverified; CD mocked. |
| Security | 74 | 4-boundary tenant isolation, RLS fail-closed, JWT hardening, 6 IDOR defects fixed; SQL-auth + open Azure firewall remain. |
| Multi-Tenancy | 80 | Isolation proven at auth/app/RLS/messaging incl. async path; ceiling is scale, not correctness. |
| Distributed Systems Maturity | 50 | Excellent at-least-once messaging; distributed-monolith coupling via shared DB; no sharding/replicas. |
| Enterprise Readiness | 48 | Audit/lineage/RBAC present; DR + CD + perf certification incomplete. |
| Workday-Class Readiness | 22 | Cannot horizontally scale the data tier; single region; single DB blast radius = 100% of tenants. |

Scoring rule: a dimension cannot exceed 60 while an open **Critical** finding sits in its causal path.

---

## PHASE 1 — System Model

### What it actually is
A **.NET modular monolith**, not a microservice mesh. Two deployable hosts:

- **`Karamchari.Api`** — HTTP host, loads all modules in-process via `ICapabilityModule` registration.
- **`Karamchari.Worker`** — background host (outbox relay, projections, consumers, scheduled jobs).

Modules (bounded contexts, in-process): HR, Payroll, TimeAttendance, PSA, Performance, Compensation, Recruitment, Benefits, Capability, Intelligence, Governance, Compliance, Billing, FinancialOps, Forecasting, Workflow, Notifications, Onboarding, Engagement, Helpdesk, AssetManagement, Succession, Extensibility, Analytics, DataMigration, Integration, PlatformIntelligence. Platform: Core, Identity, Approvals.

Module-to-module coupling is **contract-only** (`*.Contracts` projects) and enforced by 60 NetArchTest architecture tests — verified claim. **But** all modules share one physical database, so the contract boundary is logical, not physical: this is a **distributed monolith at the data tier** when scaled out.

### Stores / brokers / caches
| Component | Tech | Topology (as-coded) | Evidence |
|---|---|---|---|
| Primary DB | Azure SQL / SQL Server | **Single database**, shared-DB schema-per-tenant (`tenant_<id>`, ~177 tables/tenant) | `KaramchariDbContext.cs`, `database-registry.md`, `TenantProvisioningService.cs` |
| Identity DB | same SQL instance, `identity` schema | shared | `database-registry.md` |
| Cache | Redis | single instance (fail-soft) | `docker-compose.yml`, `redis.bicep` |
| Broker | MassTransit over RabbitMQ **or** Azure Service Bus **or** InMemory | env-selected at startup | `MassTransitExtensions.cs:145-207` |
| Outbox | EF Core outbox + custom `OutboxRelayService` | single global `dbo.OutboxState/OutboxMessage` | `OutboxRelayService.cs` |
| Identity provider | self-hosted ASP.NET Identity + JWT (signing-key rotation, JTI blacklist, refresh-token families) | in-process | `database-registry.md`, prior auth cert |

### Trust boundaries
Internet → API (JWT auth, tenant resolution middleware) → in-process modules → SQL (RLS + SESSION_CONTEXT fail-closed) → broker (tenant filters on consume/publish/send). Four tenant-isolation boundaries, all with evidence.

### Diagram (logical / data flow)
```
                      ┌──────────────────────────────────────┐
   client ──JWT──▶    │ Karamchari.Api (all modules in-proc)  │
                      │  AuthN ▶ TenantResolution ▶ Module     │
                      └───────────────┬───────────────┬───────┘
                                      │ EF (RLS+SESSION_CONTEXT)│ Publish→ EF Outbox row
                                      ▼                         ▼
                      ┌───────────────────────────┐   ┌──────────────────────────┐
                      │  SINGLE Azure SQL DB       │   │ dbo.OutboxState/Message  │
                      │  schema-per-tenant         │   └────────────┬─────────────┘
                      │  tenant_dev tenant_acme... │                │ poll (UPDLOCK/READPAST)
                      │  identity / dbo            │                ▼
                      └───────────────────────────┘   ┌──────────────────────────┐
                          ▲ single point of failure    │ Karamchari.Worker        │
                          │ for ALL tenants            │ OutboxRelay ▶ Broker ▶   │
                          └────────────────────────────┤ Consumers/Projections    │
                                                        └──────────────────────────┘
                                            Broker: RabbitMQ | ASB | (InMemory fallback ⚠)
```

---

## PHASE 2 — Availability, Partition Tolerance, Consistency, CAP

### Availability — failure-mode walk
| Failure | Behavior (as-coded) | Verdict |
|---|---|---|
| One API node fails | Stateless API; LB routes elsewhere. **No LB/replica defined in bicep.** | Safe *if* multi-instance deployed; **not deployed that way** |
| Multiple API nodes fail | Same; depends on instance count (unset) | Unproven |
| Worker node fails | Outbox relay multi-instance safe (UPDLOCK/READPAST); stale-lock recovery on restart | **Safe** (verified in code) |
| K8s/VM/AZ fails | No zone-redundancy in `sql.bicep`/`appservice.bicep` | **Not safe** |
| **AZ fails** | SQL `Standard` tier (cap 10) — not zone-redundant | **Total DB outage** |
| **Region fails** | No failover group, no geo-replica | **Total platform outage; RPO = geo-backup interval** |
| LB fails | None defined | Unknown |
| DNS fails | Standard external dependency; no mitigation coded | Unknown |

**Single points of failure:** the one SQL database (100% of tenants), the single broker, single Redis. **Blast radius of the DB = the entire customer base.**

### Partition tolerance
- DB reachable, broker down → writes + outbox rows persist; relay circuit-breaker opens; events drain on broker return. **Safe, RPO=0** (outbox design).
- Broker reachable, DB down → API 5xx; nothing to publish. **Fail-stop, safe.**
- Split-brain → not applicable: single DB is the arbiter; no quorum system to split. (Also why there is no cross-region partition tolerance — there is only one region.)
- Cross-region replication → **does not exist.**

### Consistency classification
| Operation class | Level | Justified? |
|---|---|---|
| Same-module writes (payroll run, leave deduction, offer accept) | Strong (single-DB tx) | Yes — financial/statutory correctness needs it |
| Cross-module reactions (CandidateHired → Employee) | Eventual (outbox→broker→inbox) | Yes — async is correct here; idempotent consumers verified |
| Read-after-write within a request | Strong | Yes |
| Analytics/projections read models | Eventual (projection lag) | Yes — acceptable |
| Tenant provisioning | Strong but **serial, long DDL** | Acceptable correctness, poor scale |

No *insufficient* consistency found. One *overly coupled* area: because everything is one DB, "eventual" cross-module flows still share a failure domain — eventual consistency without fault isolation.

### CAP per module
All modules inherit the same substrate, so per-module CAP is **uniform, not independent**:

| Module | Effective | Note |
|---|---|---|
| Payroll, Billing, FinancialOps, Compensation, HR | **CP** | Strong consistency on single DB; unavailable on DB loss |
| Recruitment, TimeAttendance, Performance, Benefits, Capability, Workflow | **CP** | Same substrate |
| Analytics, Intelligence, Forecasting, Notifications | **CP for writes, eventual reads** | Projection read models lag |
| Cross-module integration (messaging) | **AP-leaning** | At-least-once, idempotent, survives broker partition |

**Inconsistency / finding:** the system is structured as modular (implying per-context tunable CAP) but is physically **CP-uniform with a shared failure domain**. True bounded-context independence is not achievable without splitting the data tier. (See C-1.)

---

## PHASE 3 — Idempotency

| Surface | Mechanism | Evidence | Gap |
|---|---|---|---|
| Mutating API endpoints | `IdempotencyFilter` + `dbo.IdempotentRequests` (7-day TTL, cleanup worker) | `IdempotencyFilter.cs`, `IdempotentRequest.cs` | Coverage per-endpoint not exhaustively asserted (verify all money/state endpoints opt in) |
| Integration-event consumers | MassTransit `InboxState` (MessageId per consumer) + domain guards (ProcessedEventLog, ExternalRevisionId, NotificationDedup, LearningEnrollmentKey) | prior INBOX cert; `MarkDeliveredAtomicAsync` | Live B1/B2/B3 replay still listed unexecuted in prior cert |
| Outbox redelivery | At-least-once + idempotent consumer requirement; atomic DELETE+UPDATE+DELETE | `OutboxRelayService.cs:627` | Correct by design |
| Domain double-action | Offer-accept-twice / hire-twice rejected; 50-concurrent-apply → 1 row (unique index) | prior recruitment cert | Strong |
| Scheduled jobs | `JobGovernanceService` / `JobStatusProjection` | present | Not audited for re-entrancy this pass |

**Can duplicate execution cause duplicate payments / deductions / enrollments / audit?** Design says no, and tests support it. **Not yet closed:** end-to-end live replay proof (B1–B3) and an explicit per-endpoint idempotency coverage assertion. → P1-1.

---

## PHASE 4 — Event-Driven Architecture

| Question | Finding |
|---|---|
| Events immutable? | Yes — stored verbatim as MassTransit envelopes in `OutboxMessage.Body` |
| Replayable? | Partially — DLQ rows retain full body/headers; no general event-replay/rebuild-from-log facility beyond projection checkpoints |
| Rebuild state from history? | Projections have checkpoints + rebuild interface (`IProjectionRebuilder`, `ProjectionCheckpoint`); a `BiTemporalEventStore` exists but is not the system-of-record for all modules |
| Schema versioning / consumer evolution | **No explicit event-version field or schema-compatibility policy found.** Type resolved by URN via `OutboxMessageTypeRegistry`; renaming/moving a contract type breaks resolution → poison dead-letter |
| Old consumer reads new event / vice-versa | No compatibility contract enforced; additive JSON is tolerant, but breaking changes are unguarded |

**Findings:** H-1 (no event-schema versioning/compatibility gate). The URN type registry makes contract-type moves a runtime poison-message hazard.

---

## PHASE 5 — Data Integrity

| Simulation | Behavior | Verdict |
|---|---|---|
| Mid-transaction crash | Single-DB tx → rollback; outbox row only commits with the aggregate (EF outbox) | Safe |
| Consumer crash mid-process | InboxState/domain guard → no duplicate side effect on redelivery | Safe (design + tests) |
| DB failover | No failover topology; connection drops → request fails; on reconnect, committed state intact | Safe for RPO, **no HA for RTO** |
| Message loss | Outbox is source of truth; at-least-once → no loss *unless* InMemory transport (C-4) | Conditional |
| Out-of-order delivery | Per-OutboxId ordering preserved (`ORDER BY SequenceNumber`); cross-aggregate ordering not guaranteed | Acceptable; document it |
| Distributed transaction / saga | **No saga/compensation framework.** Cross-module workflows rely on eventual consistency + idempotency, not orchestrated compensation | H-2 |

Referential integrity is intra-schema (per tenant). **No cross-module FK** (correct for modularity) — integrity across modules depends on event handlers, which is acceptable but unmonitored for orphan/divergence. → P2.

---

## PHASE 6 — Database Scalability (the central finding)

Schema-per-tenant in **one** database. Per `TenantProvisioningService`, each tenant gets ~177 tables, all indexes cloned, plus RLS predicates.

| Tenants | Tables (≈177/tenant) | RLS predicates (≈) | Outcome |
|---:|---:|---:|---|
| 100 | 17,700 | 168k | Workable |
| 1,000 | 177,000 | 1.68M | Metadata pressure: slow `sys.*`, plan-cache bloat, long provisioning DDL holding schema locks, multi-hour backup/restore |
| 10,000 | 1.77M | 16.8M | Beyond practical Azure SQL single-DB limits; model load, DBCC, and metadata contention dominate |
| 100,000 | 17.7M | 168M | **Infeasible** on any single SQL database |

Additional ceilings:
- **Write scaling:** single DB → single primary; no sharding (`shardkey`/`partitionkey` absent in app code — grep hits were SQL window functions).
- **Read scaling:** no read-replica routing (`ApplicationIntent=ReadOnly` absent in connection strings).
- **Provisioning:** serial DDL (`SELECT * INTO` + per-index `CREATE INDEX` + RLS) per tenant — onboarding latency grows with catalog size and contends with live metadata.
- **Backup/restore is whole-DB:** cannot restore a single tenant; PITR restores all tenants together → noisy-neighbor recovery coupling.
- **`dbo` template tables hold real schema** and have already accreted orphan rows (`dbo.PayrollProfiles`, 7 rows) — template tables are a latent leak/restore hazard.

**This is C-1: the dominant Workday-class blocker.** Remediation = move to **database-per-tenant (pooled, e.g. Azure SQL Elastic Pools) or tenant-sharded catalog** with a tenant→shard map. Schema-per-tenant tops out at low-thousands of tenants on a heavily-provisioned single DB; Workday-class is six-to-seven-figure tenant/employee counts.

---

## PHASE 7 — Multi-Tenancy (strongest area)

| Check | Result | Evidence |
|---|---|---|
| Every query tenant-filtered | Yes — global `HasQueryFilter` on all `ITenantOwned` + `__tenant__` schema rewrite + RLS | `KaramchariDbContext.cs:274`, interceptors |
| RLS fail-closed | Yes — `sa` SELECT without SESSION_CONTEXT → 0 rows; ~1,680 predicates live | prior DB/security cert |
| Every cache tenant-scoped | Asserted by prior cert; **re-verify each sprint** | — |
| Every event tenant-scoped | Yes — `TenantConsume/Publish/Send` filters on **all three** transports (RabbitMQ leak fixed) | `MassTransitExtensions.cs:170-191` |
| Async path isolation | D1 defect fixed + re-verified; concurrent 3-tenant onboard isolated | prior async cert |
| Leakage attempt | Cross-tenant IDOR → 404; 560-test `TenantIsolationCertification` suite passes | prior certs, dedicated test project |

**No open cross-tenant leakage path found.** Residual: SESSION_CONTEXT/RLS is a *runtime* control — a missing `SET SESSION_CONTEXT` on a raw connection path would bypass query filters, but RLS fail-closed catches it (defense in depth). Keep `TenantSessionContextValidator` enforced. Multi-tenancy correctness is **certified**; the only multi-tenant risk is **scale** (C-1), not isolation.

---

## PHASE 8 — Resilience (fault injection, as-coded)

| Injected failure | Continues serving? | Degrades | Stops |
|---|---|---|---|
| Broker outage | Yes (writes persist, events queue in outbox) | async reactions delayed | — |
| Cache outage | Yes (Redis fail-soft) | latency ↑ | — |
| **DB outage** | **No** | — | **everything** |
| Single API instance | Yes (if replicated) | capacity ↓ | — |
| DNS / cert expiry | Coded mitigations absent | — | likely outage |
| Storage exhaustion (outbox/DLQ growth) | Partial — DLQ unbounded growth not capped | — | DB fills |
| Clock drift | Backoff/stale-lock use SYSUTCDATETIME (DB clock, single source) — robust to app-node drift | — | — |

Mechanisms present and good: circuit breaker, exponential backoff (capped 300s), stale-lock recovery, durable outbox, RLS fail-closed, message retry. **Missing:** HA for the DB (the only thing whose failure is total), bulkheads between modules (shared thread/DB pool), DLQ size cap + alert.

---

## PHASE 9 — Observability

| Capability | State | Evidence |
|---|---|---|
| Metrics | Real — OTel (AspNetCore, EF, MassTransit) + 67 series incl. `karamchari_outbox_*`, projection lag, tenant metrics | `Observability/*`, prior cert |
| Structured logs | Yes — Seq; AUTH_SUCCESS/FAILURE captured live | prior cert |
| Traces / correlation | Wired; **end-to-end trace correlation across outbox→broker→consumer NOT verified** | prior cert (NOT CERTIFIED) |
| Tenant correlation | `TenantObservabilityMiddleware` present; tenant-scoped series not verified firing | code + prior cert gap |
| Alerts → human | **Not verified** — no Alertmanager→channel proof | prior cert blocker B4/B8 |

**Can root cause be found in 15 min? Can a failed payroll run be reconstructed? Can a distributed workflow be traced end-to-end?** Instrumentation makes this *plausible* but it is **not proven** — forced-failure observability and trace-stitch tests are unexecuted. → P1-2.

---

## PHASE 10 — Security & Compliance

| Control | State |
|---|---|
| AuthN | JWT: validation, claims, JTI blacklist (2-tier cache), refresh-token rotation + family revocation, signing-key rotation — live-verified |
| AuthZ | 29-permission × 5-role matrix; endpoint guards; cross-role escalation denied live; 6 IDOR defects found+fixed |
| ABAC | `EmployeeScopeGuard` (ESS self-scope) — partial attribute-based |
| Audit logging | AuditInterceptor + PersistentSecurityAuditService (OWASP A09); 7-yr governance retention |
| Encryption at rest | DB-level (Azure SQL TDE implied); **not asserted in bicep** |
| Encryption in transit | `Encrypt=False` in dev conn string; **HSTS/TLS unverified (prod-only)** |
| Secrets | Key Vault module exists; **but SQL admin login+password used, not AAD-only/Managed Identity** |
| Network | **`sql.bicep` firewall opens 0.0.0.0 (all Azure)**; no private endpoint |
| Tenant isolation | 4 boundaries, fail-closed (Phase 7) |
| SOC2 / ISO27001 | Audit trail + access reviews + governance retention present; **no evidence of completed controls mapping/attestation** |

Privilege-escalation and cross-tenant attempts: defended (prior live attacks). **Open security findings:** H-3 (SQL auth + open Azure firewall + no private endpoint), M-5 (encryption-at-rest/TLS not asserted in IaC). No audit-tampering path found (append-only audit + interceptor).

---

## PHASE 11 — SRE

| Item | State |
|---|---|
| RTO / RPO | **Not certified.** Broker/worker restart RPO=0 verified; **DB/region RTO unmeasured**; backup→restore→verify never executed |
| SLA / SLO / error budgets | **Not defined** anywhere in repo |
| Backup strategy | Daily full + hourly diff *claimed* in registry; **never executed/verified** |
| Restore strategy | **Never tested** — no restore drill |
| DR | Mechanisms exist; no region failover; CD mocked → no real failover drill possible |
| Runbooks | Documented (`docs/operations/runbooks`); **not timed-drilled** |
| Incident response | Docs present (`docs/incidents`); maturity unverified |

→ P0 (define SLOs + execute backup/restore drill), P1 (runbook drills).

---

## PHASE 12 — Workday-Class Scale Model

| Scale | First bottleneck encountered | Limit |
|---|---|---|
| 100k employees (few tenants) | DB write throughput on `Standard` cap-10 SKU; payroll batch on single primary | Resolvable by SKU upsize |
| 1M employees | Single-primary write ceiling; reporting/analytics on same DB as OLTP (no read replica) | Needs read replicas + OLAP separation |
| 10M employees / many tenants | **Schema-per-tenant table explosion (Phase 6) + single-DB SPOF** | **Hard architectural wall** |

**First true scaling wall:** the data tier (C-1), reachable well before Workday volumes. Compute (stateless API/Worker) scales horizontally fine; the **DB does not.**

---

## PHASE 13 — Architectural Debt

| Debt | Evidence | Severity |
|---|---|---|
| Distributed monolith at data tier | one DB shared by all modules; logical-only contract boundary | High |
| Shared-database pattern | Phase 6 | Critical (C-1) |
| Hidden synchronization / shared pools | one connection pool + thread pool across all modules → no bulkhead | Medium |
| No event schema versioning | Phase 4 | High (H-1) |
| No saga/compensation | Phase 5 | High (H-2) |
| Global outbox poll table | `dbo.OutboxState ORDER BY Created` — cross-tenant contention point | Medium (M-1) |
| `dbo` template tables hold data | orphan `dbo.PayrollProfiles` | Medium (M-2) |
| CD mocked | `deploy-api.yml` | Critical (C-2) |
| No HA DB topology | `sql.bicep` | Critical (C-3) |
| Silent InMemory transport fallback | `MassTransitExtensions.cs:195-205` | Critical (C-4) |

No cyclic module dependencies (architecture tests enforce). Coupling is via the shared substrate, not direct references.

---

## PHASE 14 — Production Readiness Verdict

See Scorecard. **Workday-Class Readiness 22/100.** Single-region single-DB schema-per-tenant cannot be certified for the stated scale. The platform is a well-built modular monolith suitable for **single-region, bounded-tenant enterprise SaaS** once C-2/C-3/C-4 close.

Certification gates and their status:
- No critical data-corruption paths → **PASS** (idempotency + atomic outbox; pending live replay proof)
- No cross-tenant leakage paths → **PASS** (4-boundary, fail-closed, 560 tests)
- No unrecoverable failure modes → **FAIL** (DB/region loss unrecoverable as-deployed; restore untested)
- No unbounded scalability bottlenecks → **FAIL** (C-1)
- No unhandled distributed-systems failure scenarios → **FAIL** (region; silent InMemory)
- Documented degraded-mode behavior → **PARTIAL**
- Verified recovery procedures → **FAIL** (undrilled)
- Proven horizontal scalability → **FAIL at data tier**
- Proven operational resilience → **FAIL** (CD mocked)

---

## PHASE 15 — Remediation Plan

### P0 — Critical (block production at scale)

**C-1 — Eliminate single-DB / schema-per-tenant scaling ceiling.**
- *Steps:* Introduce a **tenant→shard catalog** (tenant id → connection string). Adopt **database-per-tenant in Azure SQL Elastic Pools** (pool many small tenants; isolate large ones). Replace `__tenant__` schema rewrite with per-tenant connection resolution; keep RLS as defense-in-depth. Migrate provisioning from `CREATE SCHEMA`+clone to `CREATE DATABASE`-from-template/dacpac.
- *Code:* `TenantProvisioningService.cs`, `KaramchariDbContext.cs` (drop schema interceptor for connection routing), tenant connection resolver in `Multitenancy/`.
- *DB:* shard-map table (in a directory DB), per-tenant DB template (dacpac), retire `dbo` template tables.
- *Infra:* `sql.bicep` → Elastic Pool resource.
- *Test:* provision 1,000 tenants; assert isolation + flat provisioning latency; restore a single tenant DB.
- *Validation:* single-tenant PITR works; metadata queries O(1) per tenant; onboarding latency independent of tenant count.
- *Effort:* **XL (6–10 weeks).**

**C-2 — Implement real CD.**
- *Steps:* Un-comment/implement Azure login, ACR push, bicep deploy, App Service deploy in `deploy-api.yml`; add blue-green or slot swap + automated rollback.
- *Validation:* one real deploy + one real rollback with evidence.
- *Effort:* **M (1 week).**

**C-3 — HA / DR database topology.**
- *Steps:* `sql.bicep` → zone-redundant (Business Critical/Premium or zone-redundant GP), add **failover group** for geo-replica; enable PITR + LTR; private endpoint; remove 0.0.0.0 firewall.
- *Validation:* execute **backup → restore → row-count verify**; perform a **forced failover** drill; record RTO/RPO.
- *Effort:* **L (2–3 weeks).**

**C-4 — Remove silent InMemory transport fallback.**
- *Steps:* In `MassTransitExtensions.cs` and `MessagingServiceCollectionExtensions.cs`, **fail fast** if the environment is non-dev and neither RabbitMQ nor ASB connection string is present (throw at startup). Never select `UsingInMemory` outside `isDev`.
- *Code:* `src/Backend/Hosts/Karamchari.Api/DependencyInjection/MassTransitExtensions.cs:195-205`.
- *Test:* startup integration test asserting prod config without broker throws.
- *Effort:* **S (hours).**

**P0-SRE — Define SLOs + execute first backup/restore drill** (depends on C-3). Define availability/latency SLOs + error budgets in repo; run one timed restore. *Effort: S–M.*

### P1 — High

- **P1-1 — Close live idempotency replay (B1/B2/B3) + assert per-endpoint idempotency coverage.** Run `certify-inbox-replay.sh`; add a test that fails if a state-mutating endpoint lacks the idempotency filter. *Effort: M.*
- **P1-2 — Observability forced-failure + end-to-end trace stitch.** Run `certify-observability-forced-failures.sh`; verify trace correlation outbox→broker→consumer; wire Alertmanager→channel; fire test alert. *Effort: M.*
- **H-1 — Event schema versioning.** Add explicit `version` to integration-event envelopes + a compatibility policy (additive-only, upcasters for breaking changes); CI contract-compat check against committed schema snapshots. *Effort: M.*
- **H-2 — Saga/compensation for multi-step cross-module flows** (e.g. hire→employee→payroll-profile→benefits-enroll). Adopt MassTransit state-machine saga or explicit compensation handlers for flows spanning ≥3 modules with money/headcount effects. *Effort: L.*
- **H-3 — Harden DB access.** AAD-only / Managed Identity (drop SQL admin password), private endpoint, remove open Azure firewall, assert TDE + TLS-required in bicep. *Effort: M.*
- **P1-perf — Certify performance on representative x64 hardware** (P50/P95/P99, worker throughput, DB-under-load). Run NBomber/k6 suite. *Effort: M.*

### P2 — Strategic

- Per-module bulkheads (dedicated connection pools / concurrency limits) to bound blast radius within the monolith.
- Cap + alert DLQ and outbox backlog size; retention/archival for outbox/audit growth.
- Retire `dbo` template tables after C-1; purge orphan `dbo.PayrollProfiles`.
- OLTP/OLAP separation: route analytics/reporting to read replicas.
- Formal SOC2/ISO27001 controls mapping + evidence-collection automation.
- Outbox poll partitioning (per-tenant or hash bucket) to remove the global `ORDER BY Created` contention point.

---

## What is solid (do not re-litigate)
- Multi-tenant isolation at all four boundaries incl. async path — proven (560-test suite, RLS fail-closed, transport filters).
- Outbox at-least-once correctness: multi-instance safe (UPDLOCK/READPAST), circuit breaker, backoff, stale-lock recovery, atomic completion, DLQ.
- Recruitment domain (12-step journey, 5 aggregates, state-machine guards, idempotent terminal actions).
- AuthN/AuthZ hardening (JWT, blacklist, rotation, 29×5 RBAC, 6 IDOR fixed).
- Clean modular architecture (60 NetArchTest tests, contract-only coupling, no cycles).

## Open finding register
| ID | Sev | Title | Phase | Owner | Status |
|---|---|---|---|---|---|
| C-1 | Critical | Single-DB schema-per-tenant scaling ceiling & SPOF | 6,12 | Platform/DB | **IN REVIEW** — [ADR 0018](docs/architecture/adrs/0018-tenant-storage-strategy.md) (Option C hybrid) proposed; next milestone = architecture-review approval → becomes accepted technical program |
| C-2 | Critical | CD pipeline mocked | 11,13 | DevOps | **IMPLEMENTED — AWAITING PROOF** — real `deploy-api.yml` written; CI already complete. Closes only after one successful real Azure deploy + rollback with evidence |
| C-3 | Critical | No HA/DR DB topology; restore untested | 8,11 | DBA/SRE | **IMPLEMENTED — AWAITING PROOF** — [RTO/RPO targets](docs/operations/recovery/rto-rpo-targets.md) + DR bicep done. A DR plan without a restore exercise is a hypothesis: closes only after backup→restore→validate→timing evidence |
| C-4 | Critical | Silent InMemory transport fallback in non-dev | 4,8 | Platform | **RESOLVED 2026-06-21** — `TransportConfigurationGuard` fails fast in non-dev w/o broker; 6 tests green |
| H-1 | High | No event schema versioning/compatibility gate | 4 | Platform | **RESOLVED 2026-06-21** — 80 events → `…V1` + `IIntegrationEvent`; marker relocated to `Core.Contracts`; `EventVersioningTests` arch gate (3 tests) enforces version suffix + marker + immutability in CI |
| H-2 | High | No saga/compensation for cross-module flows | 5 | Platform | **OPEN (largest engineering finding)** — do workflow discovery first (`docs/architecture/SagaCandidateAssessment.md`); only flows that span modules AND have damaging partial completion AND need compensation become sagas |
| H-4 | Med | Duplicate HTTP endpoint name `Succession.CriticalRoles.Register` → API host 500s on full-host requests | 8 | Succession | **BACKLOG (architecture debt)** — pre-existing; reclassified out of reliability-certification scope unless it causes inconsistent behaviour / security / data issues. Chip flagged |
| H-3 | High | SQL-auth + open Azure firewall + no private endpoint | 10 | Security | **IMPLEMENTED — AWAITING INFRA VALIDATION** — `sql.bicep` AAD-only, private endpoint, public access disabled, 0.0.0.0 removed. Code complete; closes after deploy validates managed identity + private endpoints |
| M-1 | Med | Global outbox poll table contention | 6,13 | Platform | OPEN |
| M-2 | Med | `dbo` template tables hold data / orphan rows | 6 | DBA | OPEN |
| M-3 | Med | No bulkheads (shared DB/thread pool) | 8,13 | Platform | OPEN |
| M-4 | Med | DLQ/outbox unbounded growth | 8 | Platform | OPEN |
| M-5 | Med | Encryption at-rest/TLS not asserted in IaC | 10 | Security | OPEN |
| O-1 | Open | Live idempotency replay B1/B2/B3 unexecuted | 3 | QA | OPEN |
| O-2 | Open | Observability forced-failure + trace stitch unexecuted | 9 | SRE | OPEN |
| O-3 | Open | SLO/error-budget undefined; perf uncertified on x64 | 11,12 | SRE | OPEN |

---

*Continuous-certification protocol: each sprint, append a dated section below with (1) re-scored scorecard, (2) new/closed findings, (3) any regression in the four invariants (consistency, resilience, scalability, tenant isolation). A feature merges only if it does not lower any score or open a Critical finding without a tracked remediation.*

## Audit history
- **2026-06-21** — Initial certification audit (this document). Verdict: NOT CERTIFIED for Workday-class; conditionally certifiable for single-region bounded-tenant once C-2/C-3/C-4 close. 4 Critical, 3 High open.
- **2026-06-21 (remediation pass 1)** — **C-4 RESOLVED.** Added `TransportConfigurationGuard.EnsureConfigured` (`Karamchari.Core/Messaging/`); wired into API (`MassTransitExtensions.cs`) and Worker path (`MessagingServiceCollectionExtensions.cs`). Non-Development host with no `RabbitMQ`/`AzureServiceBus` connection string now throws at startup instead of silently selecting the in-memory transport. 6 unit tests cover the matrix (prod+no-broker→throw, prod+RabbitMQ→ok, prod+ASB→ok, dev→allowed). Core + API build clean (0 warn/0 err). **Remaining Critical: C-1, C-2, C-3.**
- **2026-06-21 (remediation pass 3)** — **H-1 RESOLVED.** Event-versioning migration executed: relocated `IIntegrationEvent` marker to the zero-dependency `Karamchari.Core.Contracts` (Core now references Core.Contracts; 5 Contracts projects got the ref). Renamed **80** integration events to `…IntegrationEventV1` and made each implement `IIntegrationEvent` (URN preservation unnecessary — `OutboxMessageTypeRegistry` is dynamic and the system has no in-flight/production messages; future breaking changes ship `…V2`). 115 reference files updated. Added `EventVersioningTests` (3 tests, all green) to `Karamchari.ArchitectureTests` enforcing: versioned suffix, `IIntegrationEvent` marker, immutable record — runs in the existing CI ArchitectureTests gate. Full solution builds 0 errors; HR 144/144, Recruitment 127/131. **Pre-existing defect surfaced (not caused by this work): H-4** — duplicate route `Succession.CriticalRoles.Register` makes the API host 500 on full-host requests (the 4 Recruitment API-cert failures). **Remaining Critical: C-1, C-2, C-3. High: H-2, H-3, H-4.**
- **2026-06-21 (remediation pass 2)** — artifacts landed for the remaining P0/P1 (proof still pending external infra):
  - **C-1:** [ADR 0018 — Tenant Storage Strategy](docs/architecture/adrs/0018-tenant-storage-strategy.md) proposed. Recommends **Option C (hybrid: pooled Elastic Pools for small tenants + dedicated DB for large)**, extends ADR-0001, phased no-big-bang migration. Awaiting architecture review before any code.
  - **C-2:** `deploy-api.yml` rewritten from echo-mocks to a real pipeline (OIDC login, ACR push, bicep deploy, staging-slot deploy, health verification, slot-swap go-live, auto-rollback), gated on `vars.AZURE_DEPLOY_ENABLED`. Note: **CI was already complete** (locked restore, format, `-warnaserror`, coverage floor, NuGet audit, trufflehog, arch tests, tenant-isolation gate) — gap was CD only. Live deploy proof requires the user's Azure subscription.
  - **C-3 / H-3:** [RTO/RPO targets + recovery-cert doc](docs/operations/recovery/rto-rpo-targets.md) written (Tier-1 15min/2hr … Tier-3 24hr, SLOs, drill procedure). `infrastructure/bicep/modules/sql.bicep` hardened: zone-redundant GP, PITR + LTR, geo failover group (opt), TDE, AAD-only auth, public access disabled, private endpoint (opt), **0.0.0.0 firewall removed**; `main.bicep` wires the deploy principal as AAD admin. Bicep compile + restore drill pending Azure tooling/subscription.
  - **H-1:** [event-versioning standard](docs/development/standards/event-versioning-standard.md) written (envelope already exists; codifies immutable contracts, `V1`/`V2` typing, upcasters, deprecation lifecycle, proposed CI snapshot gate + arch test).
  - **Still fully open:** H-2 (saga/compensation). **Critical status:** C-4 resolved; C-1/C-2/C-3 have remediation artifacts but are not closed until reviewed/deployed/drilled.
