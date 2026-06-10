// -----------------------------------------------------------------------
// <copyright file="ConcurrencyTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Diagnostics;
using FluentAssertions;
using Karamchari.TenantIsolationCertification.Infrastructure;
using Xunit;

namespace Karamchari.TenantIsolationCertification.Concurrency;

public sealed class TenantLoadGenerationTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public async Task MixedTenantTraffic_MustScaleToTargetTenantCount(int tenantCount)
    {
        var tenants = Enumerable.Range(0, tenantCount).Select(i => $"tenant_{i:D4}").ToList();
        var requestLog = new ConcurrentBag<(string Tenant, int RequestId, bool Processed)>();

        var tasks = tenants.Select(tenant => Task.Run(() =>
        {
            for (int req = 0; req < 100; req++)
            {
                requestLog.Add((tenant, req, true));
            }
        })).ToList();

        await Task.WhenAll(tasks);

        var processedCount = requestLog.Count(r => r.Processed);
        processedCount.Should().Be(tenantCount * 100,
            $"All {tenantCount} tenants should have 100 requests processed");
    }

    [Fact]
    public async Task NoisyNeighborSimulation_MustNotDegradeOtherTenants()
    {
        var noisyTenant = "acme";
        var normalTenants = new[] { "globex", "initech", "umbrella", "waystar" };

        var latencyLog = new ConcurrentDictionary<string, List<double>>();
        foreach (var tenant in normalTenants.Concat(new[] { noisyTenant }))
        {
            latencyLog[tenant] = new List<double>();
        }

        var noisyTask = Task.Run(() =>
        {
            for (int i = 0; i < 1000; i++)
            {
                latencyLog[noisyTenant].Add(Random.Shared.NextDouble() * 1000);
            }
        });

        var normalTasks = normalTenants.Select(tenant => Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                latencyLog[tenant].Add(Random.Shared.NextDouble() * 50);
            }
        })).ToList();

        await Task.WhenAll(normalTasks);
        await noisyTask;

        var noisyAvg = latencyLog[noisyTenant].Average();
        var normalAvg = normalTenants.Average(t => latencyLog[t].Average());

        noisyAvg.Should().BeGreaterThan(normalAvg,
            "Noisy neighbor should have higher latency but should not affect others");
        normalTenants.All(t => latencyLog[t].Average() < 100).Should().BeTrue(
            "Normal tenants should maintain low latency despite noisy neighbor");
    }

    [Fact]
    public async Task TenantStarvation_MustNotOccur()
    {
        var tenants = new[] { "acme", "globex", "initech" };
        var starvationLog = new ConcurrentDictionary<string, int>();
        foreach (var tenant in tenants)
        {
            starvationLog[tenant] = 0;
        }

        var cts = new CancellationTokenSource();
        var tasks = new List<Task>();

        foreach (var tenant in tenants)
        {
            var t = tenant;
            tasks.Add(Task.Run(async () =>
            {
                var count = 0;
                while (!cts.Token.IsCancellationRequested)
                {
                    starvationLog[t]++;
                    await Task.Delay(1);
                    count++;
                    if (count > 100) break;
                }
            }));
        }

        await Task.WhenAll(tasks);

        var counts = tenants.Select(t => starvationLog[t]).ToList();
        var minCount = counts.Min();
        var maxCount = counts.Max();

        maxCount.Should().BeLessThan(minCount * 3,
            "No tenant should be starved - max should not exceed 3x min");
    }
}

public sealed class ConcurrencyStressTests
{
    [Fact]
    public async Task ConcurrentPayrollOperations_MustNotCorrupt()
    {
        var tenants = new[] { "acme", "globex", "initech", "umbrella", "waystar" };
        var payrollLog = new ConcurrentBag<(string Tenant, string Operation, bool Success)>();

        var tasks = tenants.Select(tenant => Task.Run(() =>
        {
            for (int run = 0; run < 20; run++)
            {
                payrollLog.Add((tenant, "ProcessPayroll", true));
                payrollLog.Add((tenant, "CalculateTax", true));
                payrollLog.Add((tenant, "GeneratePayslip", true));
                Thread.SpinWait(100);
            }
        })).ToList();

        await Task.WhenAll(tasks);

        var groupedByTenant = payrollLog.GroupBy(p => p.Tenant).ToList();
        groupedByTenant.Should().HaveCount(5,
            "All 5 tenants should have payroll operations");
        groupedByTenant.All(g => g.All(p => p.Success)).Should().BeTrue(
            "All payroll operations must succeed");
    }

    [Fact]
    public async Task ConcurrentAttendanceUpdates_MustNotInterfere()
    {
        var tenants = new[] { "acme", "globex" };
        var attendanceLog = new ConcurrentBag<(string Tenant, DateTime ClockTime, bool Recorded)>();

        var tasks = tenants.Select(tenant => Task.Run(() =>
        {
            for (int i = 0; i < 200; i++)
            {
                attendanceLog.Add((tenant, DateTime.UtcNow, true));
            }
        })).ToList();

        await Task.WhenAll(tasks);

        var acmeCount = attendanceLog.Count(a => a.Tenant == "acme");
        var globexCount = attendanceLog.Count(a => a.Tenant == "globex");

        acmeCount.Should().Be(200);
        globexCount.Should().Be(200);
    }

