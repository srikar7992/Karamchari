# IDEMPOTENCY RUNTIME CERTIFICATION

**Date**: 2026-06-02  
**Program**: Sprint 1 + Sprint 2 Runtime Truth Program — Section 11  
**Evidence**: Unit test (Testcontainers SQL Server) + DB schema verification

---

## Test Execution

### HighConcurrency50ParallelApplyRequestsShouldResultInExactlyOneApplication

**Test location**: `Karamchari.Recruitment.Tests/Infrastructure/HardProofTests.cs`

**Methodology**:
- 50 concurrent `Task.WhenAll` calls to `ApplyCandidateHandler.Handle()`
- Real SQL Server instance (Testcontainers)
- Shared `candidateId` + `requisitionId`
- Unique index on `(TenantId, CandidateId, RequisitionId)` enforced

**Result**:
```
Passed! Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 935ms
```

**Assertions Verified**:
- Exactly 1 result with valid ID (49 threw exceptions) ✓
- DB count: `SELECT COUNT(*) FROM ... WHERE CandidateId=? AND RequisitionId=?` = 1 ✓

### Unique Index Provisioning Fix

**Defect**: Unique index on `Recruitment_Applications` NOT present in tenant schemas after provisioning (SELECT * INTO copies columns only).

**Evidence of fix**: `CloneIndexesAsync()` added to `TenantProvisioningService` — clones ALL indexes from dbo template to tenant schema after table creation.

**SQL proof** (tenant_dev schema after re-provisioning):
```sql
SELECT i.name, i.is_unique FROM sys.indexes i
JOIN sys.tables t ON i.object_id = t.object_id  
JOIN sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = 'tenant_dev' AND t.name = 'Recruitment_Applications'

-- Result:
PK_tenant_dev_Recruitment_Applications           is_unique=1  is_primary_key=1
IX_Recruitment_Applications_TenantId_tenant_dev  is_unique=0
IX_Recruitment_Applications_TenantId_CandidateId_RequisitionId_tenant_dev  is_unique=1
```

---

## Verdict

**CERTIFIED** — 50 concurrent apply requests produce exactly 1 application. DB-level unique constraint confirmed in tenant schemas. Critical provisioning defect fixed and verified.
