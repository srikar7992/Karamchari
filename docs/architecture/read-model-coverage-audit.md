# Read-model coverage audit — pre-Attendance scan

Date: 2026-06-23
Scope: every Karamchari module. Inventory the GET endpoints the backend
exposes (in `src/Backend/Hosts/Karamchari.Api/BFF/**`), the frontend routes
that should consume them, and the portal hooks that bridge the two. The goal
is to pick the next module for Phase 1 integration predictably rather than
discovering gaps mid-implementation (the situation thatelayed Recruitment).

## Legend

- **Write side**: command handlers / mutations exist in module.
- **Read side**: GET endpoints registered in BFF.
- **Portal hook**: `src/Frontend/portal/src/lib/hooks/use-*.ts` wired.
- **Portal screen**: `src/Frontend/portal/src/app/.../page.tsx` route mounted.
- **Path drift**: hook calls a path that does NOT match the BFF group prefix.

## Module table

| Module | Write side | BFF GET endpoints | Portal hook | Portal route | Path drift | Phase 1 ready |
|---|---|---|---|---|---|---|
| **Recruitment** | ✓ | 9 (under `/api/v1/recruitment/`) | `use-recruitment.ts` | `/recruitment`, `/recruitment/[id]`, `/recruitment/offers/[id]` | ✓ aligned | ✅ DONE |
| **Employee / HR** | ✓ | 7 (under `/api/v1/hr/employees`) | `use-employees.ts` | `/directory`, `/directory/[id]` | ✓ aligned | ✅ effectively done |
| **Payroll** (core) | ✓ | 5 GET on runs/components (`/api/v1/payroll/…`) | `use-payroll.ts` | `/payroll`, `/payroll/runs/[id]` | ✓ aligned | ✅ screens in place |
| **Payroll Phase1a** (FnF, arrears, corrections, reimbursements, loans, varpay, revisions, simulations, disbursements, cockpit, anomalies) | ✓ | 22 GET (`/api/v1/payroll/{fnf,arrears,corrections,reimbursements,loans,variable-pay,revisions,simulations,disbursements,cockpit}`) | `use-payroll-phase1a.ts` | `/payroll/cockpit`, `/payroll/fnf`, `/payroll/reimbursements`, `/payroll/simulation` | ✓ aligned | ✅ hooks ready |
| **ESS** | partial | 11 GET (`/api/ess/*`, `/api/ess/me/*`) | `use-ess.ts` (only payslips, YTD, tax-sim, analyze) | `/ess/payslips`, `/ess/tax-declarations` | ✓ aligned (scope: payslips slice only) | 🟡 partial — leaves/benefits/learning/delegation reads exposed in BFF but no portal consumption |
| **Billing** | ✓ | 3 GET (`/api/billing/ar/summary`, `/api/collections/`, `/api/forecast/summary`) | `use-billing.ts` | `/admin/billing/collections`, `/admin/billing/forecast` | ✓ aligned | ✅ screens wired |
| **Admin / Compliance / Governance** | partial | 15 GET (`/api/compliance`, `/api/admin`, `/api/governance`, `/api/v1/compliance/gate`) | `use-admin.ts` (only pending declarations + approve/reject) | `/admin/compliance`, `/admin/tax-approvals` | ⚠ partial — pages also call `fetch('/api/compliance/health')` and `fetch('/api/payroll/runs')` from route component instead of hook | 🟡 partial |
| **Attendance / Time** | ✓ | 42 GET across five prefixes: `/api/v1/workforce/attendance`, `/api/v1/time`, `/api/v1/workforce/rosters`, `/api/v1/leaves`, `/api/v1/rosters`, `/api/v1/workforce/attendance/reports`, `/api/v1/attendance/{punch,geo-zones,enrollment,biometric-templates,biometric-devices}` | `use-time.ts`, `use-timesheets.ts` (raw `fetch()`) | `/admin/attendance`, `/settings/holidays`, `/settings/leave-policies`, `/leave`, `/timesheets`, `/approvals/leave`, `/approvals/timesheets` | ❌ **drift**: hooks call `/api/time/holidays`, `/api/time/leave-policies`, `/api/time/leave-balances`, `/api/time/leave-requests/*`, `/api/time/timesheets/*` — but BFF mounts these under `/api/v1/time/*`. Same for `/api/time/leave-requests/pending` which appears not to exist in BFF (only `/api/v1/leaves/my` + `/api/v1/leaves/{id}` exist). | ❌ not ready |
| **Leave** (separate from time) | ✓ | 4 GET (`/api/v1/leaves/my`, `/api/v1/leaves/{id}`, `/api/v1/leaves/balance/{policyId}`, `/api/v1/leaves/balance/history/{policyId}`) | `use-time.ts` (pending-leave-requests hook hits nonexistent route) | `/leave`, `/approvals/leave` | ❌ mismatch | ❌ not ready |
| **Capability** | ✓ | 15 GET (`/api/v1/capability/*`) | — | — | n/a | ❌ portal not built |
| **Compensation** | ✓ | 7 GET (`/api/v1/compensation/*`) | — | — | n/a | ❌ portal not built |
| **Succession** | ✓ | 12 GET (`/api/v1/succession/*`) | — | — | n/a | ❌ portal not built |
| **Engagement** | ✓ | 8 GET (`/api/v1/engagement/*`) | — | — | n/a | ❌ portal not built |
| **Helpdesk** | ✓ | 7 GET (`/api/v1/helpdesk/*`) | — | — | n/a | ❌ portal not built |
| **Asset Management** | ✓ | 6 GET (`/api/v1/assets/*`) | — | — | n/a | ❌ portal not built |
| **Onboarding** | ✓ | 4 GET (`/api/v1/onboarding/*`) | — | — | n/a | ❌ portal not built |
| **Intelligence / Strategy / Workforce** | ✓ | 12 GET (`/api/v1/intelligence/*`, `/api/v1/strategy/*`, `/api/v1/workforce-intelligence/*`) | — | `/analytics` page uses raw `fetch('/api/analytics/simulate', …)` which is unmatched by BFF | ❌ drift | ❌ not ready |
| **Platform Intelligence** | ✓ | 8 GET (`/api/v1/platform-intelligence/*`) | — | — | n/a | ❌ portal not built |
| **Forecasting / workforce planning** | ✓ | 3 GET (`/api/v1/workforce-planning/*`) | — | — | n/a | ❌ portal not built |
| **Extensibility** | ✓ | 4 GET (`/api/v1/extensibility/*`) | — | — | n/a | ❌ portal not built |
| **Integration / Webhooks** | ✓ | 4 GET (`/api/v1/integration/webhooks/*`) | — | — | n/a | ❌ portal not built |
| **Analytics** | ✓ | 7 GET (`/api/v1/analytics/*`) | — | `/analytics` page calls `/api/analytics/projects` and `/api/analytics/anomalies` raw — neither matches BFF (`/api/v1/analytics/headcount-trend` etc.) | ❌ drift | ❌ not ready |
| **PSA** | ✓ | 11 GET (`/api/v1/psa`) | — | `/timesheets` page calls `fetch('/api/psa/employees/${userId}/projects')` (matches BFF path style) but no hook | partial drift | ❌ not ready |
| **Notifications** | partial | 2 GET (`/api/v1/notifications`, `/api/v1/notifications/unread-count`) | — | — | n/a | ❌ portal not built |
| **Manager workspace** | ✓ | 5 GET (`/api/v1/manager/*`) | — | — | n/a | ❌ portal not built |
| **Executive workspace** | ✓ | 4 GET (`/api/v1/executive/*`, `/api/v1/ops/dashboards/*`) | — | — | n/a | ❌ portal not built |
| **HR workspace (review-cycles, calibration, promotions-pipeline)** | ✓ | 3 GET + 2 export (`/api/v1/hr/*`) | partial `use-employees` overlaps directory | — | n/a | ❌ portal not built |
| **Workflow** | ✓ | 3 GET (`/api/v1/workflow/*`) | — | — | n/a | ❌ portal not built |
| **Ops / Health / Catalog** | ✓ | 9 GET (`/api/v1/ops/*`) | — | — | n/a | ❌ portal not built |
| **Audit** | ✓ | 3 GET (`/api/v1/audit/*`) | — | — | n/a | ❌ portal not built |
| **Search** | ✓ | 3 GET (`/api/v1/search/*`) | — | — | n/a | ❌ portal not built |
| **DataMigration (bulk imports)** | ✓ | 3 GET + writes (`/api/v1/imports/*`) | — | — | n/a | ❌ portal not built |
| **Tenants** | ✓ | endpoint count not enumerated in this scan | — | — | n/a | ❌ portal not built |

