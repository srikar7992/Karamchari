# MULTI-TENANT WARFARE CERTIFICATION

**Date**: 2026-06-02  
**Program**: Sprint 1 + Sprint 2 Runtime Truth Program — Section 5  
**Evidence**: Live API, 4 tenant schemas (dev, acme, contoso, globex), SQL Server 2022

---

## Provisioning Evidence

| Tenant | Schema | Artifacts Verified | Status |
|--------|--------|-------------------|--------|
| dev | tenant_dev | 177 | CERTIFIED |
| acme | tenant_acme | 177 | CERTIFIED |
| contoso | tenant_contoso | 177 | CERTIFIED |
| globex | tenant_globex | 177 | CERTIFIED |

Log: `Artifact Equality Certified for schema tenant_{x}: 177 artifacts verified.`

## Index Provisioning (Critical Fix)

**Defect Found**: `TenantProvisioningService` used `SELECT * INTO` to clone tables — copies columns only, NOT indexes or constraints.

**Impact**: All unique constraints missing from tenant schemas → idempotency violated (7/25 concurrent creates succeeded instead of 1).

**Fix**: Added `CloneIndexesAsync()` to `TenantProvisioningService` — discovers all indexes from `dbo` template using `sys.indexes` catalog and recreates them in each tenant schema, including:
- Primary keys (`ALTER TABLE ... ADD CONSTRAINT PK_...`)
- Unique indexes (`CREATE UNIQUE INDEX ...`)
- Non-unique indexes

**Verification**: After re-provisioning, unique index `IX_Recruitment_Applications_TenantId_CandidateId_RequisitionId_{schema}` exists in all tenant schemas.

## Data Isolation Tests

| Test | Setup | Result | Status |
|------|-------|--------|--------|
| Same candidate email across tenants | Created `charlie.brown@e2e.test` in both `dev` and `acme` | Two distinct IDs returned (DEV vs ACME schema isolation) | PASS |
| Cross-tenant READ | Used ACME token to GET DEV candidate by ID | 404 (ACME schema has no such record) | PASS |
| Same email uniqueness within tenant | Created candidate, tried to create same email in same tenant | Rejected (email unique per tenant schema) | PASS |

## Schema Isolation Evidence

SQL Server schema structure verified:
```
dbo           → template tables (migration artifacts, 0 rows)
tenant_dev    → 177 tables with indexes, RLS policy applied
tenant_acme   → 177 tables with indexes, RLS policy applied
tenant_contoso → 177 tables with indexes, RLS policy applied
tenant_globex → 177 tables with indexes, RLS policy applied
```

RLS Policy: `TenantPolicy_{x}` in `security` schema enforces `tenant_id = SESSION_CONTEXT('tenant_id')` on all tenant-scoped tables.

---

## Verdict

**CERTIFIED** — Tenant schema isolation proven at database level. Cross-tenant read returns 404. Same-name records create separate rows in separate schemas. Critical defect (missing unique indexes) discovered and fixed.
