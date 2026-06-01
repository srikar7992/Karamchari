# Failure Certification
**Date:** 2026-06-01  
**Status:** CONDITIONALLY CERTIFIED

---

## Failure Resilience Implementation

| Failure Scenario | Implementation | Status |
|---|---|---|
| Broker down | OutboxRelayCircuitBreaker opens after 5 failures | IMPLEMENTED |
| Broker down recovery | Circuit breaker resets on first success | IMPLEMENTED |
| Consumer down | Messages remain in OutboxMessage table, claimed on restart | IMPLEMENTED |
| Database timeout | EF command timeout, retry via Polly (framework default) | IMPLEMENTED |
| Poison message | Dead-lettered to OutboxDeadLetter, processing continues | IMPLEMENTED |
| Stale lock | Recovered on restart (LockAcquiredUtc + StaleLockTimeout check) | IMPLEMENTED |
| Retry exhaustion | MaxRetries=5, then dead-letter | IMPLEMENTED |
| Duplicate message delivery | MassTransit InboxState (idempotency consumer side) | IMPLEMENTED |

## Tested Resilience Behaviors

| Test | Location | Status |
|---|---|---|
| CircuitBreaker_AfterThresholdFailures_ShouldSkip | Karamchari.Outbox.Tests | PASS |
| CircuitBreaker_AfterSuccessFollowingFailures_ShouldNotSkip | Karamchari.Outbox.Tests | PASS |
| RetryDelay_LargeAttempt_IsCappedAt300Seconds | Karamchari.Outbox.Tests | PASS |
| OutboxProcessingState_IsStale_WhenLockAcquiredExceedsStaleLockTimeout | Karamchari.Outbox.Tests | PASS |

## Runtime Scenarios (Needs Infrastructure)

| Scenario | Method |
|---|---|
| Stop RabbitMQ mid-processing | docker compose stop rabbitmq |
| Stop SQL Server mid-processing | docker compose stop sqlserver |
| Kill consumer process | kill -9 {pid} |
| Network partition between API and broker | iptables / tc netem |

## Certification Decision

**CONDITIONALLY CERTIFIED** — failure handling logic verified in unit tests. Live chaos scenarios require running infrastructure.
