// -----------------------------------------------------------------------
// <copyright file="SyntheticPlatformTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Core.Messaging.Tenant;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;
using Karamchari.Core.Observability.Tenant;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Karamchari.TenantIsolationCertification.Messaging;

public class SyntheticPlatformTests
{
    private const string TestSecret = "karamchari-internal-platform-secret-2026";

    private static IServiceCollection CreateBaseServices()
    {
        var services = new ServiceCollection();
        services.AddLogging(l => l.AddConsole());

        var config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);

        services.AddSingleton<ReplayProtectionService>();
        services.AddSingleton<TenantMetricsCollector>();
        services.AddSingleton<TenantActivitySource>();
        services.AddSingleton(new ExecutionContextSigner(TestSecret));
        services.AddSingleton(typeof(TenantConsumeFilter<>));
        services.AddSingleton(typeof(TenantPublishFilter<>));
        services.AddSingleton<TenantPublishFilter>();
        return services;
    }

    [Fact]
    public async Task SyntheticFlow_ShouldPreserveContext_OutsideOfDomainContexts()
    {
        // Arrange
        SyntheticConsumer.Reset();
        await using var provider = CreateBaseServices()
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<SyntheticConsumer>();

                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseConsumeFilter(typeof(TenantConsumeFilter<>), context);
                    cfg.UsePublishFilter(typeof(TenantPublishFilter<>), context);
                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider();

        var harness = provider.GetRequiredService<ITestHarness>();
        harness.TestTimeout = TimeSpan.FromSeconds(15);
        await harness.Start();

        var tenantId = "synthetic-tenant";
        var correlationId = "synthetic-corr-123";
        var envelope = new TenantExecutionEnvelope(tenantId, correlationId, "req-ping", ExecutionSource.HttpRequest, TenantSource.JwtClaim);
        var context = new TenantExecutionContext(envelope);

        // Act
        using (context.Establish())
        {
            await harness.Bus.Publish(new SyntheticPingIntegrationEvent(
                Guid.NewGuid(),
                tenantId,
                "Ping Payload"));
        }

        // Assert
        (await harness.Consumed.Any<SyntheticPingIntegrationEvent>()).Should().BeTrue();

        SyntheticConsumer.LastObservedTenantId.Should().Be(tenantId);
        SyntheticConsumer.LastObservedCorrelationId.Should().Be(correlationId);
        SyntheticConsumer.WasContextEstablished.Should().BeTrue();
    }

    [Fact]
    public async Task TamperedMessage_ShouldTriggerSecurityWarning_AndReject()
    {
        // Arrange
        SyntheticConsumer.Reset();
        await using var provider = CreateBaseServices()
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<SyntheticConsumer>();
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseConsumeFilter(typeof(TenantConsumeFilter<>), context);
                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider();

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        // Act - Tamper with TenantId but provide an invalid signature
        await harness.Bus.Publish(new SyntheticPingIntegrationEvent(Guid.NewGuid(), "evil-tenant", "Tamper"), context =>
        {
            context.Headers.Set(ExecutionContextHeaders.TenantId, "evil-tenant");
            context.Headers.Set(ExecutionContextHeaders.ExecutionContextSignature, "totally-wrong-signature");
        });

        // Assert
        (await harness.Consumed.Any<SyntheticPingIntegrationEvent>()).Should().BeTrue();

        var consumed = harness.Consumed.Select<SyntheticPingIntegrationEvent>().First();
        consumed.Exception.Should().NotBeNull("Tampered message must be rejected by the filter.");
        consumed.Exception.Should().BeOfType<MalformedTenantMessageException>();

        SyntheticConsumer.WasContextEstablished.Should().BeFalse("Consumer must NOT execute for tampered messages.");
    }

    [Fact]
    public async Task KeyRotation_ShouldBeSeamless_WhenPreviousKeyIsRetained()
    {
        // Arrange
        var oldSecret = "old-secret-2025";
        var currentSecret = "new-secret-2026";

        SyntheticConsumer.Reset();
        await using var provider = new ServiceCollection()
            .AddLogging(l => l.AddConsole())
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddSingleton<ReplayProtectionService>()
            .AddSingleton<TenantMetricsCollector>()
            .AddSingleton<TenantActivitySource>()
            // Signer configured with BOTH keys
            .AddSingleton(new ExecutionContextSigner(new[] { currentSecret, oldSecret }))
            .AddSingleton(typeof(TenantConsumeFilter<>))
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<SyntheticConsumer>();
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseConsumeFilter(typeof(TenantConsumeFilter<>), context);
                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider();

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var tenantId = "rotate-tenant";
        var correlationId = "rotate-corr";
        var conversationId = "rotate-conv";
        var sourceService = "OldProducer";
        var timestamp = DateTime.UtcNow.ToString("O");

        // Simulate a message signed with the OLD key
        var oldSigner = new ExecutionContextSigner(oldSecret);
        var oldSignature = oldSigner.Sign(tenantId, correlationId, conversationId, sourceService, timestamp);

        // Act - Publish message with OLD signature to consumer that knows BOTH keys
        await harness.Bus.Publish(new SyntheticPingIntegrationEvent(Guid.NewGuid(), tenantId, "Old Key Payload"), context =>
        {
            context.Headers.Set(ExecutionContextHeaders.TenantId, tenantId);
            context.Headers.Set(ExecutionContextHeaders.CorrelationId, correlationId);
            context.Headers.Set(ExecutionContextHeaders.ConversationId, conversationId);
            context.Headers.Set(ExecutionContextHeaders.SourceService, sourceService);
            context.Headers.Set(ExecutionContextHeaders.Timestamp, timestamp);
            context.Headers.Set(ExecutionContextHeaders.ExecutionContextSignature, oldSignature);
        });

        // Assert
        (await harness.Consumed.Any<SyntheticPingIntegrationEvent>()).Should().BeTrue();
        var consumed = harness.Consumed.Select<SyntheticPingIntegrationEvent>().First();
        consumed.Exception.Should().BeNull("Message signed with retained old key must be accepted.");
        SyntheticConsumer.WasContextEstablished.Should().BeTrue();
    }

    private class SyntheticConsumer : IConsumer<SyntheticPingIntegrationEvent>
    {
        public static string? LastObservedTenantId;
        public static string? LastObservedCorrelationId;
        public static bool WasContextEstablished;

        public static void Reset()
        {
            LastObservedTenantId = null;
            LastObservedCorrelationId = null;
            WasContextEstablished = false;
        }

        public Task Consume(ConsumeContext<SyntheticPingIntegrationEvent> context)
        {
            var executionContext = TenantExecutionContext.Current;
            if (executionContext != null)
            {
                LastObservedTenantId = executionContext.TenantId;
                LastObservedCorrelationId = executionContext.CorrelationId;
                WasContextEstablished = true;
            }
            return Task.CompletedTask;
        }
    }
}
