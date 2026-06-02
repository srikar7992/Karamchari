# AUTHENTICATION CERTIFICATION

**Date**: 2026-06-02  
**Program**: Sprint 1 + Sprint 2 Runtime Truth Program — Section 3  
**Evidence**: Live API (localhost:8080), running against SQL Server 2022 container

---

## Test Matrix

| Scenario | Request | Expected | Actual | Result |
|----------|---------|----------|--------|--------|
| Valid login — admin | POST /api/identity/login `admin@dev.local / Dev@Pass123!` | 200 + accessToken | 200 + accessToken | PASS |
| Valid login — manager | POST /api/identity/login `manager@dev.local` | 200 + token | 200 + token | PASS |
| Valid login — employee | POST /api/identity/login `employee@dev.local` | 200 + token | 200 + token | PASS |
| Valid login — multi-tenant (acme) | POST /api/identity/login `admin@acme.local` | 200 + token | 200 + token | PASS |
| Valid login — multi-tenant (contoso) | POST /api/identity/login `admin@contoso.local` | 200 + token | 200 + token | PASS |
| Valid login — multi-tenant (globex) | POST /api/identity/login `admin@globex.local` | 200 + token | 200 + token | PASS |
| Wrong password | POST /api/identity/login wrong password | 401 | 401 | PASS |
| Invalid user (no such email) | POST /api/identity/login ghost@dev.local | 401 | 401 | PASS |
| No token on protected POST | POST /api/v1/recruitment/requisitions — no header | 401 | 401 | PASS |
| Bad JWT token | POST protected — `Authorization: Bearer bad` | 401 | 401 | PASS |
| Tampered JWT token | POST protected — last char flipped | 401 | 401 | PASS |

## JWT Claims Verified

Admin token decoded:
```json
{
  "sub": "67778eb8-df83-42cd-0b13-08debe2ad93e",
  "email": "admin@dev.local",
  "jti": "5562af80-0cb6-434d-8dae-df4e6763c613",
  "tenant_id": "dev",
  "permission_version": "2026.05.30.2",
  "role": "Admin",
  "permission": ["employee.read","employee.write","payroll.read","payroll.run","payroll.approve","leave.read","leave.approve","bulkimport.read","bulkimport.create","bulkimport.execute","bulkimport.cancel", ...],
  "iss": "https://karamchari.com",
  "aud": "https://karamchari.com/api"
}
```

Claims present: `sub`, `email`, `jti`, `tenant_id`, `permission_version`, `role`, `permission`, `iss`, `aud` ✓

## Structured Logging Evidence

Confirmed in container logs:
```
[INF] AUTH_SUCCESS: Email: admin@dev.local, Device: curl/8.7.1 (User: ..., Tenant: dev)
[WRN] AUTH_FAILURE: Email: admin@dev.local, Reason: Invalid credentials (User: N/A, Tenant: N/A)
[WRN] AUTH_FAILURE: Email: ghost@dev.local, Reason: Invalid credentials (User: N/A, Tenant: N/A)
```

---

## Verdict

**CERTIFIED** — All authentication scenarios produce correct HTTP status codes. JWT contains all required claims. Auth events are structured and logged with correct severity levels.
