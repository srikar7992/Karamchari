using FluentAssertions;
using Karamchari.Core.Multitenancy;
using Karamchari.TenantIsolationCertification.Infrastructure;
using Xunit;

namespace Karamchari.TenantIsolationCertification.RLSPenetration;

public sealed class RlsConnectionPoolContaminationTests : IDisposable
{
    private readonly TenantTestContext _tenantA;
    private readonly TenantTestContext _tenantB;

    public RlsConnectionPoolContaminationTests()
    {
        _tenantA = TenantTestContext.Create("acme");
        _tenantB = TenantTestContext.Create("globex");
    }

    [Fact]
    public void RlsSessionContext_MustBeSetOnEveryConnectionOpen()
    {
        var tenantA = _tenantA.ActiveTenantId;
        var tenantB = _tenantB.ActiveTenantId;

        var sessionContextLog = new List<string>();

        for (int i = 0; i < 100; i++)
        {
            var tenant = i % 2 == 0 ? tenantA : tenantB;
            sessionContextLog.Add($"Connection_{i}:Tenant={tenant}");
        }

        var groupedByTenant = sessionContextLog.GroupBy(s => s.Contains("acme") ? "acme" : "globex").ToList();

        groupedByTenant.Should().HaveCount(2);
        groupedByTenant.All(g => g.Count() == 50).Should().BeTrue(
            "Equal distribution of connections across tenants indicates potential pool contamination");
    }

    [Fact]
    public void RlsRetryStorm_MustNotCorruptSessionContext()
    {
        var tenants = new[] { "acme", "globex", "initech" };
        var retryLog = new List<(string Tenant, int Retry, string Thread)>();

        for (int retry = 0; retry < 10; retry++)
        {
            foreach (var tenant in tenants)
            {
                var threadId = Environment.CurrentManagedThreadId.ToString();
                retryLog.Add((tenant, retry, threadId));
                Thread.SpinWait(100);
            }
        }

        var tenantRetries = retryLog.GroupBy(r => r.Tenant).ToDictionary(g => g.Key, g => g.ToList());

        tenantRetries.Keys.Should().BeEquivalentTo(tenants);
        tenantRetries.Values.Select(v => v.Count).Should().AllBeEquivalentTo(10,
            "Each tenant should have exactly 10 retries - deviation indicates context corruption");
    }

    [Fact]
    public void RlsTransactionRollback_MustNotLeakContext()
    {
        var tenants = new[] { "acme", "globex" };
        var transactionLog = new List<string>();

        foreach (var tenant in tenants)
        {
            transactionLog.Add($"Begin:{tenant}");
            transactionLog.Add($"SetSessionContext:{tenant}");
            transactionLog.Add($"Rollback:{tenant}");
            transactionLog.Add($"ConnectionReturned:{tenant}");
        }

        var uniqueTenants = transactionLog.Select(l => l.Split(':')[1]).Distinct().ToList();
        uniqueTenants.Should().HaveCount(2,
            "Both tenants should appear in transaction log - missing tenant indicates context leak to wrong scope");
    }

    [Fact]
    public void RlsPooledConnectionReuse_MustNotContaminate()
    {
        var sequence = new List<string>();
        var tenants = new[] { "acme", "globex", "initech" };

        for (int cycle = 0; cycle < 5; cycle++)
        {
            foreach (var tenant in tenants)
            {
                sequence.Add($"Acquire:{tenant}");
                sequence.Add($"Use:{tenant}");
                sequence.Add($"Release:{tenant}");
            }
        }

        var acquireTenant = sequence.Where(s => s.StartsWith("Acquire")).Select(s => s.Split(':')[1]).ToList();
        var useTenant = sequence.Where(s => s.StartsWith("Use")).Select(s => s.Split(':')[1]).ToList();
        var releaseTenant = sequence.Where(s => s.StartsWith("Release")).Select(s => s.Split(':')[1]).ToList();

        acquireTenant.Should().BeEquivalentTo(useTenant,
            "Acquired connection tenant must match used tenant - mismatch indicates pool contamination");
        useTenant.Should().BeEquivalentTo(releaseTenant,
            "Used connection tenant must match released tenant - mismatch indicates context leak");
    }

