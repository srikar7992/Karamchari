# ADR-0011: BFF Architecture — Persona Route Groups within Modular Monolith

**Status:** Accepted
**Date:** 2026-05-08

## Context

The domain layer is complete. Phase 3 must produce usable, performant API contracts
without collapsing into:
- Giant flat REST responses with every domain field
- N+1 dashboard queries hitting aggregate tables
- Frontend-side orchestration of 5+ parallel calls per page
- Tightly coupled UI contracts that break on every domain refactor

The architecture must serve four distinct personas:
| Persona | Primary workflow |
|---------|-----------------|
| Manager | Team oversight, reviews, promotions, calibration |
| Employee | Self-service: goals, reviews, growth, notifications |
| HR | Cycle governance, operational dashboards, approvals |
| Executive | Org-level talent health, succession, retention risk |

## Decision

**BFF stays within `Karamchari.Api` as organized route groups — no separate projects.**

The modular-monolith rule (CLAUDE.md §2) forbids additional deployable units.
Separate BFF surfaces are implemented as `MapGroup` extensions on `WebApplication`,
each in its own folder under `Karamchari.Api/BFF/{Persona}/`.

### Route prefixes

| Group | Prefix |
|-------|--------|
| Manager | `/api/v1/manager/` |
| Employee | `/api/v1/me/` |
| HR | `/api/v1/hr/` |
| Executive | `/api/v1/executive/` |
| Notifications | `/api/v1/notifications/` |

### Invariants

1. **Projection-first reads.** All list and dashboard endpoints read from CQRS projection
   tables (ADR-0009). Aggregate tables are never queried by BFF handlers.
   Exception: employee self-service reads small per-employee sets directly (Goals, Skills)
   with `.AsNoTracking()` + `.Select()` projection to DTOs.

2. **Graph-aware visibility.** Manager endpoints resolve visible employee IDs via
   `IVisibilityResolver` (Karamchari.HR.Services) before filtering projections.
   `Employee.ManagerId` is never used directly for access control.

3. **No aggregate references in DTOs.** BFF DTOs are flat, UI-shaped records.
   No domain enums, no navigation properties, no row versions.

4. **Pagination.** List endpoints use offset (`page`/`pageSize`, default 20).
   Phase 3b will migrate to keyset cursor pagination (cursor = base64 of last row key).

5. **Staleness indicator.** Every dashboard response includes `DataAsOf` (UTC) and
   `IsStale` (true if `DataAsOf < UtcNow - 15min`) to drive eventual-consistency UX.

### Versioning

- URL path prefix: `/api/v1/` → `/api/v2/` on breaking contract change.
- Non-breaking additions (new fields in existing DTOs) require no version bump.
- Projection schema changes that require DTO changes do require a version bump.

### Graph-aware visibility scopes

`VisibilityScope` enum governs which relationship types grant access:

| Scope | RelationshipTypes included |
|-------|---------------------------|
| `DirectOnly` | DirectManager, ActingManager |
| `DirectAndFunctional` | + DottedLineManager, ProjectManager |
| `SkipLevel` | + SkipLevelManager |
| `CalibrationPanel` | CalibrationCommitteeMember only |
| `AllManaged` | All except Mentor |

Graph traversal is single-hop only (actor → direct reports).
Skip-level is resolved by querying `SkipLevelManager` relationships explicitly,
not by recursive traversal. Max visible-set size assumed ≤ 500 employees per manager.

## Consequences

**Positive:**
- Single deployable, no distributed-system overhead
- Cross-BC reads are free (multiple DbContexts in one process)
- Visibility logic centralized and testable in isolation
- Each persona's surface is independently findable and modifiable

**Negative:**
- Program.cs must call 5 additional `app.Map*()` methods
- Single DbContext per context rule: if performance BC is split later, query routing
  in BFF handlers must be updated

**Future work (Phase 3b+):**
- Keyset cursor pagination for all list endpoints
- Response caching layer (IMemoryCache → Azure Cache for Redis)
- RBAC claim-based authorization policies per group (e.g., `manager` role)
- SignalR for real-time notification count push
