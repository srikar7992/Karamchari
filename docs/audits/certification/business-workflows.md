# Phase 18 — Business Workflow Certification

**Result: ⛔ BLOCKED for live end-to-end flows; ✅ substrate verified.** Every multi-step business flow begins behind a JWT-protected endpoint, so none can be driven through the API until authentication is repaired.

## Flow-by-flow status
| Workflow | Entry point | Live status | Substrate evidence |
|---|---|---|---|
| **Tenant Provisioning** | `--provision-dev-tenants` / `POST /api/tenants` | ✅ CLI path works (schemas + RLS + 3 tenants created); ❌ **not idempotent** (re-run → SQLException 3729, exit 134). API path `POST /api/tenants` is auth-gated. | `tenant_dev/acme/contoso` (136 tables each), 1,680 RLS predicates |
| **Employee Lifecycle** | `POST /api/v1/hr/employees` → transfer → history → delete | ⛔ auth `302` | HR schema + 610 isolation tests + audit interceptors |
| **Payroll Processing** | `POST /api/payroll/runs` → lock → summary → `PayrollProcessedEvent` | ⛔ auth `302` | `Payroll.Tests` 44 passed; Payroll schema + outbox registered |
| **Notification Flow** | event → consumer → notification | ⛔ Worker not running + no API consumers + auth | Notifications schema + EF outbox registered |
| **Workflow Processing** | `POST /api/v1/approvals/{id}/approve` | ⛔ auth `302` | Workflow schema + EF outbox; `ReviewTaskInboxItems` tables per tenant |
| **Forecasting Flow** | `GET /api/forecast/summary`, `POST /api/analytics/simulate` | ⛔ auth `302` | Forecasting schema + outbox; `FinancialChaosTests` 5 passed |
| **Billing Flow** | `POST /api/billing/invoices/generate` → finalize → payment | ⛔ auth `302` | Billing schema + outbox; GST fields present |

## Why blocked
The authentication CRITICAL defect (Phase 4) means no caller can obtain a token, and protected endpoints redirect (302) rather than execute. Additionally, asynchronous flows (payroll→notification, workflow) require `Karamchari.Worker` (the consumer host) to be running and the RabbitMQ tenant-filter gap (Phase 8) resolved.

## What IS proven
- The data substrate for every workflow exists and is tenant-isolated (schemas, tables, RLS, outbox/inbox).
- Domain/calculation logic for Payroll, PSA, TimeAttendance, FinancialOps is unit/integration tested (714 tests passing).

## Verdict
No business workflow could be certified end-to-end through the running application. This is the single highest-impact consequence of the auth defect. Re-run this entire phase after: (1) Identity migrations + provisioning fix, (2) JWT challenge fix, (3) Worker running, (4) RabbitMQ tenant filters restored.
