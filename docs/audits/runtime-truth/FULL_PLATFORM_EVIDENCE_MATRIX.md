# FULL PLATFORM EVIDENCE MATRIX

**Date**: 2026-06-02  
**Program**: Sprint 1 + Sprint 2 Runtime Truth Program — Section 22  
**Infrastructure**: SQL Server 2022, RabbitMQ 3, Redis 7, OTEL Collector, Seq (structured logs)

---

## Infrastructure Stack Verified Running

| Service | Image | Port | Status |
|---------|-------|------|--------|
| karamchari.api | karamchari-api:local | 8080 | healthy |
| karamchari.worker | karamchari-worker:local | — | running |
| sqlserver | mssql/server:2022-latest | 1433 | running |
| rabbitmq | rabbitmq:3-management-alpine | 5672/15672 | running |
| redis | redis:7-alpine | 6379 | running |
| seq | datalust/seq:latest | 8081 | running |
| otel-collector | opentelemetry-collector-contrib | 4317-4318 | running |
| prometheus | prom/prometheus | 9090 | running |
| grafana | grafana/grafana | 3000 | running |
| azurite | azure-storage/azurite | 10000-10002 | running |
| mailpit | axllent/mailpit | 1025/8025 | running |

## Full Recruitment Journey (Live Execution)

### Tenant: dev

**Step 1 — CreateRequisition**
```
POST /api/v1/recruitment/requisitions
Authorization: Bearer {admin_token}
Body: {"title":"Staff Engineer","departmentId":"...","hiringManagerId":"..."}
→ 201 Created {"id":"13b801b0-d616-409a-ac1c-4d2ec5d91435"}
```

**Step 2 — PublishRequisition**
```
POST /api/v1/recruitment/requisitions/13b801b0.../publish
→ 204 No Content
```

**Step 3 — CreateCandidate**
```
POST /api/v1/recruitment/candidates
Body: {"firstName":"Bob","lastName":"Smith","email":"bob.smith@example.com"}
→ 201 Created {"id":"a8210c48-2970-4ec3-8353-4527dbc3b547"}
```

**Step 4 — ApplyCandidate**
```
POST /api/v1/recruitment/applications
Body: {"candidateId":"a8210c48...","requisitionId":"13b801b0..."}
→ 201 Created {"id":"5cf49748-2138-4ebd-9a35-b3491d47f8e5"}
```

**Step 5 — AdvanceToScreening**
```
POST /api/v1/recruitment/applications/5cf49748.../advance
Body: {"targetStatus":"Screening"}
→ 204 No Content
```

**Step 6 — AdvanceToInterviewing**
```
POST /api/v1/recruitment/applications/5cf49748.../advance
Body: {"targetStatus":"Interviewing"}
→ 204 No Content
```

**Step 7 — ScheduleInterview**
```
POST /api/v1/recruitment/interviews
Body: {"applicationId":"5cf49748...","scheduledAt":"2026-06-09T12:00:00Z","interviewerIds":["..."]}
→ 201 Created {"id":"c23fe993-44ba-49cb-bf32-b1ed49d7c1ce"}
```

**Step 8 — CreateOffer**
```
POST /api/v1/recruitment/offers
Body: {"applicationId":"5cf49748...","baseSalary":95000,"currency":"USD"}
→ 201 Created {"id":"524329eb-6e9e-420c-bc52-03d50bf92557"}
```

**Step 9 — ApproveOffer**
```
POST /api/v1/recruitment/offers/524329eb.../approve
→ 204 No Content
```

**Step 10 — IssueOffer**
```
POST /api/v1/recruitment/offers/524329eb.../issue
Body: {"expiresAt":"2026-07-01T00:00:00Z"}
→ 204 No Content
```

**Step 11 — AcceptOffer**
```
POST /api/v1/recruitment/offers/524329eb.../accept
→ 204 No Content
```

**Step 12 — HireCandidate**
```
POST /api/v1/recruitment/applications/5cf49748.../hire
Body: {"hiredBy":"admin@dev.local"}
→ 204 No Content
```

**Complete pipeline: 12/12 steps succeeded.**

## Multi-Tenant Evidence

| Operation | Dev Tenant | ACME Tenant | Isolation |
|-----------|-----------|-------------|-----------|
| Create candidate same email | id=8d37b90d | id=5c87618f | ISOLATED |
| Cross-read with wrong token | — | 404 | BLOCKED |
| Auth logging | AUTH_SUCCESS tenant=dev | AUTH_SUCCESS tenant=acme | CORRECT |

## Structured Log Evidence (Seq)

Auth events captured in structured logs:
```
AUTH_SUCCESS Email=admin@dev.local Tenant=dev User=67778eb8
AUTH_FAILURE Email=admin@dev.local Reason=Invalid credentials
AUTH_SUCCESS Email=admin@acme.local Tenant=acme User=82252b85
```

---

## Verdict

**CERTIFIED** — Complete 12-step recruitment journey executed against live infrastructure. All API calls return correct status codes. Multi-tenant isolation proven. Structured logging confirmed for auth events.
