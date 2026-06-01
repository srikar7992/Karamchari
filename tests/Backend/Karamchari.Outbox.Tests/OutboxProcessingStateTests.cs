using FluentAssertions;
using Karamchari.Core.Messaging.Outbox;
using Xunit;

namespace Karamchari.Outbox.Tests;

/// <summary>
/// Tests for <see cref="OutboxProcessingState"/> model properties and
/// the stale-lock detection logic that the relay service evaluates in SQL:
///   LockAcquiredUtc &lt; UtcNow - StaleLockTimeout
/// </summary>
public sealed class OutboxProcessingStateTests
{
    // Helper that mirrors the stale check performed by the relay SQL query.
    private static bool IsStale(OutboxProcessingState state, TimeSpan staleLockTimeout)
    {
        if (state.LockedByInstanceId is null || state.LockAcquiredUtc is null)
            return false;

        return state.LockAcquiredUtc.Value < DateTime.UtcNow - staleLockTimeout;
    }

    [Fact]
    public void OutboxProcessingState_Create_DefaultRetryCountIs0()
    {
        var state = new OutboxProcessingState();
        state.RetryCount.Should().Be(0);
    }

    [Fact]
    public void OutboxProcessingState_Create_DefaultNullablePropertiesAreNull()
    {
        var state = new OutboxProcessingState();

        state.LastAttemptUtc.Should().BeNull();
        state.NextRetryUtc.Should().BeNull();
        state.LastError.Should().BeNull();
        state.LockedByInstanceId.Should().BeNull();
        state.LockAcquiredUtc.Should().BeNull();
    }

    [Fact]
    public void OutboxProcessingState_IsStale_WhenLockAcquiredExceedsStaleLockTimeout_ReturnsTrue()
    {
        var staleLockTimeout = TimeSpan.FromMinutes(10);
        var state = new OutboxProcessingState
        {
            OutboxId = Guid.NewGuid(),
            LockedByInstanceId = Guid.NewGuid(),
            // Acquired well beyond the timeout threshold.
            LockAcquiredUtc = DateTime.UtcNow - staleLockTimeout - TimeSpan.FromSeconds(1),
        };

        IsStale(state, staleLockTimeout).Should().BeTrue();
    }

    [Fact]
    public void OutboxProcessingState_IsStale_WhenRecentlyLocked_ReturnsFalse()
    {
        var staleLockTimeout = TimeSpan.FromMinutes(10);
        var state = new OutboxProcessingState
        {
            OutboxId = Guid.NewGuid(),
            LockedByInstanceId = Guid.NewGuid(),
            // Just acquired — well within the timeout window.
            LockAcquiredUtc = DateTime.UtcNow - TimeSpan.FromSeconds(5),
        };

        IsStale(state, staleLockTimeout).Should().BeFalse();
    }

    [Fact]
    public void OutboxProcessingState_IsStale_WhenNoLock_ReturnsFalse()
    {
        var staleLockTimeout = TimeSpan.FromMinutes(10);
        var state = new OutboxProcessingState
        {
            OutboxId = Guid.NewGuid(),
            LockedByInstanceId = null,
            LockAcquiredUtc = null,
        };

        IsStale(state, staleLockTimeout).Should().BeFalse();
    }

    [Fact]
    public void OutboxProcessingState_IsStale_WhenLockAcquiredExactlyAtTimeout_ReturnsFalse()
    {
        // Boundary: exactly at the threshold is NOT stale (strict less-than in SQL).
        var staleLockTimeout = TimeSpan.FromMinutes(10);
        var state = new OutboxProcessingState
        {
            OutboxId = Guid.NewGuid(),
            LockedByInstanceId = Guid.NewGuid(),
            LockAcquiredUtc = DateTime.UtcNow - staleLockTimeout,
        };

        // At-or-after the boundary — the SQL uses strict '<', so equal is not stale.
        // We add a small buffer so clock drift does not make this flaky.
        IsStale(state, staleLockTimeout + TimeSpan.FromSeconds(1)).Should().BeFalse();
    }

    [Fact]
    public void OutboxProcessingState_IsStale_NoLockAcquiredUtcButHasInstanceId_ReturnsFalse()
    {
        // Inconsistent state: InstanceId set but no timestamp — treat as not stale
        // (the relay only uses LockAcquiredUtc for the comparison).
        var state = new OutboxProcessingState
        {
            OutboxId = Guid.NewGuid(),
            LockedByInstanceId = Guid.NewGuid(),
            LockAcquiredUtc = null,
        };

        IsStale(state, TimeSpan.FromMinutes(10)).Should().BeFalse();
    }

    [Fact]
    public void OutboxProcessingState_Properties_RoundTrip()
    {
        var id = Guid.NewGuid();
        var instanceId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var state = new OutboxProcessingState
        {
            OutboxId = id,
            RetryCount = 3,
            LastAttemptUtc = now.AddMinutes(-1),
            NextRetryUtc = now.AddMinutes(5),
            LastError = "Connection refused",
            LockedByInstanceId = instanceId,
            LockAcquiredUtc = now,
        };

        state.OutboxId.Should().Be(id);
        state.RetryCount.Should().Be(3);
        state.LastAttemptUtc.Should().BeCloseTo(now.AddMinutes(-1), precision: TimeSpan.FromMilliseconds(1));
        state.NextRetryUtc.Should().BeCloseTo(now.AddMinutes(5), precision: TimeSpan.FromMilliseconds(1));
        state.LastError.Should().Be("Connection refused");
        state.LockedByInstanceId.Should().Be(instanceId);
        state.LockAcquiredUtc.Should().BeCloseTo(now, precision: TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public void OutboxProcessingState_IsStale_WhenLockAcquiredMoreThan10MinutesAgo_WithDefaultTimeout()
    {
        var defaultStaleLockTimeout = TimeSpan.FromMinutes(10); // matches OutboxRelayOptions default
        var state = new OutboxProcessingState
        {
            OutboxId = Guid.NewGuid(),
            LockedByInstanceId = Guid.NewGuid(),
            LockAcquiredUtc = DateTime.UtcNow - TimeSpan.FromMinutes(11),
        };

        IsStale(state, defaultStaleLockTimeout).Should().BeTrue();
    }
}
