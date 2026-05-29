# Phase 4 — Authentication Certification

**Result: ❌ FAIL — CRITICAL.** Authentication is non-functional in a freshly provisioned environment.

## Summary of attempts
| Flow | Expected | Actual | Result |
|---|---|---|---|
| Register | 200/201 + user row | **HTTP 500** `Invalid object name 'identity.AspNetUsers'` | ❌ |
| Login | 200 + access/refresh token | **HTTP 500** `Invalid object name 'identity.AspNetUsers'` | ❌ |
| Refresh | new token | Blocked — depends on login | ❌ |
| Logout | revoke | Blocked — depends on login | ❌ |
| Expired token | 401 | Could not mint a token (see below) | ⛔ Blocked |
| Invalid token | 401 | **302 → /Account/Login** (not 401) | ❌ |
| Missing token | 401 | **302 → /Account/Login** (not 401) | ❌ |
| Tenant / Cross-tenant token | enforced | Blocked — no tokens obtainable | ⛔ Blocked |

## Root cause (CRITICAL #1) — Identity schema never created
- `Program.cs --provision-dev-tenants` migrates **16 DbContexts** but the list **does not include `IdentityDbContext`**.
- `IdentityDbContext` (`Karamchari.Identity.Infrastructure/Persistence/IdentityDbContext.cs`) uses `HasDefaultSchema("identity")` but **ships with no EF migrations** (`tests`/`src` search for `*Migrations*` in the Identity projects returns nothing).
- DB verification after provisioning:
  ```sql
  SELECT name FROM sys.schemas WHERE name='identity';          -- 0 rows
  SELECT ... FROM sys.tables WHERE name LIKE 'AspNet%';         -- 0 rows
  ```
- Therefore `UserManager.CreateAsync`/`FindByEmailAsync` hit a non-existent table → `SqlException 208`.
- **Impact:** No user can register or log in. Every JWT-protected endpoint (HR, Payroll, Billing, PSA, Workflow, Analytics…) is unreachable end-to-end. This blocks Phases 5, 6 (HTTP path), 8/9 (publish path), 13, 14 (positive cases), 18.

## Root cause (HIGH #2) — Wrong challenge scheme for an API
- Missing/invalid bearer tokens produce `302 Found → /Account/Login?ReturnUrl=…` instead of `401 Unauthorized`. No `WWW-Authenticate` header is returned.
- An API consumed by SPAs/mobile/Scalar must return `401`; a redirect to a (non-existent) MVC login page is incorrect and leaks a cookie-auth default challenge. Indicates the default authenticate/challenge scheme is not pinned to JWT Bearer.

## HIGH #3 — `BuildServiceProvider()` per token validation
- `IdentityServiceCollectionExtensions.cs` (`AddJwtBearer` → `IssuerSigningKeyResolver`) calls `services.BuildServiceProvider().GetRequiredService<DatabaseSigningKeyResolver>()` **inside the resolver delegate**, i.e. it builds a brand-new DI container on every token validation. This is a documented anti-pattern → unbounded memory growth and singleton duplication under load.

## Note on test suite vs. runtime
`Karamchari.Identity.IntegrationTests` reports **9/9 passing**. Those tests stand up their own database (test fixture creates the identity tables), so they do **not** exercise the production provisioning path — which is exactly why the defect escaped. The certification verdict reflects the *running, provisioned application*, where auth fails.

## Security headers
No `X-Content-Type-Options`, `X-Frame-Options`, `Strict-Transport-Security`, `Content-Security-Policy`, or `Referrer-Policy` on responses (checked on `/health`). See `security.md`.

## Verdict: ❌ FAIL (CRITICAL). Authentication must be fixed before any further functional certification.
