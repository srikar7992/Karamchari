# Phase 10 — Database Certification

**Result: ✅ PASS (schema/RLS strong); ⚠️ findings on FK density, decimal precision, Identity & provisioning.**

## Metrics (post-provision, DB: `Karamchari`)
| Metric | Value |
|---|---|
| Tables | 589 |
| Indexes (non-heap) | 572 |
| Foreign keys | **41** |
| Columns named `TenantId` | 476 |
| Migrations applied (`__EFMigrationsHistory`) | **16** (one per migrated DbContext) |
| Tenant schemas | `tenant_dev`, `tenant_acme`, `tenant_contoso` (136 tables each) |
| Other schemas | `dbo`, `security` |

## DbContexts (16 migrated)
HR, Payroll, TimeAttendance, PSA, Performance, Notifications, Compensation, Recruitment, Capability, Intelligence, Governance, Billing, Forecasting, Workflow, FinancialOps, OutboxRelay. **`IdentityDbContext` is NOT migrated** (CRITICAL — see Phase 4).

## RLS (verified)
- `sys.security_policies`: `TenantPolicy_dev/acme/contoso`, all `is_enabled=1`, `is_schema_bound=1`.
- `sys.security_predicates`: **1,680**.
- Predicate function `security.fn_tenant_access` (SCHEMABINDING). ✅

## Indexes / FKs / TenantId
- Indexes present (572) including tenant-scoped and PKs.
- **`TenantId` exists on 476 tables** — pervasive, consistent with RLS.
- **Finding (MEDIUM):** Only **41 foreign keys** across 589 tables. Referential integrity is largely **not enforced at the database level** (likely deferred to the application/aggregate layer). For a financial/payroll system this raises orphaned-row risk; review whether intra-aggregate FKs should be added.

## Other findings
- **CRITICAL:** Identity schema/tables absent; `IdentityDbContext` has no migrations (Phase 4).
- **HIGH (operational):** `ProvisionRlsInfrastructureAsync` is **not idempotent** — re-running `--provision-dev-tenants` throws `SqlException 3729: Cannot DROP FUNCTION 'security.fn_tenant_access' because it is being referenced by object 'TenantPolicy_dev'`, and the process exits **134 (SIGABRT)**. Re-provision/restart of an already-provisioned DB crashes. (Captured in `evidence/provision.log`.)
- **MEDIUM:** Decimal money/score columns created without explicit precision (defaults to `decimal(18,2)`, silent truncation risk) — see `startup.md`.

## Verdict
Schema generation, multi-tenant schemas, indexing, TenantId coverage, and RLS are **strong and verified**. Identity migration gap (CRITICAL), provisioning idempotency (HIGH), and FK/decimal concerns must be addressed.