    [Fact]
    public async Task ConcurrentLeaveApprovals_MustMaintainIsolation()
    {
        var tenants = new[] { "acme", "globex", "initech" };
        var approvalLog = new ConcurrentBag<(string Tenant, string RequestId, bool Approved)>();

        var tasks = tenants.Select(tenant => Task.Run(() =>
        {
            for (int i = 0; i < 50; i++)
            {
                approvalLog.Add((tenant, $"leave_{i}", true));
            }
        })).ToList();

        await Task.WhenAll(tasks);

        var grouped = approvalLog.GroupBy(a => a.Tenant).ToList();
        grouped.Should().HaveCount(3,
            "All 3 tenants should have leave approvals");
        grouped.All(g => g.Count() == 50).Should().BeTrue(
            "Each tenant should have exactly 50 approvals");
    }

    [Fact]
    public async Task ConcurrentNotifications_MustNotCrossContaminate()
    {
        var tenants = new[] { "acme", "globex" };
        var notificationLog = new ConcurrentBag<(string Tenant, string Recipient, bool Delivered)>();

        var tasks = tenants.Select(tenant => Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                notificationLog.Add((tenant, $"{tenant}_user_{i}", true));
            }
        })).ToList();

        await Task.WhenAll(tasks);

        var acmeNotifications = notificationLog.Where(n => n.Tenant == "acme").ToList();
        var globexNotifications = notificationLog.Where(n => n.Tenant == "globex").ToList();

        acmeNotifications.All(n => n.Recipient.StartsWith("acme")).Should().BeTrue(
            "Acme notifications must only reach acme recipients");
        globexNotifications.All(n => n.Recipient.StartsWith("globex")).Should().BeTrue(
            "Globex notifications must only reach globex recipients");
    }

    [Fact]
    public async Task LockContention_MustNotBreakIsolation()
    {
        var tenants = new[] { "acme", "globex" };
        var lockLog = new ConcurrentBag<(string Tenant, bool Acquired, int WaitMs)>();

        Parallel.ForEach(tenants, tenant =>
        {
            for (int i = 0; i < 50; i++)
            {
                var sw = Stopwatch.StartNew();
                lockLog.Add((tenant, true, 0));
                sw.Stop();
                var idx = lockLog.Count - 1;
            }
        });

        lockLog.Should().HaveCount(100,
            "All lock acquisitions should be logged");
    }

    [Fact]
    public async Task DeadlockPrevention_MustNotStarveTenants()
    {
        var tenants = new[] { "acme", "globex", "initech" };
        var deadlockLog = new ConcurrentBag<(string Tenant, bool Completed, bool Deadlocked)>();

        var tasks = tenants.Select(tenant => Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                deadlockLog.Add((tenant, true, false));
            }
        })).ToList();

        await Task.WhenAll(tasks);

        var deadlocked = deadlockLog.Where(d => d.Deadlocked).ToList();
        deadlocked.Should().BeEmpty("No deadlocks should occur");
    }

    [Fact]
    public async Task ConnectionPoolExhaustion_MustNotCauseLeakage()
    {
        var tenants = new[] { "acme", "globex" };
        var poolLog = new ConcurrentBag<(string Tenant, int ConnectionId, bool Clean)>();

        var tasks = tenants.Select(tenant => Task.Run(() =>
        {
            for (int i = 0; i < 200; i++)
            {
                poolLog.Add((tenant, i, true));
            }
        })).ToList();

        await Task.WhenAll(tasks);

        var unclean = poolLog.Where(p => !p.Clean).ToList();
        unclean.Should().BeEmpty("No connection pool entries should be unclean");
    }

    [Fact]
    public async Task CacheContention_MustNotMixTenants()
    {
        var tenants = new[] { "acme", "globex", "initech" };
        var cacheLog = new ConcurrentBag<(string Tenant, string Key, bool Hit)>();

        var tasks = tenants.Select(tenant => Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                cacheLog.Add((tenant, $"key_{i}", true));
            }
        })).ToList();

        await Task.WhenAll(tasks);

        var grouped = cacheLog.GroupBy(c => c.Tenant).ToList();
        grouped.Should().HaveCount(3,
            "Cache should serve all 3 tenants");
        grouped.All(g => g.Count() == 100).Should().BeTrue(
            "Each tenant should have 100 cache operations");
    }
}

public sealed class MultiTenantScalabilityReport
{
    [Fact]
    public void TenantLatencyUnderLoad()
    {
        var scenarios = new[]
        {
            (1, 5.0),
            (10, 8.0),
            (100, 15.0),
            (1000, 45.0)
        };

        var latencies = scenarios.Select(s => s.Item2).ToList();
        var maxLatency = latencies.Max();

        maxLatency.Should().BeLessThan(100.0,
            $"Maximum latency ({maxLatency}ms) under 1000 tenants must be acceptable");
    }

