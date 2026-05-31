# Karamchari Bulk Import Guide

## Overview

The Karamchari Bulk Import platform allows administrators to onboard large datasets safely and repeatably with full tenant isolation.

**Base URL:** `http://localhost:<port>/api/v1/migration/imports`

**Authentication:** Bearer token required. Use `POST /api/v1/identity/login` to get a token.

**Required Permission:** `bulkimport.create` (Admin role has this by default).

---

## Supported Import Types

| Import Type | File | Description |
|---|---|---|
| `EmployeeImport` | `employees.csv` | Onboard employees |
| `LeaveBalanceImport` | `leave-balances.csv` | Set opening leave balances |
| `SalaryComponentImport` | `salary-components.csv` | Import historical salary structure |
| `SalaryRevisionImport` | `salary-revisions.csv` | Import historical salary revisions |
| `HistoricalPayrollImport` | `payroll-history.csv` | Import historical payroll summaries |

---

## Step-by-Step: Upload → Validate → Execute

### Step 1: Upload File

```bash
curl -X POST http://localhost:60463/api/v1/migration/imports \
  -H "Authorization: Bearer <token>" \
  -F "importType=EmployeeImport" \
  -F "file=@sample-data/employees.csv"
```

Response:
```json
{ "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6" }
```

Save the `id` — you'll need it for subsequent steps.

---

### Step 2: Preview (Optional)

Preview the first 10 rows to verify the file was parsed correctly.

```bash
curl http://localhost:60463/api/v1/migration/imports/{id}/preview \
  -H "Authorization: Bearer <token>"
```

---

### Step 3: Validate (Dry Run)

Validates all rows against business rules. Does NOT import any data.

```bash
curl -X POST http://localhost:60463/api/v1/migration/imports/{id}/validate \
  -H "Authorization: Bearer <token>"
```

Response:
```json
{
  "jobId": "...",
  "validRows": 380,
  "invalidRows": 20,
  "errors": [
    { "rowNumber": 5, "errorMessage": "Department 'R&D' not found." }
  ]
}
```

Fix the source file if errors are present, then re-upload.

---

### Step 4: Execute

Triggers the background import worker. Returns immediately — use Status to poll progress.

```bash
curl -X POST http://localhost:60463/api/v1/migration/imports/{id}/execute \
  -H "Authorization: Bearer <token>"
```

---

### Step 5: Monitor Status

```bash
curl http://localhost:60463/api/v1/migration/imports/{id} \
  -H "Authorization: Bearer <token>"
```

`status` field values:
- `Created` — File uploaded
- `Validated` — Dry-run passed
- `ValidationFailed` — Dry-run found errors
- `Queued` — Sent to background worker
- `Processing` — Worker is running
- `Completed` — All rows succeeded
- `CompletedWithErrors` — Some rows failed
- `Failed` — Fatal processing error
- `Cancelled` — Manually cancelled

---

### Step 6: Download Error Report

If the import completed with errors, download the CSV error report:

```bash
curl http://localhost:60463/api/v1/migration/imports/{id}/error-report \
  -H "Authorization: Bearer <token>" \
  --output errors.csv
```

---

### Cancel

Cancel a job that is in `Created`, `Validated`, or `Queued` state.

```bash
curl -X POST http://localhost:60463/api/v1/migration/imports/{id}/cancel \
  -H "Authorization: Bearer <token>"
```

---

## CSV Format Reference

### employees.csv

| Column | Required | Notes |
|---|---|---|
| Employee Number | ✅ | Unique identifier |
| First Name | ✅ | |
| Last Name | | |
| Email | ✅ | Must be unique |
| Department | | Must match existing department name |
| Designation | | |
| Manager Employee Number | | Must be an existing employee number |
| Date Of Joining | | YYYY-MM-DD format |
| Employment Type | | Full-Time, Part-Time, Contract |

### leave-balances.csv

| Column | Required | Notes |
|---|---|---|
| Employee Number | ✅ | Must match an existing employee |
| Leave Type | ✅ | Must match an existing leave policy name |
| Balance | ✅ | Decimal days |
| Effective Date | | YYYY-MM-DD format |

### salary-components.csv

| Column | Required | Notes |
|---|---|---|
| Employee Number | ✅ | Must match an existing employee |
| Component | ✅ | Component name (e.g., Basic, HRA) |
| Amount | ✅ | Monthly amount |
| Frequency | | Monthly (default), Annual |

### salary-revisions.csv

| Column | Required | Notes |
|---|---|---|
| Employee Number | ✅ | Must match an existing employee |
| Previous CTC | | Annual CTC before revision |
| New CTC | ✅ | Annual CTC after revision |
| Effective Date | | YYYY-MM-DD format |
| Revision Type | | AnnualIncrement, Promotion, MarketCorrection, OffCycle |
| Remarks | | Optional notes |

### payroll-history.csv

| Column | Required | Notes |
|---|---|---|
| Employee Number | ✅ | Must match an existing employee |
| Month | ✅ | 1–12 |
| Year | ✅ | Calendar year (e.g., 2024) |
| Gross Pay | | Monthly gross earnings |
| Deductions | | Total deductions |
| Net Pay | ✅ | Take-home amount |

---

## Troubleshooting

**"Department not found"** — Run `GET /api/v1/hr/departments` to see available departments. Add missing ones before import.

**"Employee not found"** — This usually means the referenced manager or other employee hasn't been imported yet. Import employees first, then leave balances, then salary data.

**Import stuck in "Queued"** — Check that the Worker service is running (`docker compose up karamchari-worker`).

**Import stuck in "Processing"** — Check worker logs: `docker compose logs karamchari-worker --tail=50`.

**Getting 401** — Your token may have expired. Re-authenticate via `POST /api/v1/identity/login`.

**Getting 403** — Your account does not have the `bulkimport.execute` permission. Ensure your role is Admin.

---

## Recommended Import Order

When importing data for a new tenant:

1. **Employees** (`EmployeeImport`) — Must come first; all other imports reference employee numbers.
2. **Leave Balances** (`LeaveBalanceImport`) — Requires employees and leave policies to exist.
3. **Salary Components** (`SalaryComponentImport`) — Requires employees.
4. **Salary Revisions** (`SalaryRevisionImport`) — Requires employees.
5. **Historical Payroll** (`HistoricalPayrollImport`) — Requires employees.
