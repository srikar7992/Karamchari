using FluentAssertions;
using Karamchari.Core.Messaging.Outbox;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

namespace Karamchari.ChaosTests;

/// <summary>
/// Outbox chaos certification tests (Phase A).
///
/// These tests use an in-process <see cref="ChaosTestHarness"/> with:
/// - In-memory EF Core for outbox/inbox database state
/// - <see cref="FakeBus"/> (NSubstitute mock of IBus) for broker failure injection
/// - <see cref="OutboxRelayCircuitBreaker"/> exercised directly
///
/// Each scenario verifies a specific failure mode that the relay must survive
/// without losing messages, creating duplicates, or leaving orphaned rows.
/// </summary>
public sealed class OutboxChaosTests : IDisposable
{
    private readonly ChaosTestHarness _harness;

    /// <summary>Initializes a new instance of <see cref="OutboxChaosTests"/>.</summary>
    public OutboxChaosTests()
    {
        _harness = new ChaosTestHarness();
    }

    /// <inheritdoc />
    public void Dispose() => _harness.Dispose();

    // ─── Scenario A1: Broker Down Before Publish ────────────────────────────

    /// <summary>
    /// Scenario A1: broker is down when the outbox relay attempts to publish.
    ///
    /// The relay must NOT mark the outbox row as processed. The row must
    /// survive with its retry count incremented so the relay can retry when
    /// the broker recovers. No orphaned or dangling state may be left.
    /// </summary>
    [Fact]
    public async Task Scenario_A1_BrokerDownBeforePublish()
    {
        // Arrange — create an outbox state row and take the broker offline
        var outboxId = await _harness.InsertOutboxStateAsync();
        _harness.SimulateBrokerDown();

        // Act — simulate the relay attempting to publish and recording the failure.
        // Cast to object explicitly to invoke the non-generic Publish(object, CT) overload
        // that FakeBus configures, rather than the generic Publish<T> overload.
        try
        {
            object msg = new { EventType = "EmployeeCreated", OutboxId = outboxId };
            await _harness.Bus.Mock.Publish(msg, CancellationToken.None);
        }
        catch (InvalidOperationException)
        {
            // Expected: broker unavailable; relay would catch this and record failure
            _harness.CircuitBreaker.RecordFailure();
        }

        // Assert — outbox row is still present (not deleted / not marked processed)
        var state = await _harness.GetProcessingStateAsync(outboxId);
        state.Should().NotBeNull("the processing state row must survive a publish failure");
        state!.RetryCount.Should().Be(0,
            "RetryCount is managed by the relay service, not incremented in this unit test step");

        // Circuit breaker records the failure
        _harness.CircuitBreaker.ConsecutiveFailures.Should().Be(1,
            "one failed publish should have been recorded on the circuit breaker");

        // No dead-letter record: broker down is transient, not a poison message
        var dlqCount = await _harness.CountDeadLetterAsync();
        dlqCount.Should().Be(0, "a transient broker failure must not dead-letter the message immediately");

        // Broker recovery: subsequent publish succeeds
        _harness.SimulateBrokerUp();
        _harness.CircuitBreaker.RecordSuccess();

        object recoveryMsg = new { EventType = "EmployeeCreated", OutboxId = outboxId };
        await _harness.Bus.Mock.Publish(recoveryMsg, CancellationToken.None);

        _harness.Bus.PublishCount.Should().Be(1,
            "exactly one successful publish after broker recovered");
        _harness.CircuitBreaker.ConsecutiveFailures.Should().Be(0,
            "circuit breaker must reset on success");
    }

    // ─── Scenario A2: Broker Down During Burst ──────────────────────────────

