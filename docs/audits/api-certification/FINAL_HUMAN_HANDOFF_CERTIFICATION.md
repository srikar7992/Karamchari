# FINAL HUMAN HANDOFF CERTIFICATION (Phase 12)

**Program:** Karamchari Module, API & Developer Experience Certification
**Date:** 2026-05-30 · Runtime-proof only. Build under test: API `karamchari-api:local` (rebuilt this pass),
Worker `d90fea0e…`. Reports: `docs/audits/api-certification/`.

## FINAL STATUS: ⚠️ CONDITIONALLY APPROVED FOR FEATURE DEVELOPMENT
The developer-experience foundation is now in place (discovery, default credentials, seeding, auth docs,
per-endpoint security). **Two platform-level defects (GAP-1, GAP-2) block full module certification** and
must be fixed before "every major business capability works without tribal knowledge" is true.

## What was DELIVERED this pass (not just reported)
1. **Developer seeding system** — `DevDataSeeder`: 16 documented users (admin/manager/employee/readonly ×
   dev/acme/contoso/globex), all runtime-verified to log in. New tenant **globex** provisioned. (Phase 6/7)
2. **`AUTHENTICATION_GUIDE.md`** — copy-paste login, credentials table, token/refresh/role model. (Phase 7)
3. **OpenAPI per-operation security** — Scalar now shows correct per-endpoint auth (161 protected / 9 anon),
   was a single blanket requirement. (Phase 5)
4. **Security fix** — `POST /api/tenants` no longer anonymous (now 401 without auth). (Phase 9)
5. **Code fix** — `approvals/my` owned-entity query corrected (surfaced the real GAP-1 missing-table cause).
6. **Test fix** — `DeleteEmployee` integration test corrected to assert soft-delete (from prior pass).

## Per-module gate (Phase 12 criteria)
| Module | Endpoint | Workflow | Scalar | Seed | Default users | Telemetry | Verdict |
|---|---|---|---|---|---|---|---|
| Identity | ✅ | ✅ login/refresh | ✅ | ✅ | ✅ | ✅ | **APPROVED** |
| HR (Employee) | ✅ | ✅ onboard→profile→terminate | ✅ | ✅ | ✅ | ✅ | **APPROVED** |
| Payroll | ✅ profile | ⚠️ run/payslip not run | ✅ | ⚠️ | ✅ | ✅ | CONDITIONAL |
| TimeAttendance | ❌ leave-balances 500 | — | ✅ | ⚠️ | ✅ | ✅ | **BLOCKED (GAP-1)** |
| Workflow | ❌ approvals 500 | ❌ | ✅ | — | ✅ | ✅ | **BLOCKED (GAP-1)** |
| Capability | ❌ 403 | — | ✅ | — | ✅ | ✅ | **BLOCKED (GAP-2)** |
| Billing | ⚠️ 404 | ❌ not run | ✅ | — | ✅ | ✅ | NOT VERIFIED |
| Others (PSA, Performance, Notifications, Documents, Recruitment, Compensation, Forecasting, Governance, FinancialOps, Intelligence) | auth ✅, not exercised | — | ✅ | varies | ✅ | ✅ | NOT VERIFIED |

## Blocking defects (must fix before full handoff)
- **GAP-1 (HIGH):** tenant provisioning creates **137/181** tables — ~44 owned-collection/child tables
  (`LeaveBalanceEntries`, `Workflow_StepInstances`, …) never cloned → multiple endpoints 500. Fix the
  `ITenantTableRegistry` (derive from the EF model) and re-provision.
- **GAP-2 (HIGH):** login issues JWTs with **empty permissions** → every permission-gated endpoint 403s even
  for admin. Wire role→permission mapping at login.

## Final certification rule (verbatim) — assessment
> "approved ONLY when a newly onboarded engineer can build, run, authenticate, discover, debug, understand,
> and extend **every major business capability** without tribal knowledge."

- **build / run / authenticate / discover / debug / understand:** ✅ achieved (the DX foundation this program
  asked for is now real and verified).
- **extend *every* major capability:** ❌ not yet — TimeAttendance/Workflow/Capability are runtime-broken
  (GAP-1/GAP-2), and most modules are NOT VERIFIED individually.

**Therefore: CONDITIONALLY APPROVED.** The platform is ready for feature development on the certified core
(Identity + HR + onboarding) and for *fixing the two blockers*, but is **not** unconditionally approved
across all modules until GAP-1 and GAP-2 are closed and the per-module/endpoint matrix is fully exercised.

## Outputs produced
`api-inventory.md`, `SCALAR_CERTIFICATION_REPORT.md`, `AUTHENTICATION_GUIDE.md`,
`ENDPOINT_CERTIFICATION_REPORT.md`, `MODULE_CERTIFICATION_REPORT.md`, `WORKFLOW_CERTIFICATION_REPORT.md`,
`DEVELOPER_EXPERIENCE_REPORT.md`, `ENDPOINT_GAP_ANALYSIS.md`, `PERFORMANCE_CERTIFICATION_REPORT.md`,
`FINAL_HUMAN_HANDOFF_CERTIFICATION.md`. Repro: `scripts/runtime/dx-cert.sh`.
