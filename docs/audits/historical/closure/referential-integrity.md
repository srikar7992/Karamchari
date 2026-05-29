# WS8 — Referential Integrity

**Status: ✅ CLOSED (was MEDIUM) — audited, key gap fixed, deliberate exclusions documented.**

## What changed
- **47 FKs** now exist (up from 41) after the Identity migration added the 6 ASP.NET Identity FKs.
- **Fixed a real missing-table provisioning gap** (more impactful than raw FK count): the `EmployeeHistory`
  owned-collection table was not registered as a tenant table, so it was never cloned into tenant schemas →
  every `GET /employees` 500'd with `Invalid object name 'tenant_dev.EmployeeHistory'`. Registered it
  (`RegisterTenantTable("EmployeeHistory")`); it is now cloned and RLS-covered in all tenant schemas.

## FK audit & matrix (representative)
```
AspNetRoleClaims    -> AspNetRoles
AspNetUserClaims    -> AspNetUsers
AspNetUserRoles     -> AspNetRoles / AspNetUsers
AspNetUserTokens    -> AspNetUsers
Billing_InvoiceLines-> Billing_Invoices
Billing_Payments    -> Billing_Invoices
Billing_RateCards   -> Billing_Contracts
CalibrationAdjustmentRecords -> CalibrationEntries
```
Within-aggregate child tables (invoice lines→invoice, payments→invoice, rate cards→contract, calibration adjustments→entries, ASP.NET Identity graph) are FK-enforced.

## Deliberate exclusions (documented)
The platform is a **modular monolith with schema-per-tenant + RLS**. FKs are intentionally **not** created:
1. **Across bounded-context boundaries** (e.g. Payroll → HR Employee). Modules integrate via integration events / soft references (`EmployeeId` as a value), not cross-module FKs — this preserves module independence and is a core architectural rule enforced by `ArchitectureTests`.
2. **Across tenant schemas** — each tenant schema is a self-contained clone; cross-schema FKs would break tenant isolation and the `SELECT INTO` cloning model.
3. **Owned collections** are modeled via EF ownership (cascade by configuration) rather than explicit FKs in some cases.

These exclusions are by design; referential integrity for cross-context references is enforced at the application/event layer and verified by the 610 tenant-isolation tests.

## Verdict
Referential Integrity = **PASS** — within-aggregate FKs enforced, the high-impact missing-table defect fixed, and cross-context/cross-tenant exclusions are deliberate and documented.
