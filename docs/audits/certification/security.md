# Phase 14 — Security Certification

**Result: ❌ FAIL (multiple HIGH/CRITICAL findings).** Strong tenant data-isolation foundation undermined by authentication and hardening defects.

## Probe results
| Probe | Expected | Actual | Verdict |
|---|---|---|---|
| JWT validation (valid token) | accept | Could not mint a token (auth broken) | ⛔ |
| Unauthorized request | 401 | **302 → /Account/Login**, no `WWW-Authenticate` | ❌ |
| Invalid token | 401 | **302 → /Account/Login** | ❌ |
| Tenant spoofing (`X-Tenant-Id: contoso`) | 401/403 | 302 (auth intercepts first) — not positively verifiable | ⚠️ |
| Header tampering | rejected | 302 (auth intercepts) | ⚠️ |
| SQL injection (`dev'; DROP TABLE…`) | safe | 302 (never reached DB); EF is parameterized + RLS | ⚠️ (not reachable to prove) |
| Mass assignment | bound DTO | `OnboardEmployeeCommand`/`UpdateEmployeeCommand` are constrained DTOs, not entities | ✅ (design) |
| Broken object authorization | enforced | Not testable (auth) | ⛔ |
| Rate limiting | 429 under burst | 60 rapid `/health` → all 200, no 429 | ⚠️ not observed |

## Findings
- **CRITICAL — Auth unusable** (Phase 4): no identity tables/migrations; register/login 500.
- **HIGH — Wrong challenge scheme:** protected API returns 302 redirect to a cookie-login page instead of 401.
- **HIGH — `BuildServiceProvider()` per token validation** (`IdentityServiceCollectionExtensions`): new DI container built on every JWT validation → memory/DoS risk.
- **HIGH — Missing security headers:** no `X-Content-Type-Options`, `X-Frame-Options`, `Strict-Transport-Security`, `Content-Security-Policy`, `Referrer-Policy` on responses.
- **HIGH — Verbose error leakage in `Local` env:** `UseDeveloperExceptionPage()` is enabled for **both `Development` and `Local`** (`Program.cs`). Responses leak full stack traces **and request headers** (`exception.details`, `headers`). `Local` is a deployable-sounding profile; ensure it is never internet-facing.
- **MEDIUM — Prod error detail:** `GlobalExceptionHandler` sets `Detail = exception.Message`, surfacing raw internal messages (incl. SQL text) to clients in non-dev envs.
- **MEDIUM — Rate limiter not observably enforcing** on tested path; verify policies/limits.

## Strengths
- Row-Level Security (1,680 predicates, schema-bound, enabled) — robust DB-layer isolation.
- `TenantCacheGuard` blocks cross-tenant cache access/poisoning.
- Parameterized EF Core; constrained command DTOs (mass-assignment resistant).
- Tenant resolution supports trusted-gateway fingerprint + subdomain agreement.

## Verdict: ❌ FAIL. Fix auth + challenge scheme + security headers + error hygiene before exposure. Then re-run positive-path security probes (spoofing, BOLA, injection) with real tokens.
