# ADR-0014 — Search Infrastructure

**Status:** Accepted  
**Date:** 2026-05-09  
**Author:** Srikar

---

## Context

Performance systems require rich search across employees, skills, goals,
promotion candidates, and succession lists. SQL `LIKE '%term%'` queries are
O(n) scans and cannot support fuzzy search, weighted ranking, or faceting.

Two-phase decision: pick the cheapest viable technology now; migrate to the
best at scale.

---

## Decision

### Phase 1 — SQL Server Full-Text Search

**Technology:** SQL Server FTS (`CONTAINS`, `FREETEXT`, `FREETEXTTABLE`)  
**Rationale:** Already in the stack. No new infrastructure. FTS on the
performance projection tables covers 80% of search use cases for tenants
under ~5,000 employees.

**Tables with FTS indexes (Phase 1):**

| Table | FTS Columns |
|-------|-------------|
| `EmployeeSkillInventoryItems` | `SkillName`, `ProficiencyNote` |
| `PromotionPipelineItems` | `EmployeeDisplayName`, `Department` |
| `TalentHeatmapEntries` | `EmployeeDisplayName`, `Department` |
| `TeamGoalSummaries` | `OwnerDisplayName`, `GoalTitle` |

**BFF endpoints (Phase 1):**
```
GET /api/v1/search/employees?q=&dept=&level=&page=&pageSize=
GET /api/v1/search/talent?q=&nineBoxPosition=&isAtRisk=&cycleId=
GET /api/v1/search/skills?q=&category=&proficiency=
```

**Tenant isolation:** RLS handles row filtering; FTS respects tenant schema.

### Phase 2 — Azure AI Search

**Trigger:** > 10,000 employees per tenant, or requirement for:
- Semantic search / NLP ranking
- Cross-field relevance scoring
- Real-time autocomplete at scale
- Vector search (AI-powered talent matching)

**Migration path:**
1. Add `ISearchIndexer` interface (already planned).
2. Register `SqlFtsSearchIndexer` now.
3. Register `AzureAiSearchIndexer` in Phase 2 — swap registration, no consumer changes.
4. Projection-based event consumers push delta documents on entity change.

**Index schema per document type** is defined in
`src/Backend/Karamchari.Performance/Search/` (new namespace).

### Indexing Pipeline

```
Domain Event → CQRS Projection Consumer → Projection Updated
→ SearchIndexingConsumer → ISearchIndexer.IndexAsync(document)
```

Idempotency: indexer receives `documentId = TenantId:EntityType:EntityId`.
Re-indexing same document = upsert (safe to replay).

### Operational Safety

- Max query length: 200 characters (enforced at BFF).
- Wildcard abuse prevention: FTS `CONTAINS` uses prefix terms only; no
  open-ended `*` wildcards exposed via API.
- Stale index protection: `LastIndexedAt` column on projection rows;
  BFF `DataAsOf` includes index lag.
- Reindex storm protection: bulk reindex uses a dedicated queue with
  `ConcurrentMessageLimit=2`.

---

## Consequences

**Phase 1:**
- Zero new infrastructure
- SQL FTS is less powerful than dedicated search
- Acceptable for MVP tenant sizes

**Phase 2 migration:**
- `ISearchIndexer` abstraction makes swap straightforward
- Azure AI Search has per-query cost; must budget
- Tenant isolation in Azure AI Search requires per-tenant indexes or OData filter
  (recommend filter-based isolation for cost, per-tenant indexes for strict isolation)
