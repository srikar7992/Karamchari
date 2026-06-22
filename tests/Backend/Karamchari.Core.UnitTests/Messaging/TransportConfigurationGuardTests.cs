// -----------------------------------------------------------------------
// <copyright file="TransportConfigurationGuardTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Core.Messaging;
using Xunit;

namespace Karamchari.Core.UnitTests.Messaging;

/// <summary>
/// Certification finding C-4: a non-Development host must never silently run on the
/// in-memory transport. These tests pin the startup-time fail-fast contract.
/// </summary>
public sealed class TransportConfigurationGuardTests
{
    private const string RabbitMq = "amqp://guest:guest@broker:5672";
    private const string ServiceBus = "Endpoint=sb://example.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=v";

    [Fact]
    public void Production_WithNoBroker_Throws()
    {
        var act = () => TransportConfigurationGuard.EnsureConfigured(
            isDevelopment: false, rabbitMqConnectionString: null, serviceBusConnectionString: null);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No production message transport configured*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Production_WithBlankBroker_Throws(string blank)
    {
        var act = () => TransportConfigurationGuard.EnsureConfigured(
            isDevelopment: false, rabbitMqConnectionString: blank, serviceBusConnectionString: blank);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Production_WithRabbitMq_DoesNotThrow()
    {
        var act = () => TransportConfigurationGuard.EnsureConfigured(
            isDevelopment: false, rabbitMqConnectionString: RabbitMq, serviceBusConnectionString: null);

        act.Should().NotThrow();
    }

    [Fact]
    public void Production_WithAzureServiceBus_DoesNotThrow()
    {
        var act = () => TransportConfigurationGuard.EnsureConfigured(
            isDevelopment: false, rabbitMqConnectionString: null, serviceBusConnectionString: ServiceBus);

        act.Should().NotThrow();
    }

    [Fact]
    public void Development_WithNoBroker_IsAllowed()
    {
        var act = () => TransportConfigurationGuard.EnsureConfigured(
            isDevelopment: true, rabbitMqConnectionString: null, serviceBusConnectionString: null);

        act.Should().NotThrow();
    }
}
