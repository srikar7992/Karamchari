# WS3 — JWT Security Hardening

**Status: ✅ CLOSED (was HIGH).**

## Changes implemented (`IdentityServiceCollectionExtensions.cs`)
1. **Default schemes pinned to JWT bearer**: `DefaultAuthenticateScheme`, `DefaultChallengeScheme`, `DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme`.
2. **`BuildServiceProvider()` removed**: the per-request `IssuerSigningKeyResolver` that built a new DI container on every validation is replaced by a singleton `IPostConfigureOptions<JwtBearerOptions>` (`ConfigureJwtBearerOptions`) that captures the singleton `DatabaseSigningKeyResolver` once at startup. PostConfigure also guarantees our params win over framework config-binding.
3. **Singleton signing-key resolver + caching + rotation**: `DatabaseSigningKeyResolver` (singleton) reads `identity.SigningKeys` with a 10-min `IMemoryCache`; `IssuerSigningKeyResolver` returns all DB keys (multi-key rotation) plus a configured-secret fallback.
4. **Cookie redirect → API codes**: `ConfigureApplicationCookie` converts the Identity cookie's `OnRedirectToLogin`/`OnRedirectToAccessDenied` into 401/403 (belt-and-suspenders).

## Verification
```
GET /api/v1/hr/employees            (no token)   -> HTTP 401   (was 302 -> /Account/Login)
GET /api/v1/hr/employees            (bad token)  -> HTTP 401
WWW-Authenticate header              present:     "Bearer"
GET /api/v1/hr/employees            (valid token)-> HTTP 200
```
`grep -rc "BuildServiceProvider" src/Backend/Karamchari.Identity*` → **0** inside authentication.

## Verdict
JWT Security Hardening = **PASS**. 401 (not 302), `WWW-Authenticate: Bearer`, no `BuildServiceProvider`, singleton resolver with caching + rotation.
