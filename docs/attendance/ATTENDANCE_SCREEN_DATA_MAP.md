# Attendance Screen Data Map

Phase 1A deliverable. Maps each portal screen to its data requirements and the backend source.

---

## Screens

### 1. Attendance Command Center (Admin)
**Route:** `/admin/attendance`
**File:** `src/Frontend/portal/src/app/admin/attendance/page.tsx`

| Component | Data | Backend Source |
|-----------|------|---------------|
| KpiRibbon | total, present, late, absent counts | `GET /api/v1/workforce/attendance/dashboard/health` |
| AttendanceGrid | Live employee presence map | `GET /api/v1/workforce/attendance/sessions/live` |
| DlqPanel | Unmatched biometric punches | `GET /api/v1/attendance/punch` (DLQ sub-resource — **Phase 2**) |
| FraudPanel | Fraud flag records | Not yet exposed as HTTP endpoint — **Phase 2** |
| ReprocessPanel | Reprocess job status | Not yet exposed as HTTP endpoint — **Phase 2** |

**Notes:**
- This screen uses SignalR (`AttendanceHubClient`) for real-time updates patched into React Query cache.
- DLQ, Fraud, and Reprocess HTTP endpoints do not exist in the BFF yet. These are internal services.
  Wire them in Phase 2 after adding the backend endpoints.

---

### 2. Leave Self-Service (Employee)
**Route:** `/leave`
**File:** `src/Frontend/portal/src/app/leave/page.tsx`

| Component | Data | Backend Source |
|-----------|------|---------------|
| Balance KPI cards | Leave balance per policy | `GET /api/v1/time/leave-balances` (all) or `GET /api/v1/leaves/balance/{policyId}` (per policy) |
| Policy selector in modal | Available leave policies | `GET /api/v1/leaves/policies` (**added in Phase 1**) |
| Request history table | Employee's leave requests | `GET /api/v1/leaves/my` |
| Submit modal | Create + submit leave request | `POST /api/v1/leaves/request` then `POST /api/v1/leaves/{id}/submit` |
| Day calculation | Dry-run calculate actual days | No backend endpoint — compute client-side (business days minus holidays) |

---

### 3. Leave Approval (Manager)
**Route:** `/approvals/leave`
**File:** `src/Frontend/portal/src/app/approvals/leave/page.tsx`

| Component | Data | Backend Source |
|-----------|------|---------------|
| Approval inbox | Pending leave requests | `GET /api/v1/leaves/pending` (**added in Phase 1**) |
| Policy name display | Leave policy details | `GET /api/v1/leaves/policies` (**added in Phase 1**) |
| Approve action | Approve a leave request | `POST /api/v1/leaves/{id}/approve` (**added in Phase 1**) |
| Reject action | Reject a leave request | `POST /api/v1/leaves/{id}/reject` (**added in Phase 1**) |

---

### 4. Timesheets (Employee)
**Route:** `/timesheets`
**File:** `src/Frontend/portal/src/app/timesheets/page.tsx`

| Component | Data | Backend Source |
|-----------|------|---------------|
| Project selector | Employee's assigned projects | `GET /api/psa/employees/{employeeId}/projects` |
| Timesheet grid | Current week timesheet | `GET /api/v1/time/timesheets/current` |
| Submit button | Submit timesheet | No full CRUD endpoint — basic wrapper only (**Phase 2**) |

**Notes:**
- Backend has a minimal legacy wrapper at `GET /api/v1/time/timesheets/current`.
- Full timesheet CRUD (submit, approve, reject, pending queue) is not yet implemented in the BFF.
- For Phase 1: wire project fetch via `api` client; leave submit as direct fetch until endpoints exist.

---

### 5. Timesheet Approvals (Manager)
**Route:** `/approvals/timesheets`
**File:** `src/Frontend/portal/src/app/approvals/timesheets/page.tsx`

| Component | Data | Backend Source |
|-----------|------|---------------|
| Pending timesheets | Timesheets awaiting approval | Not yet implemented — **Phase 2** |

---

### 6. Holiday Calendar (Settings)
**Route:** `/settings/holidays`
**File:** `src/Frontend/portal/src/app/settings/holidays/page.tsx`

| Component | Data | Backend Source |
|-----------|------|---------------|
| Holiday list | All tenant holidays | `GET /api/v1/time/holidays` |
| Add holiday | Create new holiday | No POST endpoint in BFF yet — **Phase 2** |

---

### 7. Leave Policies (Settings)
**Route:** `/settings/leave-policies`
**File:** `src/Frontend/portal/src/app/settings/leave-policies/page.tsx`

| Component | Data | Backend Source |
|-----------|------|---------------|
| Policy grid | All tenant leave policies | `GET /api/v1/leaves/policies` (**added in Phase 1**) |
| Policy builder drawer | Create new leave policy | `POST /api/v1/leaves/policies` (**added in Phase 1**) |
