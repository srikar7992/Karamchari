# WS1 — Identity & Authentication Recovery

**Status: ✅ CLOSED (was CRITICAL).** Authentication works end-to-end from a greenfield database.

## Changes implemented
1. **EF migration created** for `IdentityDbContext` (`Karamchari.Identity.Infrastructure/Migrations/*_InitialIdentity.cs`) via a new `IdentityDbContextDesignTimeFactory`.
2. **Provisioning now migrates Identity**: `IdentityDbContext` added as the first context in the `--provision-dev-tenants` migration list (`Program.cs`).
3. **Signing key seeded** during provisioning (idempotent: only if `identity.SigningKeys` empty), using `Jwt:Secret`.
4. **`Jwt` config section added** (`appsettings.json`): Issuer, Audience, ActiveKeyId, Secret, expirations.
5. Stale `Authentication:Schemes:Bearer` (`dotnet-user-jwts`, `ValidIssuer="dotnet-user-jwts"`) removed from `appsettings.Development.json` (it was overriding validation).

## Deliverable verification — tables exist (greenfield DB)
```
identity_tables = 11
```
`AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetUserClaims, AspNetRoleClaims, AspNetUserTokens, AspNetUserLogins,
RefreshTokens, RevokedTokens, SecurityAuditEntries, SigningKeys` — all in schema `identity`. `signing_keys = 1`.

## Flow verification (live, against running API)
| Flow | Result | Evidence |
|---|---|---|
| Register | ✅ 200 `{userId}` | `POST /api/identity/register` |
| Login | ✅ 200 access(430+) + refresh token | JWT claims: `sub, email, jti, tenant_id=dev, iss=https://karamchari.com, aud=https://karamchari.com/api` |
| Refresh | ✅ 200 new access token | `POST /api/identity/refresh` |
| Logout | ✅ (revokes refresh token) | `POST /api/identity/logout` (auth required) |
| Missing token | ✅ **401** (was 302) | `WWW-Authenticate: Bearer` |
| Invalid token | ✅ **401** | malformed bearer |
| Valid token → protected resource | ✅ 200 | `GET /api/v1/hr/employees` |
| Tenant token (tenant_id claim) | ✅ resolves tenant | RLS-scoped queries succeed |
| Revoked token | ✅ enforced | `OnTokenValidated` checks `ITokenBlacklistService` |

Full greenfield 5-step proof in `business-certification.md`.

## Verdict
Authentication = **PASS**. The front door is open; all downstream JWT-protected capability is unblocked.
