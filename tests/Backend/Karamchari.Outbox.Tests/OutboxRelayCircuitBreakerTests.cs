using FluentAssertions;
using Karamchari.Core.Messaging.Outbox;
using Microsoft.Extensions.Options;
using Xunit;

namespace Karamchari.Outbox.Tests;

public sealed class OutboxRelayCircuitBreakerTests
{
    private static OutboxRelayCircuitBreaker CreateBreaker(int threshold = 5)
    {
        var opts = Options.Create(new OutboxRelayOptions
        {
            CircuitBreakerFailureThreshold = threshold,
            CircuitBreakerResetTimeout = TimeSpan.FromMinutes(1),
        });
        return new OutboxRelayCircuitBreaker(opts);
    }

    [Fact]
    public void CircuitBreaker_Initially_ShouldNotSkip()
    {
        var breaker = CreateBreaker();
        breaker.ShouldSkip.Should().BeFalse();
    }

    [Fact]
    public void CircuitBreaker_AfterThresholdFailures_ShouldSkip()
    {
        var breaker = CreateBreaker(threshold: 5);

        for (var i = 0; i < 5; i++)
            breaker.RecordFailure();

        breaker.ShouldSkip.Should().BeTrue();
    }

    [Fact]
    public void CircuitBreaker_AfterSuccessFollowingFailures_ShouldNotSkip()
    {
        // Accumulate failures below threshold, then a success should reset.
        var breaker = CreateBreaker(threshold: 5);

        for (var i = 0; i < 3; i++)
            breaker.RecordFailure();

        breaker.RecordSuccess();

        breaker.ShouldSkip.Should().BeFalse();
    }

    [Fact]
    public void CircuitBreaker_SingleFailureBelowThreshold_ShouldNotSkip()
    {
        var breaker = CreateBreaker(threshold: 5);

        breaker.RecordFailure();

        breaker.ShouldSkip.Should().BeFalse();
    }

    [Fact]
    public void CircuitBreaker_ExactlyAtThreshold_ShouldSkip()
    {
        const int threshold = 3;
        var breaker = CreateBreaker(threshold: threshold);

        for (var i = 0; i < threshold; i++)
            breaker.RecordFailure();

        breaker.ShouldSkip.Should().BeTrue();
    }

    [Fact]
    public void CircuitBreaker_OneBelowThreshold_ShouldNotSkip()
    {
        const int threshold = 5;
        var breaker = CreateBreaker(threshold: threshold);

        for (var i = 0; i < threshold - 1; i++)
            breaker.RecordFailure();

        breaker.ShouldSkip.Should().BeFalse();
    }

    [Fact]
    public void CircuitBreaker_ConsecutiveFailures_ReflectsAccumulatedCount()
    {
        var breaker = CreateBreaker(threshold: 10);

        breaker.RecordFailure();
        breaker.RecordFailure();
        breaker.RecordFailure();

        breaker.ConsecutiveFailures.Should().Be(3);
    }

    [Fact]
    public void CircuitBreaker_AfterSuccess_ConsecutiveFailuresResetToZero()
    {
        var breaker = CreateBreaker(threshold: 10);

        breaker.RecordFailure();
        breaker.RecordFailure();
        breaker.RecordSuccess();

        breaker.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public void CircuitBreaker_StateLabel_InitiallyIsClosed()
    {
        var breaker = CreateBreaker();
        breaker.StateLabel.Should().Be("Closed");
    }

    [Fact]
    public void CircuitBreaker_StateLabel_IsOpenAfterThresholdExceeded()
    {
        var breaker = CreateBreaker(threshold: 2);
        breaker.RecordFailure();
        breaker.RecordFailure();

        breaker.StateLabel.Should().Be("Open");
    }

    [Fact]
    public void CircuitBreaker_StateLabel_IsClosedAfterSuccessFromClosed()
    {
        var breaker = CreateBreaker();
        breaker.RecordSuccess();

        breaker.StateLabel.Should().Be("Closed");
    }
}
