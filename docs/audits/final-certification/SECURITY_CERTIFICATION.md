# SECURITY CERTIFICATION (Phase 7)

**Date:** 2026-05-30 · **Method:** live attacks against running API `3d47a2d7…`. Repro: `scripts/runtime/security-probe.sh`.

| Attack / control | Expected | Observed | Verdict |
|---|---|---|---|
| Unauthenticated access to protected endpoint | 401 | **401** | ✅ VERIFIED |
| **`alg=none` token forgery** (algorithm confusion) | 401 | **401** | ✅ VERIFIED |
| Garbage/tampered token | 401 | **401** | ✅ VERIFIED |
| Valid `dev` JWT → own tenant data | 200 | **200** | ✅ VERIFIED |
| **Cross-tenant object read** (`dev` JWT reads `acme` employee by id — IDOR) | 404/empty | **404** | ✅ VERIFIED |
| **Async cross-tenant write** (D1) | own schema only | own schema only | ✅ VERIFIED (see ASYNC_CERTIFICATION) |
| Auth rate limiting (25 rapid logins) | throttle | **17×429** | ✅ VERIFIED |
| Security headers | present | `X-Content-Type-Options:nosniff`, `X-Frame-Options:DENY`, `Referrer-Policy:no-referrer`, `CSP: default-src 'self'; frame-ancestors 'none'; object-src 'none'; base-uri 'self'` | ✅ VERIFIED |
| RLS fail-closed at DB | no rows w/o tenant context | sa SELECT on `tenant_*` returned **0 rows** without `SESSION_CONTEXT('TenantId')`; **1,680** RLS predicates / 3 policies live | ✅ VERIFIED |
| JWT prod fail-fast guard | refuse placeholder secret in prod | guard in `Program.cs` (verified prior: Production+placeholder → exit, 0 listeners) | ✅ VERIFIED (prior) |

## Defense-in-depth confirmed
Tenant isolation is enforced at **three** independent layers, all observed live:
1. **Auth** — JWT tenant claim authoritative; forged/none-alg tokens rejected (401).
2. **Application** — cross-tenant object read returns 404 (object-level authorization).
3. **Database** — RLS (1,680 predicates) fail-closed; rows invisible without session tenant context.
4. **Messaging** — signed `MT-Tenant-Id` + HMAC; consumer rejects unsigned/missing-tenant messages (Enforce mode).

## NOT VERIFIED / notes
- **HSTS (`Strict-Transport-Security`)** absent — expected on plaintext-HTTP local; **must be enabled
  behind TLS in production** (recommendation, not a local defect).
- **Refresh-token rotation, RBAC privilege-escalation, signature-tampering live rejection**: code paths
  exist and are unit-tested; not individually exercised live this pass → NOT VERIFIED (lower priority).
- Secret scanning is in CI (`trufflehog --only-verified`).

## Verdict: **Phase 7 Security — VERIFIED (no security-critical defect found).**
All gate-relevant controls (authn, algorithm-confusion, cross-tenant read/write, RLS, rate limiting,
headers) pass live. Production TLS/HSTS and full RBAC matrix are follow-ups, not blockers.
