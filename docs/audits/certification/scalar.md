# Phase 3 — Scalar / OpenAPI Certification

**Result: ✅ PASS**

## Evidence
| Check | Result | Detail |
|---|---|---|
| `/` redirect | ✅ | `GET /` → `302` → `http://localhost:60463/scalar` |
| Scalar UI | ✅ | `GET /scalar/` → `200`, `<title>Karamchari API Explorer</title>` |
| OpenAPI generation | ✅ | `GET /openapi/v1.json` → `200`, 132,943 bytes |
| JWT support in doc | ✅ | `components.securitySchemes.Bearer` = `http`/`bearer`/`JWT`; global security requirement applied (Program.cs document transformer) |
| Endpoint discovery | ✅ | **167 routes** across HR, Payroll, PSA, Billing, Identity, Capability, Workflow, Analytics, Compliance, ESS, Forecast, Tenants |
| Request execution | ⚠️ | UI renders & issues requests; authenticated calls cannot complete end-to-end because auth is broken (see `authentication.md`). Unauthenticated/health calls execute correctly. |

Saved doc: `docs/certification/evidence/openapi.json`.

### Sample of discovered routes
```
POST /api/identity/register | POST /api/identity/login | POST /api/identity/refresh | POST /api/identity/logout
POST /api/tenants
POST /api/v1/hr/employees | GET /api/v1/hr/employees | GET|PUT|DELETE /api/v1/hr/employees/{id}
GET  /api/v1/hr/employees/{id}/history | POST /api/v1/hr/employees/{id}/transfer
POST /api/payroll/runs | PUT /api/payroll/runs/{id}/lock | GET /api/payroll/runs/{id}/summary
POST /api/billing/invoices/generate | POST /api/billing/invoices/{id}/finalize
GET  /api/forecast/summary | GET /api/analytics/cashflow/forecast
POST /api/v1/approvals/{stepInstanceId}/approve  ... (167 total)
```

## Verdict
OpenAPI generation, JWT scheme advertisement, route discovery, and Scalar UI all **PASS**. Live authenticated execution is gated by the auth defect documented in Phase 4.
