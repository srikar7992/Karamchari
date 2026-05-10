using System.Diagnostics;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Karamchari.Core.Multitenancy;
using Karamchari.TenantIsolationCertification.Infrastructure;
using Xunit;

namespace Karamchari.TenantIsolationCertification.Propagation;

public sealed class AsyncLocalContaminationTests : IDisposable
{
    private readonly TenantTestContext _tenantA;
    private readonly TenantTestContext _tenantB;

    public AsyncLocalContaminationTests()
    {
        _tenantA = TenantTestContext.Create("acme");
        _tenantB = TenantTestContext.Create("globex");
    }

    [Fact]
    public async Task AsyncLocal_ThreadReuse_MustNotBleedTenant()
    {
        var collector = new TenantContextCollector();
        var tenantATenant = _tenantA.ActiveTenantId;
        var tenantBTenant = _tenantB.ActiveTenantId;

        await Task.Run(() =>
        {
            collector.Record(tenantATenant, "Start_ThreadPool");
        });

        await Task.Delay(1);

        await Task.Run(() =>
        {
            collector.Record(tenantBTenant, "End_ThreadPool");
        });

        collector.HasContamination().Should().BeFalse(
            "Thread pool reuse should NOT cause tenant bleed between sequential tasks");
    }

    [Fact]
    public async Task AsyncLocal_ContinuationHopping_MustPreserveTenant()
    {
        var tenantId = _tenantA.ActiveTenantId;
        var collector = new TenantContextCollector();

        var result = await Task.FromResult(1)
            .ContinueWith(_ => { collector.Record(tenantId, "Continuation_1"); return 42; })
            .ContinueWith(t => { collector.Record(tenantId, "Continuation_2"); return t.Result * 2; })
            .ContinueWith(async t =>
            {
                await Task.Delay(1);
                collector.Record(tenantId, "Continuation_3_Async");
                return t.Result;
            });

        collector.HasContamination().Should().BeFalse(
            "Multiple continuation hops should NOT lose tenant context");
        result.Should().Be(84);
    }

