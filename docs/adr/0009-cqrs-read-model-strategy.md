# ADR 0009 — CQRS Read-Model Strategy

- **Status:** Accepted
- **Date:** 2026-05-08
- **Deciders:** Solo founder

## Context

The Performance BC aggregates are well-designed for writes. They are not designed for
reads. Dashboard queries that naively walk aggregate graphs to render a manager's view
would:
- Load full `GoalCycle` → `Goal[]` → `GoalProgressUpdate[]` for each goal
- Load full `ReviewCycle` → `ReviewAssignment[]` → `ReviewSubmission[]` for each submission
- Load full `CalibrationSession` → `CalibrationEntry[]` for distribution
- Join all of this in memory before producing a single page

This pattern collapses at 50+ employees per manager. The N+1 problem compounds as
teams grow.

## Decision

Segregate writes and reads. Aggregates are the write model. Dedicated denormalized
**projection tables** (read models) serve all dashboard and search queries.

**Read models are:**
- Tenant-scoped, RLS-covered tables (regular tenant tables, not special).
- Updated by MassTransit consumers that react to domain integration events.
- Never exposed to writes from the API — read-only from the API layer.
- Projection-only: they carry denormalized data so queries need no joins.
- Replay-safe: projections can be rebuilt by replaying source events.

**Read model catalog (initial):**

| Table | Serves |
|---|---|
| `ManagerDashboardProjections` | Manager cockpit: team KPI status, overdue goals, pending reviews |
| `ReviewTaskInboxItems` | Reviewer's pending tasks with priority and deadline |
| `CalibrationBoardProjections` | Per-session distribution, panel status, entry counts |
| `PromotionPipelineItems` | Per-promotion with readiness score, stage, approval status |
| `TalentHeatmapEntries` | Per-employee quadrant placement (performance × potential) |
| `TeamGoalSummaries` | Aggregate goal completion rate per cycle per reporting unit |
| `EmployeeSkillInventoryItems` | Skill snapshot per employee for talent discovery queries |

## Projection Update Pattern

```
Domain event published (e.g., ReviewSubmissionSubmitted)
→ MassTransit consumer (ProjectionUpdater<TEvent>)
→ EF Core: update/upsert relevant projection table row(s)
→ All within MassTransit outbox: projection update is atomic with event acknowledgment
```

Projection consumers are idempotent. Each uses the source event's `EventId` as an
idempotency key.

## Staleness Policy

| Projection | Acceptable Lag | Rebuild Trigger |
|---|---|---|
| `ReviewTaskInboxItems` | 10 seconds | `ReviewAssignment` status change |
| `ManagerDashboardProjections` | 60 seconds | Any team member goal/KPI change |
| `CalibrationBoardProjections` | 30 seconds | `CalibrationEntry` adjustment |
| `PromotionPipelineItems` | 30 seconds | Approval stage transition |
| `TalentHeatmapEntries` | After cycle close | `PerformanceSnapshot` materialized |
| `TeamGoalSummaries` | 60 seconds | Goal progress update |
| `EmployeeSkillInventoryItems` | 5 minutes | Skill assessment recorded |

## Query API Design

- BFF endpoints return DTOs derived from projection tables, never from aggregate tables.
- Projection queries use compiled EF Core queries with `.AsNoTracking()`.
- Pagination uses cursor-based loading (keyset pagination by `(TenantId, LastModified, Id)`)
  not offset-based, to prevent the "page 50 is slow" problem.
- Dashboard endpoints return the minimum fields needed for the view — no over-fetching.

## Projection Rebuild

A `/admin/projections/rebuild?type=ManagerDashboard&tenantId=acme` endpoint (admin
only, not exposed via APIM to tenants) triggers a full projection rebuild from source
tables for a given tenant. This is an offline operation, rate-limited to one concurrent
rebuild per tenant.

## Consequences

- Adding a new dashboard feature requires adding a projection table + migration +
  projection updater consumer. This is intentionally more explicit than adding a query.
- Source tables (Goals, Reviews, etc.) should not be queried by the API for reads.
  Any direct source query in an API handler is a code smell and must be reviewed.
- Projection tables must be covered by RLS (registered via `RegisterTenantTable`).