## Path-prefix schemas found

- `/api/v1/<module>/...` — Recruitment, Time, Attendance (v1 workforce + v1 attendance), Leaves,
  Rosters, Capability, Compensation, Engagement, Helpdesk, AssetManagement, Onboarding,
  Intelligence, Strategy, Workforce-intelligence, Platform-intelligence, Workforce-planning,
  Extensibility, Integration, Analytics, PSA, Notifications, Manager, Executive, HR (employees +
  reports), Workflow, Ops, Audit, Search, Compliance (gate only), DataMigration imports,
  Payroll (all sub-modules), Governance (audit page only — `/api/governance` is non-versioned).
- `/api/<module>/...` (non-versioned) — Billing, Collections, Forecast, Compliance, Admin,
  Governance, ESS.
- Mixed within same module — Compliance exposes both `/api/compliance/...` and
  `/api/v1/compliance/gate/...`. ESS exposes `/api/ess/...` for self-service and
  `/api/ess/manager/...` for manager inbox. Tenants endpoints remain to be enumerated.

## Major findings

1. **Recruitment is the only module where any Phase 1 work is complete end-to-end.** No
   other module has gotten the read-side endpoint review, hook wire, and portal screen
   integration at the same time.
2. **Payroll + HR + Billing are the next-closest to "done"** — hooks + screens wired but
   each with caveats:
   - Payroll's FnF/cockpit/etc actually use `/api/v1/payroll/...` paths correctly, but the
     BFF registers them under `/api/v1/payroll` only for some sub-paths. Confirm each
     component's prefix closely before adding new screens.
   - Admin endpoints mix `/api/admin/declarations/...` (used by `use-admin.ts`) and
     `/api/compliance/health`, `/api/payroll/runs` — the latter two are called directly
     from `admin/compliance/page.tsx` instead of via a hook. Refactor to hook pattern.