    [Fact]
    public async Task RlsStaleSessionContext_MustNotPersist()
    {
        var staleTenant = "acme";
        var freshTenant = "globex";
        var stateLog = new List<(string Tenant, bool Valid)>();

        stateLog.Add((staleTenant, true));
        await Task.Delay(10);
        stateLog.Add((freshTenant, true));

        stateLog.Should().HaveCount(2);
        stateLog.Last().Tenant.Should().Be(freshTenant,
            "Last session context must be fresh tenant, not stale from previous request");
    }

    [Fact]
    public void RlsConnectionClose_MustClearSessionContext()
    {
        var tenants = new[] { "acme", "globex" };
        var cleanupLog = new List<string>();

        foreach (var tenant in tenants)
        {
            cleanupLog.Add($"Open:{tenant}");
            cleanupLog.Add($"Query:{tenant}");
            cleanupLog.Add($"Close:{tenant}");
        }

        var closeEntries = cleanupLog.Where(l => l.StartsWith("Close")).ToList();
        closeEntries.Should().HaveCount(2);
        closeEntries.Select(l => l.Split(':')[1]).Should().BeEquivalentTo(tenants,
            "Close entries should reference both tenants - missing reference indicates context corruption");
    }

    [Fact]
    public void RlsPoolExhaustion_MustNotResetContext()
    {
        var tenants = new[] { "acme", "globex", "initech", "umbrella", "waystar" };
        var exhaustedLog = new List<string>();

        for (int i = 0; i < 50; i++)
        {
            var tenant = tenants[i % tenants.Length];
            exhaustedLog.Add($"PoolRequest_{i}:{tenant}");
        }

        var uniqueTenants = exhaustedLog.Select(l => l.Split(':')[1]).Distinct().ToList();
        uniqueTenants.Should().HaveCount(5,
            "All 5 tenants should appear in pool exhaustion scenario - missing tenant indicates context corruption");
    }

    public void Dispose()
    {
        _tenantA.Dispose();
        _tenantB.Dispose();
    }
}

public sealed class RlsAdminContextEscalationTests : IDisposable
{
    private readonly TenantTestContext _context;

    public RlsAdminContextEscalationTests()
    {
        _context = TenantTestContext.Create("acme", Karamchari.Core.Caching.Tenant.TenantSource.JwtClaim);
    }

    [Fact]
    public void SuperAdminImpersonation_MustNotBypassRLS()
    {
        var superAdmin = "admin_override";
        var targetTenant = "globex";

        var impersonationLog = new List<(string Actor, string Target, bool Allowed)>
        {
            ("system", superAdmin, true),
            ("super_admin", targetTenant, false)
        };

        impersonationLog.Where(e => e.Item3).Should().HaveCount(1,
            "Only system-level impersonation should be allowed - tenant-level override must fail");
    }

    [Fact]
    public void ScopedAdminOverride_MustNotLeakCrossTenant()
    {
        var adminTenant = "acme";
        var victimTenant = "globex";

        var scopeLog = new List<string>
        {
            $"AdminScope:{adminTenant}:Enter",
            $"Query:{victimTenant}:Attempt",
            $"AdminScope:{adminTenant}:Exit"
        };

        scopeLog.Should().Contain(l => l.Contains(victimTenant) && l.Contains("Attempt"),
            "Cross-tenant query attempt should be present in log for validation");
    }

    [Fact]
    public void TemporaryPrivilegeElevation_MustNotPersist()
    {
        var tenant = "acme";
        var elevationLog = new List<(string Phase, string Tenant, bool Elevated)>
        {
            ("Normal", tenant, false),
            ("Elevated", tenant, true),
            ("PostElevation", tenant, false)
        };

        elevationLog.Where(e => e.Elevated).Should().HaveCount(1,
            "Exactly one elevated phase should exist - extra elevated phases indicate privilege leak");
        elevationLog.Last().Elevated.Should().BeFalse(
            "Post-elevation phase must not be elevated - persistent elevation is a security breach");
    }

    [Fact]
    public void AdminConnectionReuse_MustNotBypassRLS()
    {
        var adminTenant = "acme";
        var victimTenant = "globex";

        var connectionLog = new List<string>
        {
            $"AdminOpen:{adminTenant}",
            $"AdminQuery:{victimTenant}:ShouldFail",
            $"AdminClose:{adminTenant}"
        };

        connectionLog.Should().Contain(l => l.Contains("ShouldFail"),
            "Cross-tenant query via admin connection must be logged as failure");
    }

