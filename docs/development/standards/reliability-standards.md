# Reliability & Operational Standards

## 1. Retry Policies (Advanced Reliability)
- **Transient Failures:** Use exponential backoff (e.g., Polly policies).
- **Retry Caps:** Never retry infinitely. Cap retries at 3-5 attempts for sync calls.
- **Circuit Breakers:** Required for all cross-service HTTP calls and critical DB calls.

## 2. Distributed Locking & Concurrency
- Use `UPDLOCK + READPAST` for distributed polling workers (e.g., Outbox).
- Use `RowVersion` (Optimistic Concurrency) on all Domain Aggregates.

## 3. Rate Limiting & Backpressure
- `RateLimiter` middleware is mandatory on all BFF endpoints.
- High-volume endpoints (e.g., ESS) get fixed window limiters.
- Background workers must respect queue saturation and gracefully back off.

## 4. Observability & SLOs
- **SLIs:** API Latency, Error Rate, Outbox Dispatch Delay.
- **SLOs:** 99.9% API success, 95th percentile latency < 200ms.
- **Tracing:** `tenant.id` and `correlation_id` must be attached to all logs and traces. No PII or raw tokens in logs.
