# Security Certification
**Date:** 2026-06-01  
**Status:** CONDITIONALLY CERTIFIED

---

## OWASP Top 10 Assessment

| Risk | Mitigation | Status |
|---|---|---|
| A01 Broken Access Control | RBAC (permissions per role), ITenantOwned EF filter, RLS | IMPLEMENTED |
| A02 Cryptographic Failures | JWT signed with HmacSha256, refresh tokens hashed (SHA-256) | IMPLEMENTED |
| A03 Injection | EF Core parameterized queries throughout, no raw SQL in endpoints | IMPLEMENTED |
| A04 Insecure Design | Domain invariants enforced in aggregate, not HTTP layer | IMPLEMENTED |
| A05 Security Misconfiguration | SecurityHeadersMiddleware (CSP, CORS headers), Scalar relaxation scoped | IMPLEMENTED |
| A06 Vulnerable Components | NuGetAudit=moderate in Directory.Build.props | IMPLEMENTED |
| A07 Auth Failures | Rate limiting ("auth" policy), token blacklist, refresh token rotation | IMPLEMENTED |
| A08 Integrity Failures | OutboxDeadLetter for poison messages, invoice SnapshotJson immutable | IMPLEMENTED |
| A09 Logging Failures | Serilog + OpenTelemetry, PersistentSecurityAuditService | IMPLEMENTED |
| A10 SSRF | No outbound HTTP to user-controlled URLs | N/A |

## Specific Security Controls

| Control | Detail |
|---|---|
| Tenant escalation prevention | tenant_id claim from JWT (not request body), enforced server-side |
| Permission escalation prevention | Permissions embedded in JWT claims, verified server-side |
| Mass assignment prevention | DTOs mapped explicitly, no direct entity binding from HTTP body |
| Token replay detection | Refresh token family revocation on compromised token reuse |
| Token revocation | JTI-based blacklist with 2-tier cache (MemoryCache + DB) |
| Signing key rotation | DatabaseSigningKeyResolver — rotate without service restart |

## Test Evidence

- `PermissionResolverTests` — all role boundaries tested
- `TenantIsolationCertification` — 560 multi-tenant isolation tests
- `Authorization_Manager_ShouldBeDenied_Hiring` — cross-role escalation denied

## Runtime Blockers

- Penetration test for actual token tampering (needs running API)
- Mass assignment fuzzing (needs running API)
- SQL injection attempts (needs running API)

## Certification Decision

**CONDITIONALLY CERTIFIED** — all controls implemented. Runtime exploit verification needs live stack.