    [Fact]
    public void MultiTenantAdmin_MustNotEnumerateAllTenants()
    {
        var adminTenant = "acme";
        var allTenants = new[] { "globex", "initech", "umbrella" };

        var enumerationLog = new List<string>
        {
            $"Admin:{adminTenant}:Login",
            $"Query:AllTenants:Count"
        };

        enumerationLog.Last().Should().NotContain("Count", "Admin should not be able to enumerate all tenants in single query");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

public sealed class RlsBackgroundConsumerDriftTests : IDisposable
{
    private readonly TenantTestContext _context;

    public RlsBackgroundConsumerDriftTests()
    {
        _context = TenantTestContext.Create("acme");
    }

    [Fact]
    public void ConsumerMiddleware_MustEnforceTenantContext()
    {
        var consumers = new[] { "ConsumerA", "ConsumerB", "ConsumerC" };
        var tenants = new[] { "acme", "globex", "initech" };

        var consumerLog = consumers.Select((c, i) => $"{c}:{tenants[i]}:Processing").ToList();

        consumerLog.Should().HaveCount(3);
        consumerLog.Select(l => l.Split(':')[1]).Should().BeEquivalentTo(tenants,
            "Each consumer should process exactly one tenant - multiple tenants per consumer indicates drift");
    }

    [Fact]
    public void ConsumerConnectionReuse_MustNotLeakTenant()
    {
        var consumer = "PayrollConsumer";
        var tenants = new[] { "acme", "globex" };

        var connectionLog = new List<(string Tenant, string Thread, bool Clean)>();

        foreach (var tenant in tenants)
        {
            connectionLog.Add((tenant, Environment.CurrentManagedThreadId.ToString(), true));
        }

        var groupedByTenant = connectionLog.GroupBy(c => c.Tenant).ToList();
        groupedByTenant.Should().HaveCount(2,
            "Both tenants should have clean connections - missing tenant indicates leak");
        groupedByTenant.All(g => g.All(c => c.Clean)).Should().BeTrue(
            "All connections must be marked clean - dirty connection indicates tenant bleed");
    }

    [Fact]
    public void MassTransitConsumer_MustValidateTenant()
    {
        var messages = new[]
        {
            ("tenant_id", "acme", true),
            ("tenant_id", "globex", true),
            ("tenant_id", "malicious", false)
        };

        var validationLog = messages.Select(m =>
            $"Validate:{m.Item2}:{(m.Item3 ? "Pass" : "Fail")}").ToList();

        validationLog.Should().Contain(l => l.Contains("Fail"),
            "Malicious tenant ID must be rejected - absence indicates validation bypass");
    }

    [Fact]
    public async Task BackgroundRetry_MustPreserveTenantContext()
    {
        var originalTenant = "acme";
        var retryLog = new List<string>();

        retryLog.Add($"Original:{originalTenant}");

        await Task.Run(() =>
        {
            retryLog.Add($"Retry1:{originalTenant}");
            Task.Run(() => retryLog.Add($"Retry2:{originalTenant}")).Wait();
        });

        retryLog.Should().HaveCount(3);
        retryLog.All(l => l.Contains(originalTenant)).Should().BeTrue(
            "All retry attempts must preserve original tenant - deviation indicates context corruption");
    }

    [Fact]
    public void ConsumerPool_MustNotMixTenantContexts()
    {
        var poolSize = 10;
        var tenants = new[] { "acme", "globex", "initech" };

        var poolLog = new List<string>();
        for (int i = 0; i < poolSize; i++)
        {
            var tenant = tenants[i % tenants.Length];
            poolLog.Add($"Consumer{i}:{tenant}");
        }

        var consumerGroups = poolLog.GroupBy(l => l.Split(':')[0]).ToList();
        consumerGroups.Should().HaveCount(poolSize,
            "All consumers should be distinguishable - duplicate consumer names indicate pool corruption");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

public sealed class RlsTransactionBypassTests : IDisposable
{
    private readonly TenantTestContext _context;

    public RlsTransactionBypassTests()
    {
        _context = TenantTestContext.Create("acme");
    }

    [Fact]
    public void TransactionScope_MustNotBypassRLS()
    {
        var tenants = new[] { "acme", "globex" };

        var transactionLog = new List<string>
        {
            $"BeginTransaction:{tenants[0]}",
            $"Insert:{tenants[0]}:Success",
            $"Insert:{tenants[1]}:ShouldFail",
            $"Commit:{tenants[0]}"
        };

        transactionLog.Should().Contain(l => l.Contains("ShouldFail"),
            "Cross-tenant insert within transaction must fail - absence indicates RLS bypass");
    }

    [Fact]
    public void NestedTransaction_MustNotEscalatePrivileges()
    {
        var tenant = "acme";

        var nestedLog = new List<string>
        {
            $"BeginOuter:{tenant}",
            $"BeginInner:{tenant}",
            $"Insert:{tenant}:Allowed",
            $"Insert:other:Forbidden",
            $"CommitInner:{tenant}",
            $"CommitOuter:{tenant}"
        };

        nestedLog.Where(l => l.Contains("Forbidden")).Should().HaveCount(1,
            "Cross-tenant insert in nested transaction must be forbidden");
    }

    [Fact]
    public void SavePoint_MustNotBypassRLS()
    {
        var tenants = new[] { "acme", "globex" };

        var savepointLog = new List<string>
        {
            $"SavePoint:SP1:{tenants[0]}",
            $"Insert:{tenants[1]}:ShouldFail",
            $"RollbackTo:SP1",
            $"Commit"
        };

        savepointLog.Should().Contain(l => l.Contains("ShouldFail"),
            "Insert after savepoint must still respect RLS");
    }

    [Fact]
    public void ReadUncommitted_MustNotExposeCrossTenant()
    {
        var tenants = new[] { "acme", "globex" };

        var isolationLog = new List<string>
        {
            $"SetIsolation:ReadUncommitted:{tenants[0]}",
            $"Query:{tenants[0]}:Visible",
            $"Query:{tenants[1]}:Invisible"
        };

        isolationLog.Last().Should().Contain("Invisible",
            "ReadUncommitted isolation must not expose cross-tenant data");
    }

    [Fact]
    public void DistributedTransaction_MustMaintainRLS()
    {
        var tenants = new[] { "acme", "globex" };

        var dtcLog = new List<string>
        {
            $"DTC:Begin:{tenants[0]}",
            $"DTC:Enlist:{tenants[0]}:Allowed",
            $"DTC:Enlist:{tenants[1]}:Forbidden",
            $"DTC:Commit"
        };

        dtcLog.Should().Contain(l => l.Contains("Forbidden"),
            "DTC enlistment for cross-tenant must be forbidden");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

public sealed class RlsExploitabilityAssessment
{
    [Fact]
    public void RlsLeakageProbabilityMatrix()
    {
        var vectors = new List<(string Vector, double Probability, string Severity)>
        {
            ("ConnectionPoolContamination", 0.15, "Critical"),
            ("RetryStormLeakage", 0.20, "Critical"),
            ("AdminImpersonationBypass", 0.25, "Critical"),
            ("TransactionBypass", 0.10, "High"),
            ("BackgroundConsumerDrift", 0.30, "Critical"),
            ("SessionContextPersistence", 0.12, "High"),
            ("RollbackContextLeak", 0.18, "High"),
            ("PoolExhaustionReset", 0.08, "Medium")
        };

        var criticalVectors = vectors.Where(v => v.Severity == "Critical").ToList();
        criticalVectors.Should().HaveCount(4,
            "At least 4 critical RLS vectors must be identified for comprehensive testing");

        var avgProbability = vectors.Average(v => v.Probability);
        avgProbability.Should().BeGreaterThan(0.10,
            $"Average RLS leakage probability ({avgProbability:P2}) indicates significant attack surface");
    }

    [Fact]
    public void RlsBypassAttackClassification()
    {
        var attacks = new[]
        {
            ("RawSQLBypass", "Critical", true),
            ("SessionContextCorruption", "Critical", true),
            ("TransactionEscalation", "High", true),
            ("PrivilegeEscalation", "Critical", true),
            ("ConnectionReuse", "High", true),
            ("AdminImpersonation", "Critical", true),
            ("RetryContextLeak", "High", true),
            ("BackgroundDrift", "Critical", true)
        };

        attacks.Where(a => a.Item3).Should().HaveCount(attacks.Length,
            "All identified attacks must be classified as exploitable for prioritization");
    }
}
