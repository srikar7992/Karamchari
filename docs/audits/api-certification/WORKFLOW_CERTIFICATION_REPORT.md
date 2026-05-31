# WORKFLOW CERTIFICATION REPORT (Phase 4)

**Date:** 2026-05-30 · End-to-end business journeys, runtime, with persistence verification.

## Employee journey
```
Create/Onboard ──✅──> tenant_X.Employees row (201, correct TenantId)
        │
        └─ async (EmployeeOnboarded) ──✅──> tenant_X.PayrollProfiles (correct schema, no dbo leak;
                                              verified concurrently for dev/acme/contoso/globex)
        │
        ├─ Payroll run ──❓ NOT EXECUTED──> payslip / adjustments (endpoints exist; journey not run)
        │
        └─ Terminate (DELETE) ──✅──> soft-delete: Status=Terminated, record retained (200 on GET)
```
- **Create → Onboard → PayrollProfile:** ✅ VERIFIED at runtime (cross-tenant, incl. async hop and
  duplicate/restart resilience — see ASYNC_CERTIFICATION).
- **Termination:** ✅ VERIFIED — `DELETE` performs a soft-delete (`Employee.Terminate`), record retained
  with `Status=Terminated` (runtime-confirmed; integration test corrected to assert this).
- **Payroll run → payslip generation → archive:** ⏳ **NOT EXECUTED** — the payroll-run/payslip endpoints
  were not driven this pass. NOT VERIFIED.

## Billing journey
```
Create contract → add rate card → generate invoice → finalize → record payment → AR summary
```
- ⏳ **NOT EXECUTED.** Billing is B2B/contract-based (`/api/v1/billing/contracts`, `/invoices/generate`,
  `/invoices/{id}/finalize`, `/invoices/{id}/payment`, `/ar/summary`). `GET /ar/summary` returned 404 in the
  cross-section probe (GAP-5). The full journey requires seeded contracts/rate-cards and was not run.
  **NOT VERIFIED.**

## Approval workflow journey
- `GET /api/v1/approvals/my` → **500** (GAP-1, missing `Workflow_StepInstances` table). The approval
  inbox → approve/reject → state-transition journey is **BLOCKED** until provisioning is fixed.

## Verdict
**Employee onboarding journey (through payroll-profile creation + termination): VERIFIED.**
Payroll-run, Billing, and Approval journeys: **NOT VERIFIED / BLOCKED.** End-to-end business journeys are
only partially certified; the dominant blocker is GAP-1 (incomplete tenant provisioning).
