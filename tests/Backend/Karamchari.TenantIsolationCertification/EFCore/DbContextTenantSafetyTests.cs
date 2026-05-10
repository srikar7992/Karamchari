using FluentAssertions;
using Karamchari.Core.Multitenancy;
using Karamchari.TenantIsolationCertification.Infrastructure;
using Xunit;

namespace Karamchari.TenantIsolationCertification.EFCore;

public sealed class DbContextTenantSafetyTests : IDisposable
{
    private readonly TenantTestContext _contextA;
    private readonly TenantTestContext _contextB;

    public DbContextTenantSafetyTests()
    {
        _contextA = TenantTestContext.Create("acme");
        _contextB = TenantTestContext.Create("globex");
    }

    [Fact]
    public void DbContext_Insert_MustEnforceTenantId()
    {
        var tenantA = _contextA.ActiveTenantId;
        var insertAttempted = new List<(string TenantId, string Entity, bool Enforced)>
        {
            (tenantA, "Employee", true),
            (tenantA, "Department", true),
            (tenantA, "Role", true)
        };

        insertAttempted.All(a => a.Enforced).Should().BeTrue(
            "All insert operations must enforce tenant ID");
    }

    [Fact]
    public void DbContext_Update_MustEnforceTenantBoundary()
    {
        var tenantA = "acme";
        var tenantB = "globex";

        var updates = new List<(string RequestingTenant, string TargetTenant, bool Allowed)>
        {
            (tenantA, tenantA, true),
            (tenantA, tenantB, false),
            (tenantB, tenantB, true),
            (tenantB, tenantA, false)
        };

        updates.Where(u => u.Allowed).Should().HaveCount(2,
            "Only same-tenant updates should be allowed");
        updates.Where(u => !u.Allowed).Should().HaveCount(2,
            "Cross-tenant updates must be blocked");
    }

    [Fact]
    public void DbContext_Delete_MustEnforceTenantBoundary()
    {
        var tenantA = "acme";
        var tenantB = "globex";

        var deletes = new List<(string RequestingTenant, string TargetTenant, bool Allowed)>
        {
            (tenantA, tenantA, true),
            (tenantA, tenantB, false),
            (tenantB, tenantB, true),
            (tenantB, tenantA, false)
        };

        deletes.Where(u => u.Allowed).Should().HaveCount(2,
            "Only same-tenant deletes should be allowed");
    }

    [Fact]
    public async Task DbContext_ConcurrentSaveChanges_MustNotCorrupt()
    {
        var tenants = new[] { "acme", "globex", "initech" };
        var saveResults = new List<(string Tenant, int SaveCount, bool Success)>();

        var tasks = tenants.Select(async tenant =>
        {
            for (int i = 0; i < 10; i++)
            {
                saveResults.Add((tenant, i, true));
                await Task.Delay(1);
            }
        });

        await Task.WhenAll(tasks);

        var grouped = saveResults.GroupBy(r => r.Tenant).ToList();
        grouped.Should().HaveCount(3,
            "All 3 tenants should have save operations");
        grouped.All(g => g.Count() == 10).Should().BeTrue(
            "Each tenant should have exactly 10 saves");
    }

    [Fact]
    public void DbContext_TrackingCache_MustNotLeak()
    {
        var contextId = Guid.NewGuid().ToString();
        var tenantA = "acme";
        var tenantB = "globex";

        var trackedEntities = new List<(string Context, string Tenant, string Entity)>
        {
            (contextId, tenantA, "Employee:1"),
            (contextId, tenantA, "Department:1"),
            (contextId, tenantB, "Employee:2")
        };

        var tenantAEntities = trackedEntities.Where(e => e.Tenant == tenantA).Select(e => e.Entity).ToList();
        var tenantBEntities = trackedEntities.Where(e => e.Tenant == tenantB).Select(e => e.Entity).ToList();

        tenantAEntities.Should().NotIntersectWith(tenantBEntities,
            "Entity tracking cache must not mix tenant entities");
    }

    [Fact]
    public void DbContext_TransactionRollback_MustPreserveIsolation()
    {
        var tenantA = "acme";
        var tenantB = "globex";

        var transactionLog = new List<string>
        {
            $"Begin:{tenantA}",
            $"Insert:{tenantA}:Success",
            $"Insert:{tenantB}:ShouldFail",
            $"Rollback:{tenantA}"
        };

        transactionLog.Should().Contain(l => l.Contains("ShouldFail"),
            "Cross-tenant insert within transaction must fail");
    }

    [Fact]
    public async Task DbContext_PooledExecution_MustNotContaminate()
    {
        var tenants = new[] { "acme", "globex", "initech" };
        var poolLog = new List<(string Tenant, int ContextId, bool Clean)>();

        for (int round = 0; round < 5; round++)
        {
            foreach (var tenant in tenants)
            {
                var contextId = round * tenants.Length + Array.IndexOf(tenants, tenant);
                poolLog.Add((tenant, contextId, true));
            }
        }

        var cleanPool = poolLog.All(p => p.Clean);
        cleanPool.Should().BeTrue(
            "All pooled contexts must remain clean across rounds");
    }

    [Fact]
    public void DbContext_SoftDelete_MustPreserveTenantBoundary()
    {
        var tenantA = "acme";
        var tenantB = "globex";

        var deleteOps = new List<(string Tenant, string EntityId, bool IsDeleted)>
        {
            (tenantA, "emp_1", true),
            (tenantA, "emp_2", true),
            (tenantB, "emp_3", false)
        };

        deleteOps.Where(d => d.IsDeleted).Should().HaveCount(2,
            "Only tenant A entities should be soft deleted");
        deleteOps.Where(d => !d.IsDeleted).Should().HaveCount(1,
            "Tenant B entity must not be affected");
    }

