# ADR 0010 — Search & Talent Discovery Architecture

- **Status:** Accepted
- **Date:** 2026-05-08
- **Deciders:** Solo founder

## Context

Enterprise performance management requires talent discovery:
- Find employees with a specific skill at a target proficiency.
- Find promotion-ready employees in a career track.
- Find high performers for succession planning.
- Find employees at retention risk.
- Search goals and review submissions by keyword.

SQL LIKE queries on large tenant tables are neither scalable nor ranked.
Full-text search needs dedicated infrastructure.

## Decision

**Phase 1 (Year 1):** SQL Server Full-Text Search (FTS) against projection tables.
**Phase 2 (Year 2+):** Azure AI Search with projection-based indexing pipeline.

## Phase 1: SQL Server FTS

- Enable FTS on `EmployeeSkillInventoryItems`, `TalentHeatmapEntries`, and `Goals` tables.
- FTS columns: employee name, skill names, goal titles, career level names.
- Structured filters (department, performance bucket, promotion readiness) remain as
  regular SQL predicates combined with FTS.
- Tenant isolation: all FTS queries include `TenantId` as a predicate (RLS + FTS).
- No FTS against raw aggregate tables — projections only.

**Limitations of Phase 1:**
- No semantic search.
- Ranking quality limited by SQL FTS term-frequency scoring.
- Cross-field relevance is manual (UNION + scoring weighting in query layer).
- Full-text catalog rebuild is an offline DBA operation.

## Phase 2: Azure AI Search

Azure AI Search chosen over ElasticSearch/OpenSearch because:
- Native Azure RBAC + VNet integration (no separate infra to manage).
- Managed service, no cluster maintenance.
- Works with the existing Azure-hosted stack (ACA + Azure SQL).

**Indexing pipeline (Phase 2):**
```
Projection table row updated
→ ProjectionIndexingConsumer (MassTransit)
→ Azure AI Search SDK push (batch per 100ms window)
→ Index document with tenant-scoped partition key
```

Tenant isolation in Azure AI Search: all documents carry `tenant_id` field;
queries always filter by `tenant_id`. Documents are NOT partitioned by index
per tenant (that would create thousands of indexes). One index per entity type,
with `tenant_id` as a mandatory filter predicate.

**Search domains in scope:**
- Employee discovery (name, job title, department, skills, performance bucket)
- Skill discovery (skill name, category, employees at each proficiency level)
- Goal discovery (title, owner name, cycle, completion status)
- Talent discovery (succession-ready, high performers, retention risk)

## Talent Discovery Queries (Phase 1 SQL Approach)

```sql
-- Succession candidates for a role
SELECT e.*
FROM EmployeeSkillInventoryItems e
JOIN TalentHeatmapEntries t ON e.EmployeeId = t.EmployeeId
WHERE e.TenantId = @TenantId
  AND e.CareerLevelId = @TargetLevelId
  AND t.PerformanceBucket IN ('Exceeds', 'TopPerformer')
  AND t.IsAtRetentionRisk = 0
ORDER BY t.CompositeScore DESC

-- Retention risk employees
SELECT * FROM TalentHeatmapEntries
WHERE TenantId = @TenantId
  AND IsAtRetentionRisk = 1
  AND CompositeScore > 70
ORDER BY CompositeScore DESC
```

## Consequences

- Phase 1 FTS catalog requires `CREATE FULLTEXT CATALOG` in the schema provisioning
  script. Update `RlsScriptGenerator` to include FTS setup per tenant.
- Phase 2 requires Azure AI Search resource and managed identity access grant.
  Add to the infrastructure provisioning checklist.
- Search queries must always include `TenantId` as a mandatory filter — no exceptions.
  The query layer must enforce this, not rely on RLS alone (FTS bypasses RLS predicates
  in some SQL Server configurations).
