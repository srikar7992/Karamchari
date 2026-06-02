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

namespace Karamchari.TenantIsolationCertification.Chaos;

public class IncidentSimulationTests
{
    private const string TestSecret = "karamchari-internal-platform-secret-2026";

    private static IServiceCollection CreateSimulationServices(string validationMode = "Enforce")
    {
        var services = new ServiceCollection();
        services.AddLogging(l => l.AddConsole());

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Messaging:ExecutionContextValidationMode"] = validationMode
            })
            .Build();

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
    public async Task IncidentA_KeyCompromise_EmergencyBypass_ShouldAllowContinuity()
    {
        // 1. Arrange - System is in AuditOnly mode (Bypass)
        SyntheticConsumer.Reset();
        await using var provider = CreateSimulationServices(validationMode: "AuditOnly")
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

        var tenantId = "bypass-tenant";

        // 2. Act - Publish message with INVALID signature (simulating compromised/stale key)
        await harness.Bus.Publish(new SyntheticPingIntegrationEvent(Guid.NewGuid(), tenantId, "Bypass Test"), context =>
        {
            context.Headers.Set(ExecutionContextHeaders.TenantId, tenantId);
            context.Headers.Set(ExecutionContextHeaders.ExecutionContextSignature, "COMPROMISED-KEY-SIGNATURE");
        });

        // 3. Assert
        (await harness.Consumed.Any<SyntheticPingIntegrationEvent>()).Should().BeTrue();

        // Consumer SHOULD run because we are in AuditOnly mode
        SyntheticConsumer.ConsumeCount.Should().Be(1, "Emergency Bypass must allow processing even with invalid signatures.");
        SyntheticConsumer.LastObservedTenantId.Should().Be(tenantId);
    }

    [Fact]
    public async Task IncidentC_ConsumerRejection_ShouldRouteToDLQ()
    {
        // 1. Arrange - System is in Enforce mode
        SyntheticConsumer.Reset();
        await using var provider = CreateSimulationServices(validationMode: "Enforce")
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

        // 2. Act - Tamper with message
        await harness.Bus.Publish(new SyntheticPingIntegrationEvent(Guid.NewGuid(), "acme", "DLQ Test"), context =>
        {
            context.Headers.Set(ExecutionContextHeaders.TenantId, "evil-tenant");
            context.Headers.Set(ExecutionContextHeaders.ExecutionContextSignature, "invalid");
        });

        // 3. Assert
        // We wait for the message to be received by the endpoint
        await Task.Delay(2000);

        // Consumer SHOULD NOT run
        SyntheticConsumer.ConsumeCount.Should().Be(0, "Enforce mode must block tampered messages.");

        // Verification: The harness should show a failed consumption
        (await harness.Consumed.Any<SyntheticPingIntegrationEvent>(x => x.Exception != null)).Should().BeTrue("Tampered message must result in a failed consumption entry.");
    }

    private class SyntheticConsumer : IConsumer<SyntheticPingIntegrationEvent>
    {
        public static string? LastObservedTenantId;
        public static int ConsumeCount;

        public static void Reset()
        {
            LastObservedTenantId = null;
            ConsumeCount = 0;
        }

        public Task Consume(ConsumeContext<SyntheticPingIntegrationEvent> context)
        {
            var executionContext = TenantExecutionContext.Current;
            LastObservedTenantId = executionContext?.TenantId;
            Interlocked.Increment(ref ConsumeCount);
            return Task.CompletedTask;
        }
    }
}
