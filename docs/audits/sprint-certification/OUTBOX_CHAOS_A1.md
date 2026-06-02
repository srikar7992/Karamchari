# Outbox Chaos Scenario A1

Date: 2026-06-02
Test Class: OutboxChaosTests.Scenario_A1_BrokerDownBeforePublish

## Scenario

The outbox relay attempts to publish an event to the message broker. The broker is
unavailable at the moment the relay calls `IBus.Publish`. This is the most common
transient failure mode (network partition, broker restart, TLS handshake timeout).

The relay must NOT mark the outbox row as delivered. It must record the failure on
the circuit breaker, leave the processing-state row intact for retry, and not
dead-letter the message (dead-lettering is reserved for poison/unresolvable messages,
not transient broker failures).

After the broker recovers, the relay retries and delivers the message exactly once.

## Automated Test

File: tests/Backend/Platform/Karamchari.ChaosTests/OutboxChaosTests.cs
Method: Scenario_A1_BrokerDownBeforePublish

## Expected Assertions

- `OutboxProcessingState` row for the failed message still exists after the failed publish attempt.
- `OutboxRelayCircuitBreaker.ConsecutiveFailures` equals 1 after the single failure.
- `dbo.OutboxDeadLetter` count remains 0 (transient, not poison).
- After `SimulateBrokerUp()` + `RecordSuccess()`, a second publish succeeds and `FakeBus.PublishCount` equals 1.
- `CircuitBreaker.ConsecutiveFailures` resets to 0 after the successful publish.

## Result

PASS — test implemented and asserting correct behavior
