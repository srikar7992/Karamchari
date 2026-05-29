# ADR 0007 — PerformanceDbContext Split Roadmap

- **Status:** Accepted
- **Date:** 2026-05-08
- **Deciders:** Solo founder
- **Related:** ADR-0004 (Performance BC architecture)

## Context

`PerformanceDbContext` currently covers all Performance sub-domains: Goals, OKRs, KPIs,
Reviews, Feedback, Calibration, Promotions, Skills, Career, and Read Models. As the
platform grows, this single context may become:

- A migration hotspot (many teams adding tables to one context).
- An ownership ambiguity (who owns the calibration vs. career sub-context?).
- An operational bottleneck (all sub-domains share one connection pool configuration).

## Decision

**Do not split `PerformanceDbContext` until one of the following triggers fires:**

1. EF migration time exceeds 30 seconds on CI.
2. Two developers are regularly blocked waiting for the other's migration to land first.
3. A sub-domain needs different connection pool sizing or a different Azure SQL tier.
4. Deployment velocity of one sub-domain is measurably limited by the shared context.

**Planned split (when triggered):**

| New Context | Sub-Domains |
|---|---|
| `Performance.Core.DbContext` | Goals, OKRs, KPIs, ReviewCycles, ReviewSubmissions |
| `Performance.Reviews.DbContext` | Feedback, Calibration, Promotions |
| `Performance.Career.DbContext` | Skills, CareerFramework, GrowthPlans |
| `Performance.Analytics.DbContext` | PerformanceSnapshots + new CQRS read models |

## Consequences

- Each sub-context would be its own EF project and migration history.
- Cross-sub-domain transactions would require sagas or acceptance of eventual consistency.
- The `AddKaramchariPerformance` DI extension would split into per-sub-context extensions.
- This ADR should be reviewed at the start of year 2 regardless of trigger occurrence.

## Migration Safety

Before splitting:
1. Run a full test suite against a real SQL Server testcontainer.
2. Confirm all FK relationships that cross sub-domain boundaries are advisory only
   (no EF FK navigation properties between sub-contexts — enforced today).
3. Write a migration script that repoints outbox tables to the primary sub-context.
