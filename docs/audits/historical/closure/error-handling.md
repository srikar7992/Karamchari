# WS6 — Exception & Error Sanitization

**Status: ✅ CLOSED (was HIGH).**

## Changes implemented
1. **`DeveloperExceptionPage` restricted to `Development` only** (`Program.cs`). `Local` and all other environments now use the sanitized `GlobalExceptionHandler`. (Previously enabled for `Development || Local`, leaking stack traces + request headers in the deployable-sounding `Local` profile.)
2. **`GlobalExceptionHandler` sanitized**: no raw exception text, no SQL messages, no stack traces.
   - Generic 500 → `Detail = "An unexpected error occurred ... Quote the correlationId when contacting support."`
   - `UnauthorizedAccessException` → 401, generic detail.
   - `TenantResolutionException` → 400, generic detail (was echoing `ex.Message`).
   - `ValidationException` → 400 with structured field errors (client-safe, intended).
   - Full exception still logged server-side with the correlation id.
3. RFC 7807 `ProblemDetails` with `traceId`, `correlationId`, `tenantId`, `requestId` extensions.
4. Unit test updated (`GlobalExceptionHandlerTests`): asserts raw message is **not** leaked and `correlationId` present.

## Verification
- `Karamchari.Api.UnitTests` — 2/2 passing (incl. updated sanitization assertion).
- Forced exceptions (SQL/infra → generic 500 sanitized; validation → 400 structured; domain/unauthorized → mapped) all return `application/problem+json` with no internals.

## Verdict
Exception & Error Sanitization = **PASS**.
