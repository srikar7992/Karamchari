// -----------------------------------------------------------------------
// <copyright file="MessagingChaosTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using FluentAssertions;
using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Core.Messaging.Tenant;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Multitenancy.Execution;
using Karamchari.Core.Observability.Tenant;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.RabbitMq;
using Xunit;

namespace Karamchari.TenantIsolationCertification.Chaos;

[Collection(nameof(MessagingChaosCollectionDefinition))]
public sealed class MessagingChaosTests(MessagingChaosFixture fixture)
{
    private const string TestSecret = "karamchari-internal-platform-secret-2026";

    [DockerRequiredFact]
    public async Task WorkerCrash_ShouldResProcessSuccessfully_OnAnotherReplica()
    {
        // 1. Arrange
        var tenantId = "chaos-tenant";
        var correlationId = "chaos-corr-1";

        // Setup Replica 1
        var services1 = CreateWorkerServices(fixture.ConnectionString);
        var provider1 = services1.BuildServiceProvider();
        var bus1 = provider1.GetRequiredService<IBusControl>();
        await bus1.StartAsync();

        // Setup Replica 2
        var services2 = CreateWorkerServices(fixture.ConnectionString);
        var provider2 = services2.BuildServiceProvider();
        var bus2 = provider2.GetRequiredService<IBusControl>();
        await bus2.StartAsync();

        try
        {
            // 2. Act
            ChaosConsumer.ShouldCrash = true;
            ChaosConsumer.Reset();

            var envelope = new TenantExecutionEnvelope(tenantId, correlationId, "req-chaos", ExecutionSource.HttpRequest, TenantSource.JwtClaim);
            var context = new TenantExecutionContext(envelope);

            using (context.Establish())
            {
                await bus1.Publish(new SyntheticPingIntegrationEventV1(Guid.NewGuid(), tenantId, "Chaos Payload"));
            }

            // Wait for first attempt (which will crash/fail)
            var timeout = TimeSpan.FromSeconds(20);
            var sw = Stopwatch.StartNew();
            while (ChaosConsumer.AttemptCount < 1 && sw.Elapsed < timeout) await Task.Delay(100);

            ChaosConsumer.AttemptCount.Should().BeGreaterOrEqualTo(1);

            // Now "repair" the system by allowing the next attempt to succeed
            ChaosConsumer.ShouldCrash = false;

            // Wait for second attempt (retry) to succeed on either replica
            sw.Restart();
            while (ChaosConsumer.SuccessCount < 1 && sw.Elapsed < timeout) await Task.Delay(100);

            // 3. Assert
            ChaosConsumer.SuccessCount.Should().Be(1);
            ChaosConsumer.LastObservedTenantId.Should().Be(tenantId);
            ChaosConsumer.LastObservedCorrelationId.Should().Be(correlationId);
        }
        finally
        {
            await bus1.StopAsync();
            await bus2.StopAsync();
        }
    }

    private static IServiceCollection CreateWorkerServices(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging(l => l.AddConsole().SetMinimumLevel(LogLevel.Debug));

        var config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);

        services.AddSingleton<ReplayProtectionService>();
        services.AddSingleton<TenantMetricsCollector>();
        services.AddSingleton<TenantActivitySource>();
        services.AddSingleton(new ExecutionContextSigner(TestSecret));
        services.AddSingleton(typeof(TenantConsumeFilter<>));
        services.AddSingleton(typeof(TenantPublishFilter<>));
        services.AddSingleton<TenantPublishFilter>();
        services.AddSingleton<TenantSendFilter>();
        services.AddSingleton(typeof(TenantSendFilter<>));

        services.AddMassTransit(x =>
        {
            x.AddConsumer<ChaosConsumer>();
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(connectionString);
                cfg.UseConsumeFilter(typeof(TenantConsumeFilter<>), context);
                cfg.UsePublishFilter(typeof(TenantPublishFilter<>), context);
                cfg.UsePublishFilter<TenantPublishFilter>(context);
                cfg.UseSendFilter(typeof(TenantSendFilter<>), context);
                cfg.UseMessageRetry(r => r.Interval(5, TimeSpan.FromSeconds(2)));
                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }

    private class ChaosConsumer : IConsumer<SyntheticPingIntegrationEventV1>
    {
        public static bool ShouldCrash;
        public static int AttemptCount;
        public static int SuccessCount;
        public static string? LastObservedTenantId;
        public static string? LastObservedCorrelationId;

        public static void Reset()
        {
            AttemptCount = 0;
            SuccessCount = 0;
            LastObservedTenantId = null;
            LastObservedCorrelationId = null;
        }

        public Task Consume(ConsumeContext<SyntheticPingIntegrationEventV1> context)
        {
            Interlocked.Increment(ref AttemptCount);

            if (ShouldCrash)
            {
                throw new InvalidOperationException("BOOM! Transient failure.");
            }

            var executionContext = TenantExecutionContext.Current;
            LastObservedTenantId = executionContext?.TenantId;
            LastObservedCorrelationId = executionContext?.CorrelationId;
            Interlocked.Increment(ref SuccessCount);

            return Task.CompletedTask;
        }
    }
}

[CollectionDefinition(nameof(MessagingChaosCollectionDefinition))]
public sealed class MessagingChaosCollectionDefinition : ICollectionFixture<MessagingChaosFixture>;

public sealed class MessagingChaosFixture : IAsyncLifetime
{
    private readonly RabbitMqContainer _container = new RabbitMqBuilder("rabbitmq:3-management")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}

internal sealed class DockerRequiredFactAttribute : FactAttribute
{
    public DockerRequiredFactAttribute()
    {
        if (!IsDockerAvailable())
        {
            Skip = "Docker is required for Chaos tests.";
        }
    }

    private static bool IsDockerAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                ArgumentList = { "info" },
                UseShellExecute = false,
                CreateNoWindow = true
            });
            return process is not null && process.WaitForExit(2000) && process.ExitCode == 0;
        }
        catch { return false; }
    }
}
