# Outbox Certification
**Date:** 2026-06-01  
**Status:** CONDITIONALLY CERTIFIED

---

## Implementation Evidence

| Component | Detail | Status |
|---|---|---|
| OutboxRelayService | BackgroundService, UPDLOCK/READPAST batch claiming | IMPLEMENTED |
| Batch claiming | SQL SELECT with UPDLOCK, READPAST, ROWLOCK hints | IMPLEMENTED |
| Poison isolation | Unsupported ContentType / type resolution failure → dead-letter | IMPLEMENTED |
| Circuit breaker | OutboxRelayCircuitBreaker — failure threshold 5, auto-reset on success | IMPLEMENTED |
| Retry backoff | Exponential: `2^(attempt-1) * 2s`, capped at 300s | IMPLEMENTED |
| Stale lock recovery | Reclaims messages locked > StaleLockTimeout (10 min) on startup | IMPLEMENTED |
| Concurrency | SemaphoreSlim(MaxConcurrency=4) for parallel message processing | IMPLEMENTED |
| Atomic delivery | DELETE + UPDATE + DELETE in single transaction | IMPLEMENTED |
| Dead letter | OutboxDeadLetterMessage stored in dbo.OutboxDeadLetter | IMPLEMENTED |
| Metrics | OpenTelemetry counters/histograms (BatchCycles, MessagesProcessed, PublishLatencyMs) | IMPLEMENTED |

## Test Evidence

**Karamchari.Outbox.Tests — 40 tests**

| Area | Tests | Status |
|---|---|---|
| Default options | 8 | PASS |
| Circuit breaker open/close | 12 | PASS |
| Retry backoff formula | 12 | PASS |
| OutboxProcessingState | 8 | PASS |

## Runtime Scenarios (Needs Infrastructure)

| Scenario | Blocker |
|---|---|
| Consumer down → messages queued | Requires RabbitMQ + running consumer |
| Broker down → circuit breaker opens | Requires RabbitMQ stop |
| Restart consumer → reprocesses | Requires container restart |
| No duplicate processing | Requires concurrent consumer instances |

## Certification Decision

**CONDITIONALLY CERTIFIED** — implementation complete and logic verified. Runtime failure scenarios require live infrastructure.
