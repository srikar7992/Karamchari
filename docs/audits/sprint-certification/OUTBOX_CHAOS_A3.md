# Outbox Chaos Scenario A3

Date: 2026-06-02
Test Class: OutboxChaosTests.Scenario_A3_WorkerCrashDuringProcessing

## Scenario

The relay worker acquires SQL locks on a batch of 5 outbox rows, begins processing,
then crashes (process killed, OOM, node reboot). The locks recorded in
`dbo.OutboxProcessingState` are now orphaned: `LockedByInstanceId` is set to the
crashed instance's GUID and `LockAcquiredUtc` is older than the configured
`StaleLockTimeout` (10 minutes by default).

When a new relay instance starts, it runs `TryReleaseStaleLockAsync` which scans for
rows with `LockAcquiredUtc < (NOW - StaleLockTimeout)` and releases them by setting
`LockedByInstanceId = null` and `LockAcquiredUtc = null`.

The new instance must then be able to publish all 5 messages exactly once, with no
orphaned locks and no dead-letters.

## Automated Test

File: tests/Backend/Platform/Karamchari.ChaosTests/OutboxChaosTests.cs
Method: Scenario_A3_WorkerCrashDuringProcessing

## Expected Assertions

- All 5 `OutboxProcessingState` rows with `LockAcquiredUtc` older than `StaleLockTimeout` are detected as stale.
- After stale-lock cleanup, zero rows have a non-null `LockedByInstanceId`.
- The new relay instance publishes all 5 messages: `FakeBus.PublishCount` equals 5.
- Zero rows have non-null `LockedByInstanceId` after recovery.
- `dbo.OutboxDeadLetter` count remains 0.

## Result

PASS — test implemented and asserting correct behavior
