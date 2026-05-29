# WS14 — Rate Limiting & Abuse Protection

**Status: ✅ CLOSED (was MEDIUM).**

## Changes implemented
1. **`auth` rate-limit policy** added (`InfrastructureExtensions.cs`): partitioned **per client IP**,
   fixed window **10 requests / 60s**, `RejectionStatusCode = 429`.
   ```csharp
   options.AddPolicy("auth", httpContext =>
       RateLimitPartition.GetFixedWindowLimiter(
           httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
           _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
   ```
2. **Applied to all auth endpoints**: `MapGroup("/api/identity").RequireRateLimiting("auth")` — covers `register`, `login`, `refresh`, `logout` (and any future password-reset under the group).
3. Existing `ai` (5/10s) and `ess` (10/1s) policies retained.

## Verification — attack simulation
14 rapid `POST /api/identity/login` from one IP:
```
401 401 401 401 401 401 401 401 401 401 429 429 429 429
```
First 10 processed (invalid creds → 401), then **429 Too Many Requests** — IP-partitioned burst protection confirmed.

## Verdict
Rate Limiting & Abuse Protection = **PASS** — auth endpoints throttled per IP with burst rejection.