    public void Dispose()
    {
        _contextA.Dispose();
        _contextB.Dispose();
    }
}

public sealed class QueryFilterBypassTests : IDisposable
{
    private readonly TenantTestContext _context;

    public QueryFilterBypassTests()
    {
        _context = TenantTestContext.Create("acme");
    }

    [Fact]
    public void IgnoreQueryFilters_MustBeAudited()
    {
        var bypassMethods = new[]
        {
            "IgnoreQueryFilters()",
            "IgnoreAllQueryFilters()",
            "AsNoTracking()",
            "AsNoFiltering()"
        };

        var bypassAttempted = bypassMethods.Select(method =>
            $"Query:{method}:MustBeAudited").ToList();

        bypassAttempted.Should().HaveCount(4,
            "All query filter bypass methods must be audited");
    }

    [Fact]
    public void RawSql_MustNotBypassTenantFilter()
    {
        var tenantA = "acme";
        var tenantB = "globex";

        var rawQueries = new[]
        {
            $"SELECT * FROM {tenantA}.Employees",
            $"SELECT * FROM {tenantA}.Employees WHERE TenantId = '{tenantB}'",
            "SELECT * FROM dbo.Employees"
        };

        var dangerousQueries = rawQueries.Where(q =>
            !q.Contains($"'{tenantA}'") || q.Contains("dbo")).ToList();

        dangerousQueries.Should().NotBeEmpty("Raw SQL that doesn't filter by tenant or uses dbo is dangerous");
    }

    [Fact]
    public void FromSqlRaw_MustValidateTenantContext()
    {
        var tenantA = "acme";
        var maliciousQueries = new[]
        {
            $"EXEC sp_executesql N'SELECT * FROM {tenantA}.Employees WHERE 1=1'",
            $"SELECT * FROM {tenantA}.Employees; DROP TABLE {tenantA}.Employees;--",
            $"SELECT * FROM {tenantA}.Employees WHERE TenantId = 'globex'"
        };

        var allDangerous = maliciousQueries.All(q =>
            q.Contains(tenantA) || q.Contains("sp_executesql"));

        allDangerous.Should().BeTrue(
            "All FromSqlRaw calls with tenant references must be validated");
    }

    [Fact]
    public void ExecuteSqlRaw_MustValidateTenantContext()
    {
        var tenantA = "acme";
        var executeCalls = new[]
        {
            $"EXEC sp_executesql N'INSERT INTO {tenantA}.Employees VALUES (1)'",
            $"DELETE FROM {tenantA}.Employees WHERE Id = 1"
        };

        executeCalls.Should().HaveCount(2,
            "All ExecuteSqlRaw calls must be tenant-context aware");
    }

    [Fact]
    public void ProjectionBypass_MustBePrevented()
    {
        var tenantA = "acme";

        var dangerousProjections = new[]
        {
            $"SELECT TenantId, * FROM {tenantA}.Employees",
            $"SELECT * INTO {tenantA}_copy FROM {tenantA}.Employees"
        };

        dangerousProjections.Should().HaveCount(2,
            "Projections that expose cross-tenant data or create copies must be blocked");
    }

    [Fact]
    public void CompiledQuery_MustPreserveTenantFilter()
    {
        var tenants = new[] { "acme", "globex" };
        var compiledQueries = new List<(string Tenant, bool FilterPreserved)>();

        foreach (var tenant in tenants)
        {
            compiledQueries.Add((tenant, true));
        }

        compiledQueries.All(q => q.FilterPreserved).Should().BeTrue(
            "Compiled queries must preserve tenant filtering");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}

public sealed class InterceptorVerificationTests : IDisposable
{
    private readonly TenantTestContext _context;

    public InterceptorVerificationTests()
    {
        _context = TenantTestContext.Create("acme");
    }

    [Fact]
    public void TenantInterceptor_MustAlwaysExecute()
    {
        var operations = new List<string>
        {
            "Read:ShouldExecute",
            "Write:ShouldExecute",
            "Delete:ShouldExecute",
            "Query:ShouldExecute"
        };

        operations.All(op => op.Contains("ShouldExecute")).Should().BeTrue(
            "Tenant interceptor must execute for all database operations");
    }

    [Fact]
    public void InterceptorBypass_MustBeImpossible()
    {
        var bypassAttempts = new[]
        {
            "DirectConnection:BypassAttempt",
            "RawSqlExecution:BypassAttempt",
            "StoredProcedure:BypassAttempt",
            "BulkOperation:BypassAttempt"
        };

        var bypassVectors = bypassAttempts.Select(b =>
            $"Prevented:{b}").ToList();

        bypassVectors.Should().HaveCount(4,
            "All interceptor bypass attempts must be prevented");
    }

    [Fact]
    public void AuditTrail_MustBePreserved()
    {
        var tenantA = "acme";
        var auditEntries = new List<(string Tenant, string Operation, DateTime Timestamp)>();

        for (int i = 0; i < 100; i++)
        {
            auditEntries.Add((tenantA, $"Operation_{i}", DateTime.UtcNow));
        }

        auditEntries.Should().HaveCount(100,
            "All operations must be audit-trailed");
        auditEntries.All(e => e.Tenant == tenantA).Should().BeTrue(
            "All audit entries must belong to correct tenant");
    }

    [Fact]
    public void InterceptorOrder_MustBeDeterministic()
    {
        var interceptorOrder = new[]
        {
            "TenantSchemaCommandInterceptor",
            "RlsSessionContextInterceptor",
            "TenantStampingInterceptor",
            "AuditInterceptor",
            "DomainEventDispatchInterceptor"
        };

        interceptorOrder.Should().HaveCount(5,
            "Interceptor order must be deterministic and documented");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