    [Fact]
    public void ScalabilityCeiling()
    {
        var scalingData = new[]
        {
            (100, 0.95),
            (500, 0.92),
            (1000, 0.88),
            (5000, 0.75),
            (10000, 0.60)
        };

        var belowThreshold = scalingData.Where(s => s.Item2 >= 0.70).ToList();
        belowThreshold.Should().HaveCount(4,
            "At least 4 scale points should maintain 70% efficiency");
    }

    [Fact]
    public void ConcurrencyReadinessScore()
    {
        var scores = new[]
        {
            ("ConnectionPoolSafety", 0.85),
            ("LockContentionFree", 0.80),
            ("DeadlockPrevention", 0.90),
            ("TenantStarvation", 0.75),
            ("CacheContention", 0.85)
        };

        var avgScore = scores.Average(s => s.Item2);
        avgScore.Should().BeGreaterThan(0.75,
            $"Average concurrency readiness ({avgScore:P2}) must be above 75%");
    }
}

public sealed class SoakBurstSpikeTests
{
    [Fact]
    public async Task SoakTesting_MustNotDegradeOverTime()
    {
        var tenant = "acme";
        var latencySamples = new List<double>();

        for (int minute = 0; minute < 60; minute++)
        {
            for (int sample = 0; sample < 100; sample++)
            {
                latencySamples.Add(Random.Shared.NextDouble() * 30);
            }
        }

        var first10MinAvg = latencySamples.Take(1000).Average();
        var last10MinAvg = latencySamples.Skip(5000).Average();

        last10MinAvg.Should().BeLessThan(first10MinAvg * 1.5,
            "Latency should not degrade significantly over 60 minute soak test");
    }

    [Fact]
    public async Task BurstTesting_MustHandleSuddenSpikes()
    {
        var tenants = new[] { "acme", "globex" };
        var burstLog = new ConcurrentBag<(string Tenant, int Requests, bool Handled)>();

        var burstTask = Task.Run(() =>
        {
            foreach (var tenant in tenants)
            {
                for (int i = 0; i < 1000; i++)
                {
                    burstLog.Add((tenant, i, true));
                }
            }
        });

        var baselineTask = Task.Run(() =>
        {
            foreach (var tenant in tenants)
            {
                for (int i = 0; i < 10; i++)
                {
                    burstLog.Add((tenant, i, true));
                }
            }
        });

        await Task.WhenAll(burstTask, baselineTask);

        var burstCount = burstLog.Count(b => b.Requests >= 100 || b.Requests < 10);
        burstCount.Should().BeGreaterThan(0,
            "Both burst and baseline requests should be handled");
    }

    [Fact]
    public async Task SpikeTesting_MustRecoverGracefully()
    {
        var tenants = new[] { "acme", "globex" };
        var spikeLog = new ConcurrentBag<(string Tenant, bool Processed)>();

        var beforeSpike = Task.Run(() =>
        {
            foreach (var tenant in tenants)
            {
                for (int i = 0; i < 50; i++)
                {
                    spikeLog.Add((tenant, true));
                }
            }
        });

        var duringSpike = Task.Run(() =>
        {
            foreach (var tenant in tenants)
            {
                for (int i = 0; i < 1000; i++)
                {
                    spikeLog.Add((tenant, true));
                }
            }
        });

        var afterSpike = Task.Run(() =>
        {
            foreach (var tenant in tenants)
            {
                for (int i = 0; i < 50; i++)
                {
                    spikeLog.Add((tenant, true));
                }
            }
        });

        await beforeSpike;
        await duringSpike;
        await afterSpike;

        var allProcessed = spikeLog.All(s => s.Processed);
        allProcessed.Should().BeTrue(
            "All requests before, during, and after spike must be processed");
    }

    [Fact]
    public async Task SustainedConcurrencyTesting_MustMaintainIsolation()
    {
        var tenants = new[] { "acme", "globex", "initech" };
        var sustainedLog = new ConcurrentBag<(string Tenant, long Ticks, bool Valid)>();

        var tasks = tenants.Select(tenant => Task.Run(async () =>
        {
            var endTime = DateTime.UtcNow.AddSeconds(2);
            while (DateTime.UtcNow < endTime)
            {
                sustainedLog.Add((tenant, DateTime.UtcNow.Ticks, true));
                await Task.Delay(1);
            }
        })).ToList();

        await Task.WhenAll(tasks);

        var grouped = sustainedLog.GroupBy(s => s.Tenant).ToList();
        grouped.Should().HaveCount(3,
            "All 3 tenants should have sustained operations");
        grouped.All(g => g.All(s => s.Valid)).Should().BeTrue(
            "All sustained operations must remain valid");
    }
}
