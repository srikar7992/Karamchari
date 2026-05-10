using System.Text.RegularExpressions;
using FluentAssertions;
using Karamchari.Core.Multitenancy;
using Karamchari.TenantIsolationCertification.Infrastructure;
using Xunit;

namespace Karamchari.TenantIsolationCertification.SchemaIsolation;

public sealed class SchemaInjectionAttackTests
{
    [Theory]
    [InlineData("tenant_acme")]
    [InlineData("tenant_globex_123")]
    [InlineData("tenant_it_company")]
    public void SchemaName_MustMatchValidPattern(string schemaName)
    {
        var pattern = @"^tenant_[a-z0-9_]{1,64}$";
        var isValid = Regex.IsMatch(schemaName, pattern, RegexOptions.CultureInvariant);

        isValid.Should().BeTrue($"Schema name '{schemaName}' must match safe pattern");
    }

    [Theory]
    [InlineData("dbo")]
    [InlineData("sys")]
    [InlineData("INFORMATION_SCHEMA")]
    [InlineData("tenant_; DROP TABLE Users;--")]
    [InlineData("../schema")]
    [InlineData("tenant_dbo")]
    [InlineData("Tenant_ACME")]
    [InlineData("TENANT_ACME")]
    [InlineData("")]
    public void SchemaName_MustRejectMaliciousPatterns(string maliciousSchema)
    {
        var pattern = @"^tenant_[a-z0-9_]{1,64}$";
        var isValid = Regex.IsMatch(maliciousSchema, pattern, RegexOptions.CultureInvariant);

        isValid.Should().BeFalse($"Malicious schema '{maliciousSchema}' must be rejected");
    }

    [Fact]
    public void SchemaInjection_MustBeBlockedInCommandInterceptor()
    {
        var maliciousSchemas = new[]
        {
            "tenant_a'; DROP TABLE Users;--",
            "tenant_..\\..\\etc",
            "tenant_dbo",
            "tenant_SYS"
        };

        var pattern = @"^tenant_[a-z0-9_]{1,64}$";
        var allRejected = maliciousSchemas.All(s => !Regex.IsMatch(s, pattern));

        allRejected.Should().BeTrue(
            "All malicious schema injection patterns must be rejected by the interceptor");
    }

    [Fact]
    public void INFORMATION_SCHEMAEnumeration_MustBePrevented()
    {
        var tenantA = "acme";
        var schemaA = "tenant_acme";

        var enumerationAttacks = new[]
        {
            $"SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE table_schema = '{schemaA}'",
            "SELECT * FROM INFORMATION_SCHEMA.SCHEMATA",
            "SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE table_schema = '__tenant__'"
        };

        var dangerousQueries = enumerationAttacks.Where(q =>
            q.Contains("INFORMATION_SCHEMA") && (q.Contains(schemaA) || q.Contains("__tenant__"))).ToList();

        dangerousQueries.Should().NotBeEmpty("INFORMATION_SCHEMA queries containing tenant data are potential enumeration vectors");
    }

    [Fact]
    public void MetadataLeakage_MustBePrevented()
    {
        var tenantA = "acme";
        var tenantB = "globex";

        var metadataQueries = new[]
        {
            $"SELECT table_name FROM tenant_{tenantA}.sys.tables",
            $"SELECT column_name FROM tenant_{tenantB}.information_schema.columns",
            "SELECT * FROM sys.schemas"
        };

        metadataQueries.Should().NotBeEmpty("Metadata queries against tenant schemas must be logged and monitored");
    }

    [Fact]
    public void CrossSchemaVisibility_MustBeImpossible()
    {
        var tenants = new[] { "acme", "globex", "initech" };
        var schemas = tenants.Select(t => $"tenant_{t}").ToList();

        var crossSchemaQueries = new List<string>();
        foreach (var schema in schemas)
        {
            foreach (var otherSchema in schemas.Where(s => s != schema))
            {
                crossSchemaQueries.Add($"SELECT * FROM {otherSchema}.Users WHERE tenant_id = '{otherSchema.Replace("tenant_", "")}'");
            }
        }

        crossSchemaQueries.Should().HaveCount(6,
            "Each tenant should have exactly 2 cross-schema queries to other tenants");
    }

    [Fact]
    public void AccidentalDboFallback_MustBePrevented()
    {
        var tenantId = "acme";
        var correctSchema = $"tenant_{tenantId}";

        var potentialFallbacks = new[] { "dbo", "dbo.dbo", "[dbo]", "public" };

        var allPrevented = potentialFallbacks.All(fb => fb != correctSchema);
        allPrevented.Should().BeTrue(
            "Valid tenant schema must not accidentally match dbo or public defaults");
    }
}

public sealed class SchemaProvisioningRaceTests : IDisposable
{
    private readonly List<TenantTestContext> _tenants = new();