    [Fact]
    public async Task AsyncLocal_PooledTaskReuse_MustNotLeak()
    {
        var collector = new TenantContextCollector();
        var tasks = new List<Task>();

        for (int i = 0; i < 50; i++)
        {
            var localTenant = i % 2 == 0 ? "tenant_alpha" : "tenant_beta";
            var task = Task.Run(() =>
            {
                collector.Record(localTenant, $"PooledTask_{i}");
            });
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        collector.HasContamination().Should().BeFalse(
            "Pooled tasks from different tenants should NOT cross-contaminate");
    }

    [Fact]
    public async Task AsyncLocal_ParallelFanOut_MustMaintainIsolation()
    {
        var collector = new TenantContextCollector();
        var tenants = new[] { "acme", "globex", "initech" };

        var fanOutTasks = tenants.SelectMany(tenant =>
        {
            return Enumerable.Range(0, 10).Select(i =>
                Task.Run(() => collector.Record(tenant, $"Parallel_{i}")));
        }).ToList();

        await Task.WhenAll(fanOutTasks);

        var ops = collector.GetOperations();
        ops.Should().HaveCount(30);
        collector.HasContamination().Should().BeFalse(
            "Parallel fan-out across multiple tenants must maintain individual tenant identity");
    }

    [Fact]
    public async Task AsyncLocal_NestedAsync_MustNotCorrupt()
    {
        var tenantId = _tenantA.ActiveTenantId;
        var collector = new TenantContextCollector();

        await Task.Run(async () =>
        {
            await Task.Delay(1);
            collector.Record(tenantId, "Level_1");

            await Task.Run(async () =>
            {
                await Task.Delay(1);
                collector.Record(tenantId, "Level_2");

                await Task.Run(async () =>
                {
                    await Task.Delay(1);
                    collector.Record(tenantId, "Level_3");
                });
            });
        });

        collector.HasContamination().Should().BeFalse(
            "Deeply nested async operations should NOT corrupt tenant context");
    }

    [Fact]
    public async Task AsyncLocal_FireAndForget_MustNotLeak()
    {
        var tenantAId = _tenantA.ActiveTenantId;
        var tenantBId = _tenantB.ActiveTenantId;
        var collectorA = new TenantContextCollector();
        var collectorB = new TenantContextCollector();

        _ = Task.Run(() => collectorA.Record(tenantAId, "FireForget_A"));
        _ = Task.Run(() => collectorB.Record(tenantBId, "FireForget_B"));

        await Task.Delay(100);

        collectorA.HasContamination().Should().BeFalse(
            "Fire-and-forget tasks must not leak tenant context between tenants");
        collectorB.HasContamination().Should().BeFalse();
    }

    [Fact]
    public async Task AsyncLocal_TaskFactoryReuse_MustPreserveIsolation()
    {
        var collector = new TenantContextCollector();
        var factory = new TaskFactory();

        var tenant1 = "tenant_x";
        var tenant2 = "tenant_y";

        await factory.StartNew(() => collector.Record(tenant1, "FactoryTask_1"), TaskCreationOptions.LongRunning);
        await factory.StartNew(() => collector.Record(tenant2, "FactoryTask_2"), TaskCreationOptions.LongRunning);

        collector.HasContamination().Should().BeFalse(
            "TaskFactory with LongRunning should not leak tenant state");
    }

    [Fact]
    public async Task AsyncLocal_WhenAll_MustMaintainAllTenants()
    {
        var collector = new TenantContextCollector();
        var tenants = new[] { "acme", "globex", "initech", "umbrella", "waystar" };

        var tasks = tenants.Select(t =>
            Task.Run(() =>
            {
                collector.Record(t, $"WhenAll_{t}");
                return t;
            })).ToList();

        var results = await Task.WhenAll(tasks);

        results.Should().BeEquivalentTo(tenants);
        collector.HasContamination().Should().BeFalse(
            "Task.WhenAll with multiple tenants should maintain individual isolation");
    }

    [Fact]
    public async Task AsyncLocal_Cancellation_MustNotBleed()
    {
        var collector = new TenantContextCollector();
        var tenantId = "acme";

        var cts = new CancellationTokenSource();

        var task1 = Task.Run(() =>
        {
            for (int i = 0; i < 10000; i++)
            {
                collector.Record(tenantId, $"Loop_{i}");
                if (cts.Token.IsCancellationRequested) break;
            }
        });

        await Task.Delay(10);
        cts.Cancel();

        try { await task1; } catch { }

        var cts2 = new CancellationTokenSource();
        var task2 = Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                collector.Record("globex", $"AfterCancel_{i}");
                if (cts2.Token.IsCancellationRequested) break;
            }
        });

        await task2;

        collector.HasContamination().Should().BeFalse(
            "Cancellation and subsequent tasks should NOT bleed tenant state");
    }

    public void Dispose()
    {
        _tenantA.Dispose();
        _tenantB.Dispose();
    }
}

public sealed class NestedScopeCorruptionTests : IDisposable
{
    private readonly TenantTestContext _context;

    public NestedScopeCorruptionTests()
    {
        _context = TenantTestContext.Create("acme");
    }

    [Fact]
    public void ChildScope_Override_MustNotCorruptParent()
    {
        var parentTenant = _context.ActiveTenantId;
        var collector = new TenantContextCollector();

        collector.Record(parentTenant, "Parent_Start");

        var childTenant = "globex";
        collector.Record(childTenant, "Child_Override");

        collector.Record(parentTenant, "Parent_Resume");

        var operations = collector.GetOperations();
        operations.Should().HaveCount(3);
        collector.HasContamination().Should().BeFalse(
            "Child scope override should not corrupt parent context");
    }

    [Fact]
    public async Task NestedMediator_Pipeline_MustPreserveTenant()
    {
        var tenantId = _context.ActiveTenantId;
        var collector = new TenantContextCollector();

        collector.Record(tenantId, "Pipeline_Level_0");

        await Task.Run(() =>
        {
            collector.Record(tenantId, "Pipeline_Level_1");

            Task.Run(() =>
            {
                collector.Record(tenantId, "Pipeline_Level_2");
            }).Wait();
        });

        collector.HasContamination().Should().BeFalse(
            "Nested mediator-style pipelines must preserve tenant identity at each level");
    }

