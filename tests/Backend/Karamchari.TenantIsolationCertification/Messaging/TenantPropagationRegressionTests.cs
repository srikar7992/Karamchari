// -----------------------------------------------------------------------
// <copyright file="TenantPropagationRegressionTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using FluentAssertions;
using Karamchari.Core.Contracts.IntegrationEvents.V1;
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

public class TenantPropagationRegressionTests
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
    public async Task PublishedMessage_ShouldContainSignedTenantHeaders()
    {
        // Arrange
        await using var provider = CreateBaseServices()
            .AddMassTransitTestHarness(x =>
            {
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UsePublishFilter(typeof(TenantPublishFilter<>), context);
                });
            })
            .BuildServiceProvider();

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var tenantId = "acme";
        var correlationId = Guid.NewGuid().ToString();
        var envelope = new TenantExecutionEnvelope(tenantId, correlationId, "req-1", ExecutionSource.HttpRequest, TenantSource.JwtClaim);
        var context = new TenantExecutionContext(envelope);

        using (context.Establish())
        {
            // Act
            await harness.Bus.Publish(new EmployeeOnboardedIntegrationEventV1(
                Guid.NewGuid(),
                tenantId,
                "EMP001",
                "John Doe",
                "john@acme.com",
                DateOnly.FromDateTime(DateTime.Today)));
        }

        // Assert
        var published = (await harness.Published.SelectAsync<EmployeeOnboardedIntegrationEventV1>().First()).Context;
        published.Headers.Get<string>(ExecutionContextHeaders.TenantId).Should().Be(tenantId);
        published.Headers.Get<string>(ExecutionContextHeaders.CorrelationId).Should().Be(correlationId);
        published.Headers.Get<string>(ExecutionContextHeaders.ExecutionEnvelope).Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ConsumedMessage_ShouldRestoreContext_WhenSignatureIsValid()
    {
        // Arrange
        TestConsumer.Reset();
        await using var provider = CreateBaseServices()
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<TestConsumer>();

                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseConsumeFilter(typeof(TenantConsumeFilter<>), context);
                    cfg.UsePublishFilter(typeof(TenantPublishFilter<>), context);
                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider();

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var tenantId = "globex";
        var correlationId = Guid.NewGuid().ToString();
        var envelope = new TenantExecutionEnvelope(tenantId, correlationId, "req-1", ExecutionSource.HttpRequest, TenantSource.JwtClaim);
        var context = new TenantExecutionContext(envelope);

        // Act
        using (context.Establish())
        {
            await harness.Bus.Publish(new EmployeeOnboardedIntegrationEventV1(
                Guid.NewGuid(),
                tenantId,
                "EMP-G01",
                "Alice Globex",
                "alice@globex.com",
                DateOnly.FromDateTime(DateTime.Today)));
        }

        // Assert
        (await harness.Consumed.Any<EmployeeOnboardedIntegrationEventV1>()).Should().BeTrue();
        TestConsumer.LastObservedTenantId.Should().Be(tenantId);
    }

    [Theory]
    [InlineData("evil-tenant", "orig-corr", "orig-conv", "orig-src", "Tamper Tenant")]
    [InlineData("acme", "evil-corr", "orig-conv", "orig-src", "Tamper Correlation")]
    [InlineData("acme", "orig-corr", "evil-conv", "orig-src", "Tamper Conversation")]
    [InlineData("acme", "orig-corr", "orig-conv", "evil-src", "Tamper Source")]
    public async Task ConsumedMessage_ShouldReject_WhenHeadersAreTampered(
        string tenantId, string correlationId, string conversationId, string sourceService, string testName)
    {
        // Arrange
        await using var provider = CreateBaseServices()
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<TestConsumer>();
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseConsumeFilter(typeof(TenantConsumeFilter<>), context);
                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider();

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var timestamp = DateTime.UtcNow.ToString("O");
        var signer = provider.GetRequiredService<ExecutionContextSigner>();

        // Generate valid signature for ORIGINAL values
        var validSignature = signer.Sign("acme", "orig-corr", "orig-conv", "orig-src", timestamp);

        // Act - Publish with TAMPERED values but ORIGINAL signature
        await harness.Bus.Publish(new EmployeeOnboardedIntegrationEventV1(
            Guid.NewGuid(), tenantId, "EMP-X", testName, "t@acme.com", DateOnly.FromDateTime(DateTime.Today)),
            context =>
            {
                context.Headers.Set(ExecutionContextHeaders.TenantId, tenantId);
                context.Headers.Set(ExecutionContextHeaders.CorrelationId, correlationId);
                context.Headers.Set(ExecutionContextHeaders.ConversationId, conversationId);
                context.Headers.Set(ExecutionContextHeaders.SourceService, sourceService);
                context.Headers.Set(ExecutionContextHeaders.Timestamp, timestamp);
                context.Headers.Set(ExecutionContextHeaders.ExecutionContextSignature, validSignature);
            });

        // Assert
        await Task.Delay(1000);
        // Signature failures result in failed consumption
        (await harness.Consumed.Any<EmployeeOnboardedIntegrationEventV1>(x => x.Exception != null)).Should().BeTrue($"Tamper scenario '{testName}' must be rejected.");
    }

    [Fact]
    public async Task MultiTenantStressTest_ShouldMaintainIsolation()
    {
        // Arrange
        StressConsumer.Reset();
        await using var provider = CreateBaseServices()
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<StressConsumer>();
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseConsumeFilter(typeof(TenantConsumeFilter<>), context);
                    cfg.UsePublishFilter(typeof(TenantPublishFilter<>), context);
                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider();

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var tenants = new[] { "acme", "globex", "initech" };
        var countPerTenant = 10;
        var totalCount = tenants.Length * countPerTenant;

        // Act
        var tasks = tenants.SelectMany(tenantId =>
            Enumerable.Range(0, countPerTenant).Select(async i =>
            {
                var correlationId = $"{tenantId}-{i}";
                var envelope = new TenantExecutionEnvelope(tenantId, correlationId, $"req-{i}", ExecutionSource.HttpRequest, TenantSource.JwtClaim);
                var context = new TenantExecutionContext(envelope);

                using (context.Establish())
                {
                    await harness.Bus.Publish(new EmployeeOnboardedIntegrationEventV1(
                        Guid.NewGuid(),
                        tenantId,
                        $"EMP-{tenantId}-{i}",
                        $"Name {tenantId} {i}",
                        $"{i}@{tenantId}.com",
                        DateOnly.FromDateTime(DateTime.Today)));
                }
            })).ToList();

        await Task.WhenAll(tasks);

        // Assert
        // Wait for all messages to be consumed
        var timeout = TimeSpan.FromSeconds(30);
        var stopwatch = Stopwatch.StartNew();
        while (StressConsumer.TotalCount < totalCount && stopwatch.Elapsed < timeout)
        {
            await Task.Delay(100);
        }

        StressConsumer.TotalCount.Should().Be(totalCount);
        StressConsumer.IsolatedCount.Should().Be(totalCount);
        StressConsumer.LeakCount.Should().Be(0);
    }

    [Fact]
    public async Task ReplayedMessage_ShouldBeRejected_OnSecondAttempt()
    {
        // Arrange
        TestConsumer.Reset();
        await using var provider = CreateBaseServices()
            .AddMassTransitTestHarness(x =>
            {
                x.AddConsumer<TestConsumer>();
                x.UsingInMemory((context, cfg) =>
                {
                    cfg.UseConsumeFilter(typeof(TenantConsumeFilter<>), context);
                    cfg.UsePublishFilter(typeof(TenantPublishFilter<>), context);
                    cfg.ConfigureEndpoints(context);
                });
            })
            .BuildServiceProvider();

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var tenantId = "acme";
        var correlationId = Guid.NewGuid().ToString();
        var originalMessageId = Guid.NewGuid().ToString();
        var conversationId = Guid.NewGuid().ToString();
        var timestamp = DateTime.UtcNow.ToString("O");
        var sourceService = "Outbox";

        var signer = provider.GetRequiredService<ExecutionContextSigner>();
        var signature = signer.Sign(tenantId, correlationId, conversationId, sourceService, timestamp);

        var publishAction = (Guid actualMessageId) => harness.Bus.Publish(new EmployeeOnboardedIntegrationEventV1(
            Guid.NewGuid(), tenantId, "EMP-R01", "Replay Test", "r@a.com", DateOnly.FromDateTime(DateTime.Today)),
            context =>
            {
                context.MessageId = actualMessageId;
                context.Headers.Set(ExecutionContextHeaders.TenantId, tenantId);
                context.Headers.Set(ExecutionContextHeaders.CorrelationId, correlationId);
                context.Headers.Set(ExecutionContextHeaders.ConversationId, conversationId);
                context.Headers.Set(ExecutionContextHeaders.SourceService, sourceService);
                context.Headers.Set(ExecutionContextHeaders.Timestamp, timestamp);
                context.Headers.Set(ExecutionContextHeaders.ExecutionContextSignature, signature);
                context.Headers.Set(ExecutionContextHeaders.OriginalMessageId, originalMessageId);
            });

        // Act 1 - First attempt should pass
        await publishAction(Guid.NewGuid());
        await Task.Delay(1000);
        (await harness.Consumed.Any<EmployeeOnboardedIntegrationEventV1>()).Should().BeTrue();
        TestConsumer.ConsumeCount.Should().Be(1);

        // Act 2 - Second attempt (DIFFERENT MessageId, SAME OriginalMessageId) should be rejected
        await publishAction(Guid.NewGuid());

        // Wait a bit
        await Task.Delay(1000);

        // Assert
        TestConsumer.ConsumeCount.Should().Be(1, "Second attempt with same OriginalMessageId should have been blocked.");
        (await harness.Consumed.Any<EmployeeOnboardedIntegrationEventV1>(x => x.Exception != null)).Should().BeTrue("Replayed message must result in a failed consumption entry.");
    }

    private class TestConsumer : IConsumer<EmployeeOnboardedIntegrationEventV1>
    {
        public static string? LastObservedTenantId { get; set; }
        public static int ConsumeCount;

        public static void Reset()
        {
            LastObservedTenantId = null;
            ConsumeCount = 0;
        }

        public Task Consume(ConsumeContext<EmployeeOnboardedIntegrationEventV1> context)
        {
            var executionContext = TenantExecutionContext.Current;
            LastObservedTenantId = executionContext?.TenantId;
            Interlocked.Increment(ref ConsumeCount);
            return Task.CompletedTask;
        }
    }

    private class StressConsumer : IConsumer<EmployeeOnboardedIntegrationEventV1>
    {
        public static int TotalCount;
        public static int IsolatedCount;
        public static int LeakCount;

        public static void Reset()
        {
            TotalCount = 0;
            IsolatedCount = 0;
            LeakCount = 0;
        }

        public async Task Consume(ConsumeContext<EmployeeOnboardedIntegrationEventV1> context)
        {
            Interlocked.Increment(ref TotalCount);
            var message = context.Message;
            var executionContext = TenantExecutionContext.Current;

            // Artificial delay
            await Task.Delay(Random.Shared.Next(1, 10));

            if (executionContext?.TenantId == message.TenantId)
            {
                Interlocked.Increment(ref IsolatedCount);
            }
            else
            {
                Interlocked.Increment(ref LeakCount);
            }
        }
    }
}
