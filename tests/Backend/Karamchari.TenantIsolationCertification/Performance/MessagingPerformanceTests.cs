// -----------------------------------------------------------------------
// <copyright file="MessagingPerformanceTests.cs" company="Karamchari">
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
using MassTransit.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace Karamchari.TenantIsolationCertification.Performance;

public class MessagingPerformanceTests(ITestOutputHelper output)
{
    private const string TestSecret = "karamchari-internal-platform-secret-2026";

    [Theory]
    [InlineData(100)]
    [InlineData(1000)]
    public async Task BenchmarkExecutionContextOverhead(int messageCount)
    {
        // 1. Baseline: No Preservation Filters
        var baselineTime = await MeasurePublishThroughputAsync(messageCount, useFilters: false);

        // 2. Optimized: With Context Preservation Filters
        var optimizedTime = await MeasurePublishThroughputAsync(messageCount, useFilters: true);

        // 3. Analysis
        var diff = optimizedTime - baselineTime;
        var perMessageOverhead = diff.TotalMilliseconds / messageCount;
        var percentageIncrease = baselineTime.TotalMilliseconds > 0
            ? (optimizedTime.TotalMilliseconds / baselineTime.TotalMilliseconds - 1) * 100
            : 0;

        output.WriteLine($"Benchmark for {messageCount} messages:");
        output.WriteLine($"  Baseline Time:  {baselineTime.TotalMilliseconds:F2}ms");
        output.WriteLine($"  Preserved Time: {optimizedTime.TotalMilliseconds:F2}ms");
        output.WriteLine($"  Total Overhead: {diff.TotalMilliseconds:F2}ms");
        output.WriteLine($"  Per-Msg Cost:   {perMessageOverhead:F4}ms");
        output.WriteLine($"  Relative Cost:  {percentageIncrease:F2}%");

        // Assert reasonable performance. Threshold is 15ms to tolerate parallel test execution noise
        // (both baseline and filtered runs share CPU during pre-push). Still catches gross regressions.
        perMessageOverhead.Should().BeLessThan(15.0, "Infrastructure overhead per message should be negligible.");
    }

    private static async Task<TimeSpan> MeasurePublishThroughputAsync(int count, bool useFilters)
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder().Build();
        services.AddSingleton<IConfiguration>(config);

        services.AddSingleton<ReplayProtectionService>();
        services.AddSingleton<TenantMetricsCollector>();
        services.AddSingleton<TenantActivitySource>();
        services.AddSingleton(new ExecutionContextSigner(TestSecret));
        services.AddSingleton(typeof(TenantPublishFilter<>));
        services.AddSingleton<TenantPublishFilter>();

        services.AddMassTransitTestHarness(x =>
        {
            x.UsingInMemory((context, cfg) =>
            {
                if (useFilters)
                {
                    cfg.UsePublishFilter(typeof(TenantPublishFilter<>), context);
                    cfg.UsePublishFilter<TenantPublishFilter>(context);
                }
            });
        });

        await using var provider = services.BuildServiceProvider();

        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();

        var tenantId = "perf-tenant";
        var envelope = new TenantExecutionEnvelope(tenantId, "corr-1", "req-1", ExecutionSource.HttpRequest, TenantSource.JwtClaim);
        var context = new TenantExecutionContext(envelope);

        var sw = Stopwatch.StartNew();

        using (context.Establish())
        {
            for (int i = 0; i < count; i++)
            {
                await harness.Bus.Publish(new SyntheticPingIntegrationEventV1(Guid.NewGuid(), tenantId, "Perf Payload"));
            }
        }

        sw.Stop();
        return sw.Elapsed;
    }
}
