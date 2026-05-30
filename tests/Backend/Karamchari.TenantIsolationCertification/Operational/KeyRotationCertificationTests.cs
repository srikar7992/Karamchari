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

namespace Karamchari.TenantIsolationCertification.Operational;

public class KeyRotationCertificationTests
{
    private const string OldSecret = "karamchari-old-secret-2025";
    private const string NewSecret = "karamchari-new-secret-2026";

    private static IServiceCollection CreateRotationServices(IEnumerable<string> secrets)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        
        var config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton<ReplayProtectionService>();
        services.AddSingleton<TenantMetricsCollector>();
        services.AddSingleton<TenantActivitySource>();
        services.AddSingleton(new ExecutionContextSigner(secrets));
        services.AddSingleton(typeof(TenantConsumeFilter<>));
        services.AddSingleton(typeof(TenantPublishFilter<>));
        return services;
    }

    [Fact]
    public async Task KeyRotation_DualKeyWindow_ShouldAllowInFlightMessages()
    {
        // 1. Arrange - System is in Dual-Key Mode (Old + New)
        SyntheticConsumer.Reset();
        await using var provider = CreateRotationServices(new[] { NewSecret, OldSecret })
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

        var signerOld = new ExecutionContextSigner(OldSecret);
        var signerNew = new ExecutionContextSigner(NewSecret);

        // 2. Act - Publish message signed with OLD key (simulating in-flight or cached producer)
        var tenantId = "rotation-test";
        var correlationId = Guid.NewGuid().ToString();
        var conversationId = Guid.NewGuid().ToString();
        var sourceService = "OldService";
        var timestamp = DateTime.UtcNow.ToString("O");
        
        var signatureOld = signerOld.Sign(tenantId, correlationId, conversationId, sourceService, timestamp);

        await harness.Bus.Publish(new SyntheticPingIntegrationEvent(Guid.NewGuid(), tenantId, "Old Key Message"), context =>
        {
            context.Headers.Set(ExecutionContextHeaders.TenantId, tenantId);
            context.Headers.Set(ExecutionContextHeaders.CorrelationId, correlationId);
            context.Headers.Set(ExecutionContextHeaders.ConversationId, conversationId);
            context.Headers.Set(ExecutionContextHeaders.SourceService, sourceService);
            context.Headers.Set(ExecutionContextHeaders.Timestamp, timestamp);
            context.Headers.Set(ExecutionContextHeaders.ExecutionContextSignature, signatureOld);
        });

        // 3. Assert - Should be SUCCESS because OldSecret is in the validation list
        (await harness.Consumed.Any<SyntheticPingIntegrationEvent>()).Should().BeTrue();
        SyntheticConsumer.ConsumeCount.Should().Be(1);

        // 4. Act - Publish message signed with NEW key
        var signatureNew = signerNew.Sign(tenantId, correlationId, conversationId, sourceService, timestamp);
        await harness.Bus.Publish(new SyntheticPingIntegrationEvent(Guid.NewGuid(), tenantId, "New Key Message"), context =>
        {
            context.Headers.Set(ExecutionContextHeaders.TenantId, tenantId);
            context.Headers.Set(ExecutionContextHeaders.CorrelationId, correlationId);
            context.Headers.Set(ExecutionContextHeaders.ConversationId, conversationId);
            context.Headers.Set(ExecutionContextHeaders.SourceService, sourceService);
            context.Headers.Set(ExecutionContextHeaders.Timestamp, timestamp);
            context.Headers.Set(ExecutionContextHeaders.ExecutionContextSignature, signatureNew);
        });

        // 5. Assert - Should be SUCCESS
        (await harness.Consumed.Any<SyntheticPingIntegrationEvent>(x => x.Context.Message.Payload == "New Key Message")).Should().BeTrue();
        SyntheticConsumer.ConsumeCount.Should().Be(2);
    }

    [Fact]
    public async Task KeyRotation_FinalCleanup_ShouldRejectOldKey()
    {
        // 1. Arrange - System is in Single-Key Mode (New Only, Old removed)
        SyntheticConsumer.Reset();
        await using var provider = CreateRotationServices(new[] { NewSecret })
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

        var signerOld = new ExecutionContextSigner(OldSecret);

        // 2. Act - Publish message signed with OLD key
        var tenantId = "cleanup-test";
        var signatureOld = signerOld.Sign(tenantId, Guid.NewGuid().ToString(), Guid.NewGuid().ToString(), "OldService", DateTime.UtcNow.ToString("O"));

        await harness.Bus.Publish(new SyntheticPingIntegrationEvent(Guid.NewGuid(), tenantId, "Rejected Message"), context =>
        {
            context.Headers.Set(ExecutionContextHeaders.TenantId, tenantId);
            context.Headers.Set(ExecutionContextHeaders.ExecutionContextSignature, signatureOld);
            context.Headers.Set(ExecutionContextHeaders.Timestamp, DateTime.UtcNow.ToString("O"));
        });

        // 3. Assert - Should FAIL because OldSecret is no longer valid
        await Task.Delay(1000);
        SyntheticConsumer.ConsumeCount.Should().Be(0);
        (await harness.Consumed.Any<SyntheticPingIntegrationEvent>(x => x.Exception != null)).Should().BeTrue();
    }

    private class SyntheticConsumer : IConsumer<SyntheticPingIntegrationEvent>
    {
        public static int ConsumeCount;

        public static void Reset()
        {
            ConsumeCount = 0;
        }

        public Task Consume(ConsumeContext<SyntheticPingIntegrationEvent> context)
        {
            Interlocked.Increment(ref ConsumeCount);
            return Task.CompletedTask;
        }
    }
}
