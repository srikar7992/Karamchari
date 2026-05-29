# WS5 — Security Headers

**Status: ✅ CLOSED (was HIGH).**

## Changes implemented
`SecurityHeadersMiddleware` (`Karamchari.Api/Middleware/SecurityHeadersMiddleware.cs`), registered first in the pipeline (`app.UseKaramchariSecurityHeaders()` right after `UseExceptionHandler`). Sets headers via `Response.OnStarting` so they appear on success AND error paths.

## Verification (live `curl -D -`)
```
X-Content-Type-Options: nosniff
X-Frame-Options: DENY
Referrer-Policy: no-referrer
Content-Security-Policy: default-src 'self'; frame-ancestors 'none'; object-src 'none'; base-uri 'self'
Permissions-Policy: geolocation=(), microphone=(), camera=(), payment=()
Cross-Origin-Opener-Policy: same-origin
Cross-Origin-Resource-Policy: same-origin
Cross-Origin-Embedder-Policy: require-corp
Strict-Transport-Security: max-age=31536000; includeSubDomains; preload   (HTTPS only)
```
All 9 required headers present. HSTS gated to HTTPS to avoid pinning plain-HTTP dev endpoints.

## Verdict
Security Headers = **PASS** (9/9).