3. **Attendance/Time is broken in portal today**: `use-time.ts` and `use-timesheets.ts`
   call `/api/time/*` (missing the `/v1`) and `/api/time/leave-requests/*` (path not in
   BFF at all — leaves live under `/api/v1/leaves/*`). The Next dev server serves portal
   routes fine (Layer 3 SSR pass), but the actual hydration fetches 404. This makes
   Attendance the cleanest Phase 1 candidate — backend read side is rich (42 GET),
   portal has placeholder routes with intentionally-naive hooks that need rewiring.
4. **BFF prefixes are inconsistent**: some modules live under `/api/v1/...` and some under
   `/api/...`. Each Phase 1 effort must confirm its module's actual prefix before wiring
   hooks. Recruitment set the precedent of `/api/v1/recruitment/*` — newer modules should
   follow that, but legacy ones (Billing, Compliance, ESS) won't unless we migrate also.

## Module priority after this audit

Per the agreed module priority (Recruitment → Attendance → Workflow → Payroll), Phase 1B's
next work target is **Attendance**. Acceptance criteria for Attendance Phase 1:

1. Re-write `use-time.ts` and `use-timesheets.ts` to call the configured `api` client at
   `/api/v1/time/*`, `/api/v1/leaves/*`, `/api/v1/workforce/attendance/*`,
   `/api/v1/workforce/rosters/*`. Eliminate the `/api/time/*` drift.
2. Validate `/api/v1/leaves/pending` — confirm the BFF has it. If missing, build it first
   or refactor the pending-leave-requests hook to query `/api/v1/leaves/my` filtered by
   status.
3. Confirm the BFF smell: `/api/v1/workforce/attendance/reports/*` is mounted but no
   portal screen consumes it. Optional: leave for v2.
4. Same end-to-end smoke pattern as Recruitment: Layer 1 (endpoint registration),
   Layer 2 (live API 200), Layer 3 (portal SSR green), Layer 4-5 (browser flows), then
   push + PR.

After Attendance, **Workflow** is next in priority. The Workflow BFF has only 3 GETs
(`/api/v1/workflow/{id}/approvals`, `/api/v1/workflow/` definitions list+by-id) — minimal
read side today; expect to add reporting GETs (active runs, queue, throughput metrics)
based on Operational dashboards.

Module candidates with high read-side richness but no portal work yet (Capability 15 GETs,
Compensation 7 GETs, Succession 12 GETs, Engagement 8 GETs, Helpdesk 7 GETs, Asset 6 GETs,
Onboarding 4 GETs, Intelligence 12 GETs, Platform-intelligence 8 GETs, Notifications 2)
are flagged for out-of-priority phases.

Non-versioned prefixes (Billing, Compliance, Governance, Admin, ESS, Collections, Forecast)
**must NOT be renamed** in the Attendance phase — they're stable contracts backed by
frontend calls in production paths. Add an explicit follow-up issue to version them via
redirects if/when we standardise on `/api/v2/<module>`.

## Drift action list (small, immediate)

These drifts affect currently-rendering portal pages and should be repaired before or
during Attendance Phase 1:

- `admin/compliance/page.tsx` raw `fetch('/api/compliance/health')` and
  `fetch('/api/payroll/runs')` → migrate to a hook (use-compliance + use-payroll already
  exist; just adopt).
- `analytics/page.tsx` raw `fetch('/api/analytics/projects')`,
  `fetch('/api/analytics/anomalies')`, `fetch('/api/analytics/simulate', …)` — none of
  these match the BFF (`/api/v1/analytics/headcount-trend …`). Build an `use-analytics`
  hook or rebuild the page against real endpoints.
- `timesheets/page.tsx` raw `fetch('/api/psa/employees/${ userId }/projects')` — adopt into
  a `use-psa.ts` hook (PSA module has it exposed already).
- All raw `fetch()` in `use-time.ts` and `use-timesheets.ts` — replace with `api.get`
  client calls at the right `/api/v1/...` prefix.

## Next step

Per the agreed priority: execute Attendance Phase 1 — read endpoints audit on the next
session boot. Place recordings/learnings back into this audit memo under a new section
"Attendance Phase 1 — discovered deltas" so the next module picks benefit from it.