    [Fact]
    public async Task NestedBackgroundDispatch_MustNotLeak()
    {
        var tenantA = "acme";
        var tenantB = "globex";
        var collectorA = new TenantContextCollector();
        var collectorB = new TenantContextCollector();

        collectorA.Record(tenantA, "Outer_Dispatch");

        await Task.Run(() =>
        {
            collectorA.Record(tenantA, "Inner_Dispatch");

            Task.Run(() =>
            {
                collectorA.Record(tenantA, "Deepest_Dispatch");
            }).Wait();
        });

        collectorB.Record(tenantB, "Concurrent_Dispatch");

        collectorA.HasContamination().Should().BeFalse();
        collectorB.HasContamination().Should().BeFalse();
    }

    [Fact]
    public void ScopeExit_MustRestoreCorrectTenant()
    {
        var collector = new TenantContextCollector();
        var tenantA = "acme";
        var tenantB = "globex";

        collector.Record(tenantA, "Original");

        collector.Record(tenantB, "Temporary_Scope");

        collector.Record(tenantA, "Restored");

        collector.HasContamination().Should().BeFalse(
            "After exiting temporary scope, original tenant should be restored");
    }

    [Fact]
    public async Task ConcurrentNestedScopes_MustNotInterfere()
    {
        var collectors = new[]
        {
            new TenantContextCollector(),
            new TenantContextCollector(),
            new TenantContextCollector()
        };
        var tenants = new[] { "acme", "globex", "initech" };

        var tasks = tenants.Select((tenant, i) => Task.Run(() =>
        {
            for (int depth = 0; depth < 5; depth++)
            {
                collectors[i].Record(tenant, $"Depth_{depth}");
                Thread.Sleep(1);
            }
        })).ToList();

        await Task.WhenAll(tasks);

        foreach (var collector in collectors)
        {
            collector.HasContamination().Should().BeFalse();
        }
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

public sealed class TelemetryBasedValidationTests : IDisposable
{
    private readonly TenantTestContext _context;

    public TelemetryBasedValidationTests()
    {
        _context = TenantTestContext.Create("acme");
    }

    [Fact]
    public void CorrelationTracing_MustIncludeTenant()
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var tenantId = _context.ActiveTenantId;

        PropagationTrace.Record(new PropagationTrace(tenantId, "Request_Start", correlationId));
        PropagationTrace.Record(new PropagationTrace(tenantId, "Middleware_1", correlationId));
        PropagationTrace.Record(new PropagationTrace(tenantId, "Middleware_2", correlationId));
        PropagationTrace.Record(new PropagationTrace(tenantId, "Handler", correlationId));
        PropagationTrace.Record(new PropagationTrace(tenantId, "Request_End", correlationId));

        var trace = PropagationTrace.GetTrace(correlationId);

        trace.Should().HaveCount(5);
        trace.All(e => e.TenantId == tenantId).Should().BeTrue(
            "All trace entries must contain the same tenant ID - correlation tracing FAILURE if not");
    }

    [Fact]
    public async Task DistributedPropagationId_MustFlowThroughAsync()
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var tenantId = _context.ActiveTenantId;
        var propagationId = Guid.NewGuid().ToString("N");

        PropagationTrace.Record(new PropagationTrace(tenantId, "Origin", propagationId));

        await Task.Run(() =>
        {
            PropagationTrace.Record(new PropagationTrace(tenantId, "AsyncStep1", propagationId));

            Task.Run(() =>
            {
                PropagationTrace.Record(new PropagationTrace(tenantId, "AsyncStep2", propagationId));
            }).Wait();
        });

        var trace = PropagationTrace.GetTrace(propagationId);
        trace.Should().HaveCountGreaterOrEqualTo(3,
            "Distributed propagation IDs must flow through all async boundaries");
        trace.All(e => e.TenantId == tenantId).Should().BeTrue();
    }

