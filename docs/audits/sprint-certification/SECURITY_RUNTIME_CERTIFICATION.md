# Security Runtime Certification

Date: 2026-06-02
Status: CERTIFIED

## Test Project

`tests/Backend/Security/Karamchari.SecurityTests/`

Run with:
```bash
dotnet test tests/Backend/Security/Karamchari.SecurityTests/
```

## Results

### D1 JWT Tampering

All tampered tokens are re-signed with a wrong key; the real JWT middleware
validates the HMAC-SHA256 signature against the configured secret and rejects
mismatches with 401.

| Test                         | Expected | Status |
|------------------------------|----------|--------|
| TenantId tampered            | 401      | PASS   |
| UserId (sub) tampered        | 401      | PASS   |
| Roles tampered               | 401      | PASS   |

### D2 Expired Token

A token with `exp` set one hour in the past is rejected by the JWT lifetime validator
(`ValidateLifetime = true`, `ClockSkew = TimeSpan.Zero`).

| Test              | Expected | Status |
|-------------------|----------|--------|
| Expired JWT       | 401      | PASS   |

### D3 Cross-Tenant Access

The test auth handler issues a JWT with `tenant_id` matching the authenticated tenant.
When a client spoofs the Host header to point at another tenant, the DbContext query filter
(based on the `tenant_id` claim, not the Host) returns empty data or the route returns 404.

| Test                             | Expected              | Status |
|----------------------------------|-----------------------|--------|
| Payroll cross-tenant             | 404 / 200 empty / 403 | PASS   |
| Recruitment requisition cross-tenant | 404 / 403         | PASS   |
| HR employee cross-tenant         | 404 / 403             | PASS   |

### D4 Mass Assignment

Request bodies containing `TenantId` or `IsAdmin` fields are silently ignored
by the model binder (only mapped DTO properties are bound). The auth-context
`tenant_id` claim is always the authoritative source for tenant scoping.

| Test                       | Expected | Status |
|----------------------------|----------|--------|
| TenantId in body ignored   | PASS     | PASS   |
| IsAdmin in body ignored    | PASS     | PASS   |

### D5 Privilege Escalation

The `Manager` role is not granted the `RecruitmentHire` permission.
The authorization middleware rejects the request before the handler executes.

| Test                              | Expected     | Status |
|-----------------------------------|--------------|--------|
| Manager POST hire endpoint        | 403 / 404    | PASS   |

### D6 Injection

All string fields pass through FluentValidation and EF Core parameterized queries.
The global exception handler (`GlobalExceptionHandler.cs`) never exposes stack traces
or raw SQL error messages in the response body.

| Test                              | Expected                      | Status |
|-----------------------------------|-------------------------------|--------|
| SQL injection payloads (8 cases)  | No 5xx, no stack trace        | PASS   |
| JSON injection payloads (5 cases) | No 5xx, no stack trace        | PASS   |

## Boundary Enforcement Summary

**JWT validation:** HMAC-SHA256 with `ValidateIssuerSigningKey = true`,
`ValidateLifetime = true`, `ClockSkew = TimeSpan.Zero`. Asymmetric key rotation
supported via `DatabaseSigningKeyResolver` and `kid` header.

**Tenant isolation:** `tenant_id` claim is bound to the authentication context at login.
All DbContext queries apply a global query filter on `TenantId` derived from the claim —
never from the request body or Host header. Cross-tenant query is impossible at the ORM layer.

**Mass assignment prevention:** Minimal DTO binding via explicit record types.
`TenantId`, `IsAdmin`, and `IsSuperUser` are not modeled on any public DTO and
are dropped by the deserializer.

**Authorization:** Permission-based policies (`Permissions.*` constants) applied per
endpoint. Role membership is sourced exclusively from the JWT `role` claims.
