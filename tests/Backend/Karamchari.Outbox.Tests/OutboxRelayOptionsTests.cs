// -----------------------------------------------------------------------
// <copyright file="OutboxRelayOptionsTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Core.Messaging.Outbox;
using Xunit;

namespace Karamchari.Outbox.Tests;

public sealed class OutboxRelayOptionsTests
{
    [Fact]
    public void OutboxRelayOptions_Defaults_BatchSizeIs100()
    {
        var opts = new OutboxRelayOptions();
        opts.BatchSize.Should().Be(100);
    }

    [Fact]
    public void OutboxRelayOptions_Defaults_MaxRetriesIs5()
    {
        var opts = new OutboxRelayOptions();
        opts.MaxRetries.Should().Be(5);
    }

    [Fact]
    public void OutboxRelayOptions_Defaults_PollingIntervalIs5Seconds()
    {
        var opts = new OutboxRelayOptions();
        opts.PollingInterval.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void OutboxRelayOptions_Defaults_MaxConcurrencyIs4()
    {
        var opts = new OutboxRelayOptions();
        opts.MaxConcurrency.Should().Be(4);
    }

    [Fact]
    public void OutboxRelayOptions_Defaults_StaleLockTimeoutIs10Minutes()
    {
        var opts = new OutboxRelayOptions();
        opts.StaleLockTimeout.Should().Be(TimeSpan.FromMinutes(10));
    }

    [Fact]
    public void OutboxRelayOptions_Defaults_CircuitBreakerThresholdIs5()
    {
        var opts = new OutboxRelayOptions();
        opts.CircuitBreakerFailureThreshold.Should().Be(5);
    }

    [Fact]
    public void OutboxRelayOptions_Defaults_InitialRetryDelayIs2Seconds()
    {
        var opts = new OutboxRelayOptions();
        opts.InitialRetryDelay.Should().Be(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void OutboxRelayOptions_SectionName_IsOutboxRelay()
    {
        OutboxRelayOptions.SectionName.Should().Be("OutboxRelay");
    }
}
