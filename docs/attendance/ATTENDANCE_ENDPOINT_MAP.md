# Attendance Endpoint Map

Phase 1A deliverable. Maps each portal hook call to its correct backend path and documents drift.

---

## Summary of Drift

All hooks in `use-time.ts` and `use-timesheets.ts` used raw `fetch('/api/time/*')`.
These are relative URLs that hit Next.js (no API routes exist there) — always 404 in production.

The `attendanceApi.ts` file used `${BASE_URL}/api/attendance/*` paths that don't match the backend's
`/api/v1/workforce/attendance/*` routes.

**Fix:** All hooks now use `api.get/post(path)` from `src/lib/api/client.ts`, which prepends
`NEXT_PUBLIC_API_BASE_URL` (defaults to `http://localhost:60463`). This matches the Recruitment pattern.

---

## use-time.ts Hook Map

| Hook | Old Path | New Path | Status |
|------|----------|----------|--------|
| `useHolidays` | `fetch('/api/time/holidays')` | `api.get('/api/v1/time/holidays')` | Fixed |
| `useAddHoliday` | `fetch('/api/time/holidays', POST)` | N/A — no BFF endpoint yet | **Deferred Phase 2** |
| `useLeavePolicies` | `fetch('/api/time/leave-policies')` | `api.get('/api/v1/leaves/policies')` | Fixed (new endpoint) |
| `useAddLeavePolicy` | `fetch('/api/time/leave-policies', POST)` | `api.post('/api/v1/leaves/policies')` | Fixed (new endpoint) |
| `useLeaveBalances` | `fetch('/api/time/leave-balances')` | `api.get('/api/v1/time/leave-balances')` | Fixed |
| `useCalculateLeave` | `fetch('/api/time/leave-requests/calculate', POST)` | Removed — compute client-side | **Removed** |
| `useSubmitLeaveRequest` | `fetch('/api/time/leave-requests', POST)` | `api.post('/api/v1/leaves/request')` + `/submit` | Fixed |
| `useMyLeaveRequests` | (missing) | `api.get('/api/v1/leaves/my')` | **Added** |
| `usePendingLeaveRequests` | `fetch('/api/time/leave-requests/pending')` | `api.get('/api/v1/leaves/pending')` | Fixed (new endpoint) |
| `useApproveLeaveRequest` | `fetch('/api/time/leave-requests/{id}/approve', PUT)` | `api.post('/api/v1/leaves/{id}/approve')` | Fixed (new endpoint) |
| `useRejectLeaveRequest` | `fetch('/api/time/leave-requests/{id}/reject', PUT)` | `api.post('/api/v1/leaves/{id}/reject')` | Fixed (new endpoint) |

---

## use-timesheets.ts Hook Map

| Hook | Old Path | New Path | Status |
|------|----------|----------|--------|
| `useCurrentTimesheet` | `fetch('/api/time/timesheets/current-week')` | `api.get('/api/v1/time/timesheets/current')` | Fixed |
| `useSubmitTimesheet` | `fetch('/api/time/timesheets', POST)` | `api.post('/api/v1/time/timesheets')` | Fixed (basic endpoint) |
| `usePendingTimesheets` | `fetch('/api/time/timesheets/pending')` | N/A — no BFF endpoint | **Deferred Phase 2** |
| `useApproveTimesheet` | `fetch('/api/time/timesheets/{id}/approve', PUT)` | N/A — no BFF endpoint | **Deferred Phase 2** |
| `useRejectTimesheet` | `fetch('/api/time/timesheets/{id}/reject', PUT)` | N/A — no BFF endpoint | **Deferred Phase 2** |

---

## attendanceApi.ts Path Map

| Function | Old Path | New Path | Status |
|----------|----------|----------|--------|
| `fetchLiveAttendance` | `${BASE_URL}/api/attendance/live` | `/api/v1/workforce/attendance/sessions/live` | Fixed |
| `fetchAttendanceKpis` | `${BASE_URL}/api/attendance/kpis` | `/api/v1/workforce/attendance/dashboard/health` | Fixed |
| `fetchDlq` | `${BASE_URL}/api/attendance/dlq` | No BFF endpoint yet | **Deferred Phase 2** |
| `mapDlq` | `${BASE_URL}/api/attendance/dlq/{id}/map` | No BFF endpoint yet | **Deferred Phase 2** |
| `fetchFraudFlags` | `${BASE_URL}/api/attendance/fraud` | No BFF endpoint yet | **Deferred Phase 2** |
| `reviewFraud` | `${BASE_URL}/api/attendance/fraud/{id}/review` | No BFF endpoint yet | **Deferred Phase 2** |
| `triggerReprocess` | `${BASE_URL}/api/attendance/reprocess` | No BFF endpoint yet | **Deferred Phase 2** |
| `fetchEmployees` | `${BASE_URL}/api/hr/employees/compact` | `/api/v1/employees/compact` (verify) | **Needs verification** |
| `fetchJobProgress` | `${BASE_URL}/api/attendance/reprocess/{jobId}` | No BFF endpoint yet | **Deferred Phase 2** |

---

## Backend Endpoints Added in Phase 1

All added to `LeaveEndpoints.cs`:

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/v1/leaves/policies` | List all tenant leave policies |
| POST | `/api/v1/leaves/policies` | Create a new leave policy |
| GET | `/api/v1/leaves/pending` | Manager view — pending leave requests |
| POST | `/api/v1/leaves/{id}/approve` | Manager approves a leave request |
| POST | `/api/v1/leaves/{id}/reject` | Manager rejects a leave request |

---

## Backend Endpoints — Status Reference

### Exists and Wired
| Path | Notes |
|------|-------|
| `GET /api/v1/time/holidays` | Legacy wrapper, returns `HolidayCalendars` |
| `GET /api/v1/time/leave-balances` | Legacy wrapper, returns all balances for tenant |
| `GET /api/v1/time/timesheets/current` | Legacy wrapper, returns first timesheet (stub) |
| `GET /api/v1/workforce/attendance/sessions/live` | Live checked-in sessions |
| `GET /api/v1/workforce/attendance/dashboard/health` | KPI counts by status |
| `GET /api/v1/workforce/attendance/dashboard/manager` | Manager dashboard aggregates |
| `GET /api/v1/workforce/attendance/records` | All records with filters |
| `GET /api/v1/workforce/attendance/records/my` | Employee's own records |
| `GET /api/v1/workforce/attendance/regularizations/pending` | Pending regularizations |
| `POST /api/v1/leaves/request` | Create leave request |
| `POST /api/v1/leaves/{id}/submit` | Submit a draft request |
| `GET /api/v1/leaves/my` | Employee's own requests |
| `GET /api/v1/leaves/balance/{policyId}` | Balance for a specific policy |
| `GET /api/v1/rosters` | List rosters |
| `GET /api/psa/employees/{employeeId}/projects` | Employee's PSA projects |

### Missing — Phase 2
| Path | Notes |
|------|-------|
| `GET /api/v1/attendance/dlq` | Biometric DLQ triage |
| `GET /api/v1/attendance/fraud` | Fraud flags |
| `POST /api/v1/attendance/reprocess` | Trigger reprocessing |
| `POST /api/v1/time/holidays` | Add holiday |
| Timesheet CRUD beyond current read | submit, approve, reject, pending queue |
