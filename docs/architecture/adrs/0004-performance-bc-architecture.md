# ADR 0004 — Performance BC Architecture

- **Status:** Accepted
- **Date:** 2026-05-08
- **Deciders:** Solo founder

## Context

The Performance Management bounded context covers Goals, OKRs, KPIs, Review Cycles,
Peer Feedback, Calibration, Promotions, Skills, Career Progression, and Analytics
Projections. All of these sub-domains share strong temporal coupling (they operate
within the same performance cycle), overlapping aggregates (an employee's goal score
feeds their review submission), and a common query surface (manager dashboards combine
goals + reviews + KPIs in one view).

Two approaches were considered:
1. Single `Karamchari.Performance` BC with `PerformanceDbContext` covering all sub-domains.
2. Separate BCs per sub-domain from day one (Performance.Core, Performance.Reviews, etc.).

## Decision

Use a **single** `Karamchari.Performance` BC until year 2.

The BC is internally organized into domain folders (`/Domain/Goals`, `/Domain/OKRs`, etc.)
to preserve logical separation, but there is one `PerformanceDbContext` and one
deployment unit.

## Rationale

- Single context enables transactional cross-sub-domain queries without distributed
  transactions (e.g., locking a GoalCycle while opening a ReviewCycle).
- Premature splitting at this stage would require saga-based eventual consistency for
  operations that should be atomic, adding operational complexity for no measurable gain.
- The internal folder structure preserves aggregate ownership and enforces the same
  boundaries as a split would — just without the deployment overhead.

## Consequences

- EF migrations are shared. Adding a new sub-domain means a migration in the same
  `PerformanceDbContext`. This is acceptable until the context grows to ~20+ aggregate
  types or migrations become contended.
- The split roadmap (ADR-0007) should be re-evaluated at the start of year 2, triggered
  by migration velocity pain or deployment-unit isolation needs.
- Cross-sub-domain queries within the BC are fine via CQRS projections (see ADR-0009).

## Explicitly Out of Scope

- Compensation Planning: owns `Karamchari.Compensation` BC (separate EF context).
- HR employee data: owned by `Karamchari.HR`. Performance references employees only
  by `EmployeeId` (Guid). Cross-context queries use integration events or read-model
  projections that are populated by consuming HR integration events.