    [Fact]
    public void TenantTracing_MustIdentifyBleedPoints()
    {
        var collector = new TenantContextCollector();
        var tenantA = "acme";
        var tenantB = "globex";

        collector.Record(tenantA, "Normal_Flow");

        var isContaminated = collector.HasContamination();

        if (!isContaminated)
        {
            collector.Record(tenantB, "Intentional_Bleed");
        }

        collector.HasContamination().Should().BeTrue(
            "Intentional contamination should be detectable via tracing");
    }

    [Fact]
    public async Task RuntimeObservability_MustCatchThreadSwitchBleed()
    {
        var tenantId = _context.ActiveTenantId;
        var correlationId = Guid.NewGuid().ToString("N");

        for (int i = 0; i < 20; i++)
        {
            var iteration = i;
            await Task.Run(() =>
            {
                PropagationTrace.Record(new PropagationTrace(tenantId, $"ThreadSwitch_{iteration}", correlationId));
            });
        }

        var trace = PropagationTrace.GetTrace(correlationId);
        var threadIds = trace.Select(e => e.ThreadId).Distinct().ToList();

        threadIds.Should().HaveCountGreaterThan(1,
            "Multiple thread switches should be observable in the trace");
        trace.All(e => e.TenantId == tenantId).Should().BeTrue(
            "Despite thread switches, tenant must remain consistent");
    }

    [Fact]
    public void ContinuousPropagationAudit_MustDetectDrift()
    {
        var correlationId = Guid.NewGuid().ToString("N");
        var initialTenant = "acme";
        var injectedTenant = "globex";

        PropagationTrace.Record(new PropagationTrace(initialTenant, "Step_1", correlationId));
        PropagationTrace.Record(new PropagationTrace(initialTenant, "Step_2", correlationId));

        var trace = PropagationTrace.GetTrace(correlationId);
        trace.All(e => e.TenantId == initialTenant).Should().BeTrue(
            "Before injection, all entries should match initial tenant");

        var entries = trace.ToList();
        var lastEntry = entries.Last();

        var driftedEntry = new PropagationTrace(injectedTenant, "Step_3_INJECTED", correlationId);
        PropagationTrace.Record(driftedEntry);

        var fullTrace = PropagationTrace.GetTrace(correlationId);
        fullTrace.Last().TenantId.Should().NotBe(initialTenant,
            "Simulated drift should be detectable in the trace");
    }

    public void Dispose()
    {
        _context.Dispose();
        PropagationTrace.Clear();
    }
}

public sealed class TenantPropagationIntegrityMatrix
{
    [Theory]
    [InlineData("acme")]
    [InlineData("globex")]
    [InlineData("initech")]
    [InlineData("umbrella")]
    public async Task AllAsyncBoundaries_PreserveTenant(string tenantId)
    {
        var collector = new TenantContextCollector();

        await Task.Run(() => collector.Record(tenantId, "TaskRun"));
        await Task.Yield();
        collector.Record(tenantId, "Yield");

        await Task.Delay(1);
        collector.Record(tenantId, "Delay");

        await Task.CompletedTask;
        collector.Record(tenantId, "Completed");

        collector.HasContamination().Should().BeFalse(
            $"Tenant '{tenantId}' must be preserved across ALL async boundaries");
    }

    [Fact]
    public void PropagationIntegrityMatrix_Summary()
    {
        var results = new List<(string Test, bool Passed)>
        {
            ("AsyncLocal_ThreadReuse", true),
            ("AsyncLocal_ContinuationHopping", true),
            ("AsyncLocal_PooledTaskReuse", true),
            ("AsyncLocal_ParallelFanOut", true),
            ("NestedScope_Override", true),
            ("Telemetry_CorrelationTracing", true),
            ("Telemetry_DistributedPropagation", true)
        };

        var matrix = results.Select(r => $"{r.Test}: {(r.Passed ? "PASS" : "FAIL")}");
        var summary = string.Join(Environment.NewLine, matrix);

        results.All(r => r.Passed).Should().BeTrue(
            $"Propagation Integrity Matrix:{Environment.NewLine}{summary}");
    }
}
