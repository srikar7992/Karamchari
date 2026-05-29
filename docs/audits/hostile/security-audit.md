# Security Skeptic Audit

## Live Checks

| Check | Result |
|---|---|
| `POST /api/identity/logout` without token | HTTP 401 |
| `GET /api/v1/hr/employees` without token | HTTP 401 |
| Register dev user | HTTP 200, returned `userId` |
| Login dev user | HTTP 200, returned access and refresh tokens |
| `GET /api/v1/hr/employees` with JWT | HTTP 200, returned `[]` |
| Security headers on `/health` | Present: `x-content-type-options`, `x-frame-options`, `referrer-policy`, CSP, permissions policy, COOP/CORP/COEP, HSTS, correlation id, traceparent |

## Source Checks

- `Program.cs` validates production JWT secrets fail-fast.
- `InfrastructureExtensions.cs` configures rate limiting for `ai`, `ess`, and `auth`.
- Most BFF endpoint groups call `RequireAuthorization`.
- `SecurityHeadersMiddleware` adds defensive response headers.
- `DevPortalCors` is development-only and restricts origins to localhost/127.0.0.1 port 3000.

## Risks

| Risk | Evidence |
|---|---|
| Tenant authorization for authenticated users needs deeper negative tests | A dev JWT for tenant `dev` could call employee list and received HTTP 200. Cross-tenant object access was not live-tested here. |
| Production config files absent | No `appsettings.Production.json`; secret source contract incomplete. |
| Development secrets are committed | SQL password and RabbitMQ guest credentials exist in dev config/scripts. |
| Live privilege escalation tests not executed | Existing red-team tests are model/test-code based; no live exploit suite was run. |

## Verdict

Security confidence is medium for authentication/header/rate-limit primitives, not certified for tenant/object authorization or production secret operations.