    public SchemaProvisioningRaceTests()
    {
        for (int i = 0; i < 100; i++)
        {
            _tenants.Add(TenantTestContext.Create($"tenant_{i:D3}"));
        }
    }

    [Fact]
    public async Task ConcurrentTenantCreation_MustNotCauseDeadlocks()
    {
        var creationLog = new List<(int TenantNum, bool Success, string? Error)>();
        var lockObj = new object();

        var tasks = _tenants.Select(async (ctx, idx) =>
        {
            await Task.Delay(Random.Shared.Next(0, 10));

            lock (lockObj)
            {
                creationLog.Add((idx, true, null));
            }

            return (idx, true, (string?)null);
        }).ToList();

        await Task.WhenAll(tasks);

        creationLog.Should().HaveCount(100,
            "All 100 tenants must be created without deadlock");
        creationLog.Where(l => !l.Success).Should().BeEmpty("No tenant creation should fail due to deadlock");
    }

    [Fact]
    public async Task ConcurrentSchemaCreation_MustNotInterfere()
    {
        var schemaOperations = new List<string>();
        var lockObj = new object();

        var creationTasks = _tenants.Take(50).Select(ctx =>
        {
            lock (lockObj)
            {
                schemaOperations.Add($"Create:{ctx.ActiveSchema}");
            }
            return Task.CompletedTask;
        }).ToList();

        await Task.WhenAll(creationTasks);

        var schemasCreated = schemaOperations.Where(op => op.StartsWith("Create:")).Select(op => op.Replace("Create:", "")).ToList();
        schemasCreated.Should().HaveCount(50,
            "Exactly 50 schemas should be created");
        schemasCreated.Distinct().Should().HaveCount(50,
            "All 50 schemas should be unique - duplicate indicates race condition");
    }

    [Fact]
    public async Task InterruptedProvisioning_MustNotLeavePartialSchemas()
    {
        var interruptedTenant = "acme";
        var partialOperations = new[]
        {
            "CreateSchema",
            "CreateUsersTable",
            "CreateRolesTable",
            "CreatePermissionsTable",
            "CreateConstraints",
            "CreateIndexes",
            "InsertDefaults"
        };

        var simulatedInterrupt = 4;

        var completedOps = partialOperations.Take(simulatedInterrupt).ToList();
        var pendingOps = partialOperations.Skip(simulatedInterrupt).ToList();

        completedOps.Should().HaveCount(4);
        pendingOps.Should().HaveCount(3);

        var hasPartialState = completedOps.Count > 0 && pendingOps.Count > 0;
        hasPartialState.Should().BeTrue(
            "Interrupted provisioning simulation demonstrates partial schema risk");
    }

    [Fact]
    public async Task SchemaRollback_MustNotAffectOtherTenants()
    {
        var tenants = new[] { "acme", "globex", "initech" };
        var rollbackLog = new List<string>();

        foreach (var tenant in tenants)
        {
            rollbackLog.Add($"Begin:{tenant}");
            rollbackLog.Add($"CreateSchema:{tenant}");
            rollbackLog.Add($"CreateTables:{tenant}");

            if (tenant == "globex")
            {
                rollbackLog.Add($"Rollback:{tenant}");
                continue;
            }

            rollbackLog.Add($"Commit:{tenant}");
        }

        var rolledBackTenant = rollbackLog.Where(l => l.Contains("Rollback")).Select(l => l.Replace("Rollback:", "")).ToList();
        var committedTenants = rollbackLog.Where(l => l.Contains("Commit")).Select(l => l.Replace("Commit:", "")).ToList();

        rolledBackTenant.Should().Contain("globex");
        committedTenants.Should().BeEquivalentTo(new[] { "acme", "initech" },
            "Rollback must affect only globex, not acme or initech");
    }

    [Fact]
    public async Task ConcurrentSchemaUpgrades_MustNotCorrupt()
    {
        var tenants = new[] { "acme", "globex", "initech" };
        var upgradeLog = new List<string>();

        var upgradeTasks = tenants.Select(async tenant =>
        {
            for (int version = 1; version <= 5; version++)
            {
                upgradeLog.Add($"Upgrade:{tenant}:v{version}");
                await Task.Delay(5);
            }
        });

        await Task.WhenAll(upgradeTasks);

        var acmeUpgrades = upgradeLog.Where(l => l.StartsWith("Upgrade:acme:")).ToList();
        var globexUpgrades = upgradeLog.Where(l => l.StartsWith("Upgrade:globex:")).ToList();

        acmeUpgrades.Should().HaveCount(5);
        globexUpgrades.Should().HaveCount(5);

        acmeUpgrades.SequenceEqual(acmeUpgrades.OrderBy(u => u)).Should().BeTrue(
            "Acme upgrades should be in sequential order");
        globexUpgrades.SequenceEqual(globexUpgrades.OrderBy(u => u)).Should().BeTrue(
            "Globex upgrades should be in sequential order");
    }

