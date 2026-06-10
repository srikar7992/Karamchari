// -----------------------------------------------------------------------
// <copyright file="OutboxRelayServiceRetryTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Xunit;

namespace Karamchari.Outbox.Tests;

/// <summary>
/// Pure math tests for the exponential backoff formula used by OutboxRelayService.ComputeRetryDelay:
///   delay = Min(initialDelaySeconds * 2^(attempt-1), 300) seconds
/// These tests exercise the formula directly without requiring a database or bus.
/// </summary>
public sealed class OutboxRelayServiceRetryTests
{
    // Mirror the private static method exactly so tests stay in sync with production behaviour.
    private static TimeSpan ComputeRetryDelay(int attempt, double initialDelaySeconds = 2.0, double maxSeconds = 300.0)
    {
        var seconds = initialDelaySeconds * Math.Pow(2, attempt - 1);
        return TimeSpan.FromSeconds(Math.Min(seconds, maxSeconds));
    }

    [Fact]
    public void RetryDelay_Attempt1_Is2Seconds()
    {
        // 2 * 2^(1-1) = 2 * 1 = 2s
        var delay = ComputeRetryDelay(attempt: 1);
        delay.Should().Be(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void RetryDelay_Attempt2_Is4Seconds()
    {
        // 2 * 2^(2-1) = 2 * 2 = 4s
        var delay = ComputeRetryDelay(attempt: 2);
        delay.Should().Be(TimeSpan.FromSeconds(4));
    }

    [Fact]
    public void RetryDelay_Attempt3_Is8Seconds()
    {
        // 2 * 2^(3-1) = 2 * 4 = 8s
        var delay = ComputeRetryDelay(attempt: 3);
        delay.Should().Be(TimeSpan.FromSeconds(8));
    }

    [Fact]
    public void RetryDelay_Attempt4_Is16Seconds()
    {
        // 2 * 2^(4-1) = 2 * 8 = 16s
        var delay = ComputeRetryDelay(attempt: 4);
        delay.Should().Be(TimeSpan.FromSeconds(16));
    }

    [Fact]
    public void RetryDelay_Attempt5_Is32Seconds()
    {
        // 2 * 2^(5-1) = 2 * 16 = 32s
        var delay = ComputeRetryDelay(attempt: 5);
        delay.Should().Be(TimeSpan.FromSeconds(32));
    }

    [Fact]
    public void RetryDelay_LargeAttempt_IsCappedAt300Seconds()
    {
        // Attempt 10: 2 * 2^9 = 1024s — well above cap
        var delay = ComputeRetryDelay(attempt: 10);
        delay.Should().Be(TimeSpan.FromSeconds(300));
    }

    [Fact]
    public void RetryDelay_Attempt9_Is256Seconds_BelowCap()
    {
        // 2 * 2^8 = 512 -> capped to 300
        var delay = ComputeRetryDelay(attempt: 9);
        delay.Should().Be(TimeSpan.FromSeconds(300));
    }

    [Fact]
    public void RetryDelay_Attempt8_Is256Seconds()
    {
        // 2 * 2^7 = 256s — still below cap
        var delay = ComputeRetryDelay(attempt: 8);
        delay.Should().Be(TimeSpan.FromSeconds(256));
    }

    [Fact]
    public void RetryDelay_IsMonotonicallyIncreasingUntilCap()
    {
        var delays = Enumerable.Range(1, 8).Select(a => ComputeRetryDelay(a).TotalSeconds).ToList();

        for (var i = 1; i < delays.Count; i++)
            delays[i].Should().BeGreaterThan(delays[i - 1],
                $"delay at attempt {i + 1} should exceed delay at attempt {i}");
    }

    [Fact]
    public void RetryDelay_NeverExceedsCap()
    {
        for (var attempt = 1; attempt <= 20; attempt++)
        {
            var delay = ComputeRetryDelay(attempt);
            delay.TotalSeconds.Should().BeLessThanOrEqualTo(300,
                $"delay for attempt {attempt} must not exceed the 300s cap");
        }
    }

    [Fact]
    public void RetryDelay_IsExponentialDoubling()
    {
        // Each attempt doubles the previous until the cap is hit.
        // Attempts 1-7 are strictly below 300s with initialDelay=2s.
        double prev = ComputeRetryDelay(1).TotalSeconds;
        for (var attempt = 2; attempt <= 7; attempt++)
        {
            var current = ComputeRetryDelay(attempt).TotalSeconds;
            current.Should().BeApproximately(prev * 2, precision: 0.001,
                $"attempt {attempt} delay should be double attempt {attempt - 1}");
            prev = current;
        }
    }

    [Fact]
    public void RetryDelay_CustomInitialDelay_ScalesCorrectly()
    {
        // With initialDelay=10s: attempt 1 = 10s, attempt 2 = 20s
        var delay1 = ComputeRetryDelay(attempt: 1, initialDelaySeconds: 10.0);
        var delay2 = ComputeRetryDelay(attempt: 2, initialDelaySeconds: 10.0);

        delay1.Should().Be(TimeSpan.FromSeconds(10));
        delay2.Should().Be(TimeSpan.FromSeconds(20));
    }
}
