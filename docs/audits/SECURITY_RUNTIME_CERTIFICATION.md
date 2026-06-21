# Security Runtime Certification — Phase D (Extended)

**Date:** _FILL IN_
**Tester:** _FILL IN_
**Environment:** `docker compose up` local dev
**API build:** _FILL IN (docker image SHA)_
**Scripts:**
- `scripts/security/certify-security-extended.sh` — D1, D4, D5, D6
- `scripts/runtime/security-probe.sh` — D2 (alg=none), D3 (cross-tenant), D7 (headers), D8 (rate-limit)

---

## Previously Certified (Phase 7 / security-probe.sh)

| ID | Check | Method | Result |
|----|-------|--------|--------|
| D2 | `alg=none` forged JWT → 401 | `security-probe.sh` check 2 | PASS |
| D3 | Cross-tenant employee read → 404 | `security-probe.sh` check 4 | PASS |
| D7 | Security response headers present | `security-probe.sh` check 5 | PASS |
| D8 | Rate-limit on `/api/identity/login` → 429 | `security-probe.sh` check 6 | PASS |

Reference: `docs/audits/final-certification/SECURITY_CERTIFICATION.md`

---

## D1 — JWT Tampering

**Script:** `certify-security-extended.sh` → `### D1`
**Principle:** Any JWT with a modified claim and/or wrong signature must be rejected (401).

| Check | Payload modification | Expected | Actual | Result |
|-------|---------------------|----------|--------|--------|
| D1a | `alg=none`, tenant_id=acme (no sig) | 401 | _FILL_ | PASS/FAIL |
| D1b | Real header, payload tenant_id altered to 'acme', wrong sig | 401 | _FILL_ | PASS/FAIL |
| D1c | Real header, roles escalated to SuperAdmin, wrong sig | 401 | _FILL_ | PASS/FAIL |
| D1d | Sanity: real unmodified token | 200 | _FILL_ | PASS/FAIL |

**Notes:** _FILL IN — e.g. exact error body returned, timing of rejection_

**D1 Verdict:** `PASS / FAIL`

---

## D4 — Mass Assignment

**Script:** `certify-security-extended.sh` → `### D4`
**Endpoint:** `POST /api/v1/recruitment/requisitions`
**Principle:** Extra fields in the request body (`TenantId`, `IsAdmin`, `CreatedBy`, `Permissions`, `Role`) must be silently ignored and must NOT appear in the response or be persisted.

| Check | Extra fields sent | Expected behaviour | Actual HTTP | isAdmin reflected? | tenantId reflected? | Result |
|-------|-------------------|--------------------|-------------|-------------------|--------------------|----|
| D4a | TenantId=acme, IsAdmin=true, CreatedBy=attacker, Permissions=[admin.all] | 201 or 400; fields not reflected | _FILL_ | _FILL_ | _FILL_ | PASS/FAIL |
| D4b | No 500 on extra fields | No server error | _FILL_ | — | — | PASS/FAIL |

**Notes:** _FILL IN — if 400 returned, paste validation message; if 201, paste response body excerpt_

**D4 Verdict:** `PASS / FAIL`

---

## D5 — Privilege Escalation

**Script:** `certify-security-extended.sh` → `### D5`
**Test user:** `manager@dev.local` (role: Manager)
**Manager permissions (from PermissionResolver):** employee.read/write, payroll.read/run, leave.read/approve, reimbursement.read/approve, capability.read/write, billing.read/write, audit.read, bulkimport.read
**Missing permissions:** recruitment.hire, recruitment.offer, recruitment.create, employee.delete, tenant.settings.*

| Check | Endpoint | Manager permission? | Expected | Actual | Result |
|-------|----------|--------------------|----|---------|--------|
| D5a | `POST /api/v1/recruitment/applications/{id}/hire` | No (recruitment.hire missing) | 403 | _FILL_ | PASS/FAIL |
| D5b | `GET /api/v1/billing/ar/summary` | Yes (billing.read) | 200 | _FILL_ | PASS/FAIL |
| D5c | `DELETE /api/v1/hr/employees/{id}` | No (employee.delete missing) | 403 | _FILL_ | PASS/FAIL |
| D5d | `POST /api/v1/recruitment/offers/{id}/approve` | No (recruitment.offer missing) | 403 | _FILL_ | PASS/FAIL |

**Notes:** _FILL IN — note if 404 returned instead of 403 (resource not found vs. forbidden)_

**D5 Verdict:** `PASS / FAIL`

---

## D6 — Injection

**Script:** `certify-security-extended.sh` → `### D6`
**Principle:** SQL injection / JSON injection in query params or bodies must NOT cause 500 errors, data dumps, or SQL error details in responses.

| Check | Payload | Endpoint | Expected | Actual | Result |
|-------|---------|----------|----------|--------|--------|
| D6a | `' OR 1=1 --` in ?search= | `GET /api/v1/hr/employees/` | 200/400/404 (not 500) | _FILL_ | PASS/FAIL |
| D6b | `1; DROP TABLE employees --` in ?search= | `GET /api/v1/hr/employees/` | 200/400/404 (not 500) | _FILL_ | PASS/FAIL |
| D6c | `' UNION SELECT * FROM sys.objects --` in ?search= | `GET /api/v1/hr/employees/` | 200/400/404 (not 500) | _FILL_ | PASS/FAIL |
| D6d | JSON injection in POST body (SQL in title field) | `POST /api/v1/recruitment/requisitions` | not 500 | _FILL_ | PASS/FAIL |
| D6e | Path traversal `..%2F..%2Fadmin` in ID | `GET /api/v1/hr/employees/` | not 500 | _FILL_ | PASS/FAIL |

**Notes:** _FILL IN — any interesting response bodies; confirm no stack traces leak_

**D6 Verdict:** `PASS / FAIL`

---

## Full Run Output

```
# Paste output of: bash scripts/security/certify-security-extended.sh
# (replace this block with actual terminal output)
FILL IN
```

---

## Certification sign-off

| Phase | Checks | Pass | Fail | Verdict | Tester | Date |
|-------|--------|------|------|---------|--------|------|
| D2 (alg=none) | 1 | 1 | 0 | PASS | — | 2026-05-30 |
| D3 (cross-tenant) | 1 | 1 | 0 | PASS | — | 2026-05-30 |
| D7 (headers) | 1 | 1 | 0 | PASS | — | 2026-05-30 |
| D8 (rate-limit) | 1 | 1 | 0 | PASS | — | 2026-05-30 |
| D1 (JWT tampering) | 4 | _FILL_ | _FILL_ | PASS/FAIL | | |
| D4 (mass assignment) | 2 | _FILL_ | _FILL_ | PASS/FAIL | | |
| D5 (privilege escalation) | 4 | _FILL_ | _FILL_ | PASS/FAIL | | |
| D6 (injection) | 5 | _FILL_ | _FILL_ | PASS/FAIL | | |

**Overall Phase D Verdict:** `PASS / FAIL`