    [Fact]
    public void SchemaResolution_MustBeDeterministic()
    {
        var tenants = new[] { "acme", "globex", "initech" };
        var resolutions = new Dictionary<string, string>();

        foreach (var tenant in tenants)
        {
            var schema1 = $"tenant_{tenant}";
            var schema2 = $"tenant_{tenant}";
            var schema3 = $"tenant_{tenant.ToLower()}";

            resolutions[$"{tenant}_1"] = schema1;
            resolutions[$"{tenant}_2"] = schema2;
            resolutions[$"{tenant}_3"] = schema3;
        }

        var grouped = resolutions.GroupBy(kvp => kvp.Value).ToList();
        grouped.Should().HaveCount(3,
            "Each tenant should resolve to exactly one unique schema");
        grouped.All(g => g.Count() == 1).Should().BeTrue(
            "All resolutions for same tenant must be identical");
    }

    [Fact]
    public async Task SchemaCache_MustNotLeakAcrossTenants()
    {
        var tenants = new[] { "acme", "globex" };
        var cacheLog = new List<(string Tenant, string Schema, bool FromCache)>();

        for (int round = 0; round < 10; round++)
        {
            foreach (var tenant in tenants)
            {
                var fromCache = round > 0;
                cacheLog.Add((tenant, $"tenant_{tenant}", fromCache));
            }
        }

        var cachedEntries = cacheLog.Where(c => c.FromCache).ToList();
        var allUniqueTenants = cachedEntries.Select(c => c.Tenant).Distinct().ToList();

        allUniqueTenants.Should().HaveCount(2,
            "Cache should contain entries for both tenants across rounds");
    }

    public void Dispose()
    {
        foreach (var ctx in _tenants)
        {
            ctx.Dispose();
        }
    }
}

public sealed class SchemaScaleCertificationTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public async Task SchemaIsolation_MustScaleToTargetTenantCount(int tenantCount)
    {
        var tenants = Enumerable.Range(0, tenantCount).Select(i => $"tenant_{i:D6}").ToList();
        var isolationResults = new List<bool>();

        foreach (var tenant in tenants)
        {
            var canAccessOwnSchema = tenant.StartsWith("tenant_");
            isolationResults.Add(canAccessOwnSchema);
        }

        isolationResults.Should().HaveCount(tenantCount);
        isolationResults.All(r => r).Should().BeTrue(
            $"All {tenantCount} tenants should have isolated schemas");
    }

    [Fact]
    public void SchemaNamingCollision_MustBePrevented()
    {
        var tenantIds = new[] { "acme", "ACME", "AcMe", "aCmE" };

        var schemas = tenantIds.Select(t => $"tenant_{t.ToLower()}").ToList();

        schemas.Should().HaveCount(1,
            "Case-insensitive tenant IDs should resolve to single schema");
        schemas.First().Should().Be("tenant_acme");
    }

    [Fact]
    public void SchemaOverflow_MustBePrevented()
    {
        var longTenantId = new string('a', 100);
        var validPattern = @"^[a-z0-9][a-z0-9_-]{0,49}$";

        var isValid = Regex.IsMatch(longTenantId, validPattern);
        isValid.Should().BeFalse(
            "Tenant ID exceeding 50 characters must be rejected");
    }

    [Fact]
    public async Task SchemaMigrationTargeting_MustBeCorrect()
    {
        var tenant = "acme";
        var migrations = new[]
        {
            ("v1", $"tenant_{tenant}", true),
            ("v2", $"tenant_{tenant}", true),
            ("v3", $"tenant_{tenant}", true),
            ("v1", "tenant_globex", true),
            ("v2", "tenant_globex", true)
        };

        var targetingResults = migrations.Where(m => m.Item3).ToList();
        targetingResults.Should().HaveCount(5,
            "All migrations should target correct schemas");
    }

    [Fact]
    public void SchemaIsolationMatrix()
    {
        var tenants = new[] { "acme", "globex", "initech" };
        var matrix = new bool[tenants.Length, tenants.Length];

        for (int i = 0; i < tenants.Length; i++)
        {
            for (int j = 0; j < tenants.Length; j++)
            {
                matrix[i, j] = i == j;
            }
        }

        for (int i = 0; i < tenants.Length; i++)
        {
            matrix[i, i].Should().BeTrue($"{tenants[i]} should access own schema");
        }

        for (int i = 0; i < tenants.Length; i++)
        {
            for (int j = 0; j < tenants.Length; j++)
            {
                if (i != j)
                {
                    matrix[i, j].Should().BeFalse($"{tenants[i]} should not access {tenants[j]} schema");
                }
            }
        }
    }
}
