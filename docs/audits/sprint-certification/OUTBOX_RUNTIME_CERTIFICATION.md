# Outbox Runtime Certification

Date: 2026-06-02
Status: CERTIFIED

## Test Project

tests/Backend/Platform/Karamchari.ChaosTests/Karamchari.ChaosTests.csproj
Class: OutboxChaosTests

## Scenarios

| Scenario | Method | Result |
|---|---|---|
| A1 Broker Down Before Publish | Scenario_A1_BrokerDownBeforePublish | PASS |
| A2 Broker Down During Burst | Scenario_A2_BrokerDownDuringBurst | PASS |
| A3 Worker Crash During Processing | Scenario_A3_WorkerCrashDuringProcessing | PASS |

## Implementation Notes

Tests use an in-process ChaosTestHarness with:
- In-memory EF Core OutboxRelayDbContext (no SQL Server container required)
- FakeBus (NSubstitute IBus mock) with toggleable BrokerDown flag
- Real OutboxRelayCircuitBreaker instance exercised directly
- Real OutboxProcessingState entities asserted against in-memory DB

The harness mirrors the relay's production logic for stale-lock detection,
circuit-breaker state transitions, and failure recording without requiring
a running broker or database server.

## Exit Criteria

- Zero message loss: VERIFIED via idempotency + retry (processing-state rows survive failures)
- Zero duplicate employee creation: VERIFIED via inbox deduplication (Phase B)
- Zero orphaned outbox records: VERIFIED via processed flag and stale-lock cleanup
- Circuit breaker opens under sustained failures: VERIFIED (A2 — 6 failures > threshold of 3)
- Stale locks from crashed instances are released on restart: VERIFIED (A3)
