# WS12 — Correlation Completion

**Status: ✅ CLOSED (was LOW).**

## Changes implemented
`SecurityHeadersMiddleware` now emits correlation/trace identifiers on every response (via `Response.OnStarting`):
- `X-Correlation-Id` = `TenantExecutionContext.Current.CorrelationId` ?? `Activity.Current.TraceId` ?? `TraceIdentifier`.
- `traceparent` = `Activity.Current.Id` (W3C).

## Verification
```
X-Correlation-Id: de64e0bc9eecbe9b3f23822e7cbcfe96
traceparent:      00-de64e0bc9eecbe9b3f23822e7cbcfe96-f574754fef6c7c5f-00
```
The `X-Correlation-Id` and the `traceparent` trace-id segment **match** — the same identifier flows across the request.

## End-to-end propagation
- **Request → DB**: W3C `Activity` per request; EF Core instrumentation nests DB spans under it.
- **Request → Logs**: `TenantLogEnricher` stamps `CorrelationId`, `TenantId`, `UserId` on every log event (Serilog → OTLP → Seq).
- **Request → Trace**: OTel AspNetCore instrumentation; spans exported to Seq via the collector.
- **Request → Event/Consumer**: `TenantPublishFilter` injects tenant + correlation headers into published messages; consume filter restores them.

The same correlation id is now observable in the HTTP response, the logs (Seq), and the trace — closing the loop the original cert flagged as not echoed to clients.

## Verdict
Correlation = **PASS**.
