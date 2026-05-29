# ADR-0013 — Reporting & Export Pipeline

**Status:** Accepted  
**Date:** 2026-05-09  
**Author:** Srikar

---

## Context

Enterprise HR platforms require bulk data exports, scheduled reports, and
compliance evidence packages. Naïve synchronous exports on large tenants
cause: timeouts, memory pressure, and database lock contention. Duplicate
export requests (UI retries) compound the problem.

The platform needs a durable async export pipeline that is replay-safe,
memory-safe, tenant-isolated, and rate-limited.

---

## Decision

### Export Job Model

Every export request is persisted as an `ExportJob` aggregate:

```
POST /api/v1/hr/reports/{type}  →  ExportJob { Status=Queued }  →  202 Accepted { jobId }
GET  /api/v1/hr/reports/{jobId}/status
GET  /api/v1/hr/reports/{jobId}/download   (streams blob when Status=Ready)
```

State machine: `Queued → Processing → Ready | Failed | Cancelled`

### Processing Architecture

```
BFF → ExportJob.Create() → SaveChanges (outbox emits ExportJobQueuedIntegrationEvent)
    → MassTransit Consumer → ExportWorkerService.RunAsync()
      → IAsyncEnumerable<T> streaming query
      → ClosedXML / QuestPDF / CsvWriter chunk builder
      → Azure Blob Storage (temp, TTL 24h)
      → ExportJob.MarkReady(blobUri)
      → BFF download endpoint: redirect to blob SAS URL
```

No in-process memory accumulation — streaming pipeline via `IAsyncEnumerable`.

### Idempotency

`ExportJob.IdempotencyKey = TenantId:ReportType:ParameterHash:RequestedByEmployeeId`
with a 60-second dedup window. Duplicate requests within the window return
the existing `jobId`. After 60 seconds a new job is valid (re-run scenario).

### Operational Safety

- Tenant export quota: max 3 concurrent export jobs per tenant (enforced at
  job creation; returns `429` if exceeded).
- Maximum row limit per export: 50,000 rows (configurable per report type).
- Export TTL: blobs deleted after 24 hours; job records kept 90 days for audit.
- Rate limit on download endpoint: 5 downloads per minute per employee (APIM).

### Storage

`ExportJobs` table in `Karamchari.Performance` BC (Performance is the primary
data owner). Phase 2: extract to `Karamchari.Reporting` when the project
justifies a separate deployment unit.

### Supported Report Types (Phase 1)

| Code | Description | Format |
|------|-------------|--------|
| `review-cycle-summary` | All reviews in a cycle | Excel |
| `calibration-distribution` | 9-box distribution by department | Excel |
| `promotion-pipeline` | All promotions in flight | Excel |
| `goal-completion` | Goal completion by employee/dept | Excel |
| `employee-performance-snapshot` | Post-cycle performance summary | Excel |

---

## Alternatives Considered

| Option | Rejected Reason |
|--------|----------------|
| Synchronous export endpoint | Timeout on large tenants; no retry |
| Worker in separate container | Premature — modular monolith is one container |
| Export directly to email | Ungoverneable; no download link, no audit |

---

## Consequences

**Good:**
- No synchronous timeout risk
- Auditable export history
- Tenant-safe blob isolation
- Idempotent retry semantics

**Accepted:**
- Requires Azure Blob Storage (already in stack via Azure.Storage.Blobs)
- 24-hour blob TTL means delayed download windows
- Phase 1 formats Excel-only (PDF export Phase 2)
