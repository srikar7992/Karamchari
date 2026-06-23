# Attendance Phase 1 Report

**Branch:** `feat/attendance-phase1`
**Date:** 2026-06-23

---

## Summary

Attendance is the second module wired for live backend integration after Recruitment.
All Phase 1 work targets the endpoint drift, missing BFF routes, and raw-fetch usage
identified in the pre-phase audit.

---

## Root Cause of Broken Hooks (Pre-Phase State)

Every hook in `use-time.ts` and `use-timesheets.ts` called `fetch('/api/time/*')` — a
relative URL that hits Next.js, where no API route files exist. These were always 404.
`attendanceApi.ts` used `${BASE_URL}/api/attendance/*`, a path that does not match the
backend's `/api/v1/workforce/attendance/*` mount point.

---

## Changes Made

### Documentation (Task 1)

| File | Purpose |
|------|---------|
| `docs/attendance/ATTENDANCE_SCREEN_DATA_MAP.md` | Maps 7 screens to their data requirements and backend sources |
| `docs/attendance/ATTENDANCE_ENDPOINT_MAP.md` | Drift table — old vs new path for every hook call |

### Frontend: `use-time.ts` (Task 2)

- All hooks rewritten to use `api.get/post` from `@/lib/api/client`
- Correct paths: `/api/v1/time/holidays`, `/api/v1/leaves/policies`, `/api/v1/leaves/my`, etc.
- Added `useMyLeaveRequests` (missing hook)
- Added `useAddLeavePolicy`, `usePendingLeaveRequests`, `useApproveLeaveRequest`, `useRejectLeaveRequest`
- Removed `useCalculateLeave` — no backend dry-run endpoint; compute client-side if needed
- Removed `useAddHoliday` — no POST holiday endpoint yet (deferred Phase 2)

### Frontend: `use-timesheets.ts` (Task 3)

- `useCurrentTimesheet` → `api.get('/api/v1/time/timesheets/current')`
- `useSubmitTimesheet` → `api.post('/api/v1/time/timesheets')`
- Phase 2 stubs (`enabled: false`) for `usePendingTimesheets`, `useApproveTimesheet`, `useRejectTimesheet`

### Frontend: `attendanceApi.ts` (Task 4)

- `fetchLiveAttendance` → `/api/v1/workforce/attendance/sessions/live`
- `fetchAttendanceKpis` → `/api/v1/workforce/attendance/dashboard/health`
  - Inline mapper converts status-grouped array `[{status, count}]` to flat `{total, present, late, absent}` shape
- DLQ/Fraud/Reprocess functions retained (compile), paths remain Phase 2 stubs

### Frontend: `timesheets/page.tsx` (Task 6)

- Projects fetch: `fetch('/api/psa/employees/{id}/projects')` → `api.get('/api/psa/employees/{id}/projects')`
- Timesheet submit: `fetch('/api/time/timesheets', POST)` → `api.post('/api/v1/time/timesheets')`

### Backend: `LeaveEndpoints.cs` (Task 5)

Five new routes added to the `/api/v1/leaves` group:

| Method | Path | Handler |
|--------|------|---------|
| GET | `/api/v1/leaves/policies` | `GetLeavePolicies` — returns active policies |
| POST | `/api/v1/leaves/policies` | `CreateLeavePolicy` — calls `LeavePolicy.Create()` |
| GET | `/api/v1/leaves/pending` | `GetPendingLeaves` — Submitted + PendingApproval requests |
| POST | `/api/v1/leaves/{id}/approve` | `ApproveLeaveRequest` — calls `leave.FinalizeApproved()` |
| POST | `/api/v1/leaves/{id}/reject` | `RejectLeaveRequest` — calls `leave.FinalizeRejected(reason)` |

New records: `CreateLeavePolicyRequest`, `RejectLeaveRequest`, `LeavePolicyResponse`.
Build verified clean.

---

## Deferred to Phase 2

| Item | Reason |
|------|--------|
| `POST /api/v1/time/holidays` | No BFF route; internal admin concern |
| Timesheet approve/reject endpoints | No BFF route; manager timesheet flow not spec'd |
| DLQ, Fraud, Reprocess endpoints | Internal services — no HTTP surface yet |
| Client-side leave day calculation | No backend dry-run endpoint |

---

## Endpoint Inventory (Phase 1 Verified Paths)

| Frontend Call | Backend Path | Status |
|---------------|-------------|--------|
| `useHolidays` | `GET /api/v1/time/holidays` | Exists |
| `useLeavePolicies` | `GET /api/v1/leaves/policies` | Added Phase 1 |
| `useAddLeavePolicy` | `POST /api/v1/leaves/policies` | Added Phase 1 |
| `useLeaveBalances` | `GET /api/v1/time/leave-balances` | Exists |
| `useMyLeaveRequests` | `GET /api/v1/leaves/my` | Exists |
| `useSubmitLeaveRequest` | `POST /api/v1/leaves/request` + `/{id}/submit` | Exists |
| `usePendingLeaveRequests` | `GET /api/v1/leaves/pending` | Added Phase 1 |
| `useApproveLeaveRequest` | `POST /api/v1/leaves/{id}/approve` | Added Phase 1 |
| `useRejectLeaveRequest` | `POST /api/v1/leaves/{id}/reject` | Added Phase 1 |
| `useCurrentTimesheet` | `GET /api/v1/time/timesheets/current` | Exists |
| `useSubmitTimesheet` | `POST /api/v1/time/timesheets` | Exists |
| `fetchLiveAttendance` | `GET /api/v1/workforce/attendance/sessions/live` | Exists |
| `fetchAttendanceKpis` | `GET /api/v1/workforce/attendance/dashboard/health` | Exists (mapped) |
| Projects (timesheets page) | `GET /api/psa/employees/{id}/projects` | Exists |
| Timesheet submit (page) | `POST /api/v1/time/timesheets` | Exists |
