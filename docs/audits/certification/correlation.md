# Phase 13 — Correlation Certification

**Result: ⚠️ PARTIAL.** W3C trace context is real and propagated through the HTTP→EF path; full chain through Outbox→RabbitMQ→Consumer→logs could not be exercised (auth + Worker gaps), and correlation id is not echoed to clients.

## What is verified
- **HTTP request → Activity → response:** Every request creates an `Activity`; the W3C `traceId` is surfaced in error payloads, e.g.
  `00-60ed8499c59fffcf32b7820c5a07e772-0d2ccf8d635ff176-01`.
- **DB query correlation:** EF `DbCommand` activities execute within the request's trace scope (`TenantObservabilityMiddleware` establishes the tenant/correlation envelope consumed by `TenantLogEnricher`).
- **Log correlation:** `TenantLogEnricher` stamps `CorrelationId` + `TenantId` on every log event (code-verified).

## What could NOT be exercised end-to-end
| Hop | Status | Reason |
|---|---|---|
| HTTP request | ✅ | traceId generated |
| DB query | ✅ | EF activity in scope |
| Outbox row | ⛔ | No business txn (auth `302`) |
| RabbitMQ publish | ⛔ | Not reached |
| Consumer | ⛔ | Worker not running; no API receive endpoints |
| Logs / Trace single-id assertion | ⚠️ | Cannot assert one CorrelationId spans all hops without the above |

## Findings
- **LOW:** No correlation id is returned to the caller. A custom `X-Correlation-Id` request header was **not echoed** in any response header, and did not surface in text logs. Recommend echoing the resolved correlation/trace id (e.g. `X-Correlation-Id` / `traceparent`) on responses for client-side tracing.

## Verdict
Trace context plumbing is genuinely present (W3C IDs, EF activities, log enrichment). The "same CorrelationId everywhere — request→DB→outbox→bus→consumer→logs→trace" assertion is **PARTIAL**: provable for request→DB→logs, blocked beyond the DB by the auth and Worker gaps. Re-run after Phase 4 fix with the Worker + collector running.
