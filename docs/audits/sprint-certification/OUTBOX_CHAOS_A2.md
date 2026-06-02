# Outbox Chaos Scenario A2

Date: 2026-06-02
Test Class: OutboxChaosTests.Scenario_A2_BrokerDownDuringBurst

## Scenario

A batch of 10 outbox messages is being processed concurrently. The broker is healthy
for the first 4 publishes, then fails mid-burst. The remaining 6 publishes all throw.

This simulates a broker rolling restart or a transient network partition that begins
while a relay batch is already in flight — the most destructive transient scenario
because some messages succeed and some fail within the same batch cycle.

The relay must correctly account for which messages were delivered (before the failure)
and which must be retried. The circuit breaker must open after the failure threshold is
reached. No messages may be dead-lettered and no outbox rows may be deleted prematurely.

## Automated Test

File: tests/Backend/Platform/Karamchari.ChaosTests/OutboxChaosTests.cs
Method: Scenario_A2_BrokerDownDuringBurst

## Expected Assertions

- Exactly 4 successful publishes recorded before the broker went down.
- Exactly 6 publish failures recorded.
- `OutboxRelayCircuitBreaker.ShouldSkip` is `true` after 6 failures (threshold = 3).
- `dbo.OutboxDeadLetter` count remains 0.
- All 10 `OutboxProcessingState` rows remain in the database (none deleted).
- After `SimulateBrokerUp()` + `RecordSuccess()`, `ShouldSkip` returns `false`.

## Result

PASS — test implemented and asserting correct behavior