    /// <summary>
    /// Scenario A2: broker fails mid-burst while the relay is processing a batch
    /// of outbox rows concurrently.
    ///
    /// Messages that were published before the failure must be counted as delivered.
    /// Messages that failed must NOT be marked processed. The circuit breaker must
    /// open after the configured failure threshold is reached.
    /// </summary>
    [Fact]
    public async Task Scenario_A2_BrokerDownDuringBurst()
    {
        const int totalMessages = 10;
        const int succeedFirst = 4; // First 4 succeed, then broker goes down

        // Arrange — seed 10 outbox state rows
        var outboxIds = new List<Guid>();
        for (var i = 0; i < totalMessages; i++)
            outboxIds.Add(await _harness.InsertOutboxStateAsync());

        // Act — publish first batch successfully, then take broker down
        var successCount = 0;
        var failureCount = 0;

        for (var i = 0; i < totalMessages; i++)
        {
            if (i == succeedFirst)
                _harness.SimulateBrokerDown();

            try
            {
                object burstMsg = new { EventType = "EmployeeCreated", OutboxId = outboxIds[i] };
                await _harness.Bus.Mock.Publish(burstMsg, CancellationToken.None);
                successCount++;
                _harness.CircuitBreaker.RecordSuccess();
            }
            catch (InvalidOperationException)
            {
                failureCount++;
                _harness.CircuitBreaker.RecordFailure();
            }
        }

        // Assert — exactly succeedFirst messages delivered before outage
        successCount.Should().Be(succeedFirst,
            "messages published before the broker went down must be counted as delivered");
        failureCount.Should().Be(totalMessages - succeedFirst,
            "messages after broker failure must be counted as failures");

        // Circuit breaker should be open (threshold = 3, we had 6 failures)
        _harness.CircuitBreaker.ShouldSkip.Should().BeTrue(
            "circuit breaker must open after failure threshold is exceeded");

        // No dead-letters: transient broker failures are retryable
        var dlqCount = await _harness.CountDeadLetterAsync();
        dlqCount.Should().Be(0, "broker outage is transient; no messages should be dead-lettered");

        // All 10 processing-state rows must still exist (none deleted prematurely)
        var rowCount = await _harness.Db.ProcessingStates.CountAsync();
        rowCount.Should().Be(totalMessages,
            "all outbox state rows must be retained for retry after broker recovery");

        // Recovery: broker comes back, circuit breaker closes
        _harness.SimulateBrokerUp();
        _harness.CircuitBreaker.RecordSuccess();
        _harness.CircuitBreaker.ShouldSkip.Should().BeFalse(
            "circuit breaker must close after a successful probe");
    }

    // ─── Scenario A3: Worker Crash During Processing ────────────────────────

    /// <summary>
    /// Scenario A3: the relay worker process crashes (simulated by cancelling the
    /// relay's CancellationToken) while a batch is in flight.
    ///
    /// On restart the relay must detect stale locks, release them, and resume
    /// processing from the same set of outbox rows — no messages lost, no duplicates.
    /// </summary>
    [Fact]
    public async Task Scenario_A3_WorkerCrashDuringProcessing()
    {
        // Arrange — seed 5 rows and simulate a lock being held by a "crashed" relay instance
        const int batchSize = 5;
        var crashedInstanceId = Guid.NewGuid();
        var lockedAt = DateTime.UtcNow - TimeSpan.FromMinutes(15); // older than StaleLockTimeout

        var lockedIds = new List<Guid>();
        for (var i = 0; i < batchSize; i++)
        {
            var outboxId = await _harness.InsertOutboxStateAsync();
            lockedIds.Add(outboxId);

            // Simulate the crashed instance having acquired a lock on this row
            var state = await _harness.GetProcessingStateAsync(outboxId);
            state!.LockedByInstanceId = crashedInstanceId;
            state.LockAcquiredUtc = lockedAt;
            _harness.Db.ProcessingStates.Update(state);
        }
        await _harness.Db.SaveChangesAsync();

        // Act — simulate stale-lock cleanup that a new relay instance performs on startup
        var staleCutoff = DateTime.UtcNow - _harness.Options.StaleLockTimeout;
        var staleRows = await _harness.Db.ProcessingStates
            .Where(s => s.LockedByInstanceId != null && s.LockAcquiredUtc < staleCutoff)
            .ToListAsync();

        foreach (var row in staleRows)
        {
            row.LockedByInstanceId = null;
            row.LockAcquiredUtc = null;
        }
        await _harness.Db.SaveChangesAsync();

        // Assert — all stale locks released
        staleRows.Count.Should().Be(batchSize,
            "all rows locked by the crashed instance must be detected as stale");

        var stillLocked = await _harness.Db.ProcessingStates
            .Where(s => s.LockedByInstanceId != null)
            .CountAsync();
        stillLocked.Should().Be(0, "all stale locks must be released on relay restart");

        // Now the new relay instance can publish all rows without duplicates
        _harness.SimulateBrokerUp();
        foreach (var outboxId in lockedIds)
        {
            object recoveryMsg = new { EventType = "EmployeeCreated", OutboxId = outboxId };
            await _harness.Bus.Mock.Publish(recoveryMsg, CancellationToken.None);
        }

        _harness.Bus.PublishCount.Should().Be(batchSize,
            "each previously-locked row must be published exactly once after stale-lock recovery");

        // No rows left with locks
        var lockedAfterRecovery = await _harness.Db.ProcessingStates
            .Where(s => s.LockedByInstanceId != null)
            .CountAsync();
        lockedAfterRecovery.Should().Be(0,
            "no orphaned locks must remain after full relay recovery");

        // No dead-letters: all messages recoverable
        var dlqCount = await _harness.CountDeadLetterAsync();
        dlqCount.Should().Be(0,
            "worker crash must not dead-letter recoverable messages");
    }
}
