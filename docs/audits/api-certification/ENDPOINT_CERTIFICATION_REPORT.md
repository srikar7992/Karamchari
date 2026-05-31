# ENDPOINT CERTIFICATION REPORT (Phase 2)

**Date:** 2026-05-30 · **Method:** live requests against the running API (no code/test trust).
Inventory: **170 operations / 157 paths** (`api-inventory.md`). This pass runtime-exercised a
representative cross-section + all negative/security paths; the full 170-op matrix is NOT exhaustively run.

## Runtime results (executed this pass)
| Endpoint | Path test | Result | Note |
|---|---|---|---|
| Login (all 16 seeded users) | positive | ✅ 200 + JWT | dev/acme/contoso/globex × 4 roles |
| `POST /api/v1/hr/employees/` | positive | ✅ 201 | persists to tenant schema (+ async payroll profile) |
| `GET /api/v1/hr/employees/` | positive | ✅ 200 | |
| `GET /api/v1/hr/employees/{id}` (cross-tenant) | security | ✅ 404 | IDOR denied |
| `GET /api/v1/time/holidays` | positive | ✅ 200 | |
| `GET /api/v1/workforce/attendance/sessions/live` | positive | ✅ 200 | |
| `GET /api/v1/leaves/my` | positive | ✅ 200 | |
| `GET /api/v1/strategy/health` | positive | ✅ 200 | |
| `POST /api/v1/hr/employees/` (globex) | positive | ✅ 201 | new tenant fully functional |
| `GET /api/v1/time/leave-balances` | positive | ❌ **500** | missing table `LeaveBalanceEntries` (GAP-1) |
| `GET /api/v1/approvals/my` | positive | ❌ **500** | missing table `Workflow_StepInstances` (GAP-1) |
| `GET /api/v1/capability/skills` | positive | ❌ **403** | empty permissions in JWT (GAP-2) |
| `GET /api/v1/billing/ar/summary` | positive | ⚠️ 404 | not characterized (GAP-5) |
| `POST /api/tenants` (no auth) | security | ✅ 401 | **fixed this pass** (was anonymous) |
| `POST /api/tenants` (auth) | positive | ✅ 201 | |
| any protected, no token | negative | ✅ 401 | |
| any protected, malformed token | negative | ✅ 401 | |
| `alg=none` forged token | security | ✅ 401 | (prior security cert) |
| readonly user read | authz | ✅ 200 | |

## Persistence-path verification (not just API response)
- Employee onboard → row in `tenant_X.Employees` + async `tenant_X.PayrollProfiles`, correct `TenantId`
  stamp, **zero `dbo` leakage** (verified in ASYNC_CERTIFICATION, re-confirmed for `globex`).
- Cross-tenant read returns 404; RLS fail-closed (1,680 predicates) prevents data bleed.

## Telemetry-path (Phase 10, sampled)
- Every request flows correlation/trace IDs (response headers + Seq); 500s emit structured errors with a
  `traceId`/`correlationId` (RFC 7807). Custom `karamchari_outbox_*` + `http_server_*` metrics in Prometheus.

## Verdict
- **Certified (positive+negative+security+persistence):** Identity, HR (employee), Attendance (holidays/
  sessions), Leave (my), Strategy, Tenants (post-fix).
- **BLOCKED by GAP-1 (missing tables):** TimeAttendance leave-balances, Workflow approvals.
- **BLOCKED by GAP-2 (empty permissions):** Capability and any permission-gated endpoint.
- **Full 170-op runtime matrix: NOT exhaustively executed** → those operations are NOT VERIFIED individually.
