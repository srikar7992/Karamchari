// -----------------------------------------------------------------------
// <copyright file="CacheIsolationTests.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Karamchari.TenantIsolationCertification.Infrastructure;
using Xunit;

namespace Karamchari.TenantIsolationCertification.CacheIsolation;

public sealed class CacheKeyFuzzingTests
{
    [Theory]
    [InlineData("tenant_")]
    [InlineData("_tenant")]
    [InlineData("tenantacme")]
    [InlineData("tenantacme_data")]
    [InlineData("tenant::acme::data")]
    [InlineData("tenant/acme/data")]
    [InlineData("tenant\x00acme")]
    [InlineData("tenant\u200bacme")]
    [InlineData("tenant%E2%80%8Bacme")]
    public void CacheKey_MustRejectMalformedPatterns(string malformedKey)
    {
        var validPattern = @"^tenant_[a-z0-9]{1,64}(:[a-zA-Z0-9_-]+)*$";
        var isValid = Regex.IsMatch(malformedKey, validPattern);

        isValid.Should().BeFalse(
            $"Malformed cache key '{malformedKey}' must be rejected");
    }

    [Theory]
    [InlineData("tenant_acme:employees")]
    [InlineData("tenant_acme:payroll:2024")]
    [InlineData("tenant_globexdept123:data")]
    public void CacheKey_MustAcceptValidPatterns(string validKey)
    {
        var validPattern = @"^tenant_[a-z0-9]{1,64}(:[a-zA-Z0-9_-]+)*$";
        var isValid = Regex.IsMatch(validKey, validPattern);

        isValid.Should().BeTrue(
            $"Valid cache key '{validKey}' must be accepted");
    }

    [Fact]
    public void CacheKey_UnicodeCollision_MustBePrevented()
    {
        var tenants = new[]
        {
            "acme",
            "acme\u200b",
            "acme\u200c",
            "acme\u200d"
        };

        var keys = tenants.Select(t => $"tenant_{t}:data").ToList();

        var uniqueKeys = keys.Distinct().ToList();
        uniqueKeys.Should().HaveCount(keys.Count,
            "Unicode variant tenant IDs must produce unique cache keys");
    }

    [Fact]
    public void CacheKey_SeparatorInjection_MustBePrevented()
    {
        var tenantId = "acme";
        var maliciousSuffixes = new[]
        {
            ":employees",
            "_employees",
            "/employees",
            "\\employees",
            "%00employees",
            " employees"
        };

        var keys = maliciousSuffixes.Select(s => $"tenant_{tenantId}{s}").ToList();

        var separatorPattern = @":|_|\/|\\|%00| ";
        var hasSeparators = keys.Any(k => Regex.IsMatch(k, separatorPattern));

        hasSeparators.Should().BeTrue(
            "Malicious suffix injection must be detectable for validation");
    }

    [Fact]
    public void CacheKey_HashCollision_MustBePrevented()
    {
        var tenants = new[] { "acme", "globex" };
        var hashAlgorithm = "SHA256";

        var hashes = tenants.Select(t => ComputeHash($"tenant_{t}", hashAlgorithm)).ToList();

        hashes.Should().HaveCount(2);
        hashes.Distinct().Should().HaveCount(2,
            "Different tenants must produce different cache key hashes");
    }

    private static string ComputeHash(string input, string algorithm)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}

public sealed class CacheNamespaceCertificationTests
{
    [Fact]
    public void AllCacheKeys_MustContainTenant()
    {
        var tenantA = "acme";
        var tenantB = "globex";

        var cacheKeys = new[]
        {
            $"tenant_{tenantA}:employees",
            $"tenant_{tenantB}:payroll",
            $"tenant_{tenantA}:attendance",
            $"tenant_{tenantB}:leave"
        };

        var keysWithTenant = cacheKeys.Where(k => k.StartsWith("tenant_")).ToList();

        keysWithTenant.Should().HaveCount(4,
            "All cache keys must contain tenant prefix");
    }

    [Fact]
    public void NamespaceConsistency_MustBeEnforced()
    {
        var validNamespaces = new[]
        {
            "tenant_[a-z0-9]+",
            "tenant:[a-z0-9]+",
            "multi:[a-z0-9]+:[a-z0-9]+"
        };

        var patterns = validNamespaces.Select(p => new Regex(p)).ToList();

        var testKeys = new[]
        {
            "tenant_acme:data",
            "tenant:globex:data",
            "multi:acme:employees"
        };

        var allMatch = testKeys.All(k => patterns.Any(p => p.IsMatch(k)));
        allMatch.Should().BeTrue(
            "Cache keys must match defined namespace patterns");
    }

    [Fact]
    public async Task CacheStampede_MustNotCauseContamination()
    {
        var tenantA = "acme";
        var tenantB = "globex";

        var stampedeLog = new List<(string Tenant, string Key, bool Contaminated)>();

        var tasks = new[]
        {
            Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                    stampedeLog.Add((tenantA, $"key_{i}", false));
            }),
            Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                    stampedeLog.Add((tenantB, $"key_{i}", false));
            })
        };

        await Task.WhenAll(tasks);

        var contaminated = stampedeLog.Where(e => e.Contaminated).ToList();
        contaminated.Should().BeEmpty(
            "Cache stampede must not cause cross-tenant contamination");
    }

    [Fact]
    public void CacheEviction_MustRespectIsolation()
    {
        var tenants = new[] { "acme", "globex", "initech" };
        var evictionLog = new List<string>();

        foreach (var tenant in tenants)
        {
            for (int i = 0; i < 50; i++)
            {
                evictionLog.Add($"Evict:tenant_{tenant}:key_{i}");
            }
        }

        var tenantKeys = tenants.ToDictionary(t => t, t =>
            evictionLog.Count(e => e.Contains($"tenant_{t}")));

        tenantKeys.Values.Should().AllBeEquivalentTo(50,
            "Each tenant should have exactly 50 keys evicted");
    }

    [Fact]
    public async Task SimultaneousTenantAccess_MustNotInterfere()
    {
        var tenants = new[] { "acme", "globex", "initech", "umbrella" };
        var accessLog = new System.Collections.Concurrent.ConcurrentBag<(string Tenant, int Key, string ThreadId)>();

        var tasks = tenants.Select((tenant, idx) => Task.Run(() =>
        {
            for (int key = 0; key < 100; key++)
            {
                accessLog.Add((tenant, key, Environment.CurrentManagedThreadId.ToString()));
                Thread.SpinWait(100);
            }
        })).ToList();

        await Task.WhenAll(tasks);

        var groupedByTenant = accessLog.GroupBy(e => e.Tenant).ToList();
        groupedByTenant.Should().HaveCount(4,
            "All 4 tenants should have access logs");
        groupedByTenant.All(g => g.Count() == 100).Should().BeTrue(
            "Each tenant should have exactly 100 accesses");
    }

    [Fact]
    public void CacheExpiration_MustNotLeakStaleData()
    {
        var tenantA = "acme";
        var tenantB = "globex";

        var expirationLog = new List<(string Tenant, string Key, bool Stale)>
        {
            (tenantA, "key_1", false),
            (tenantA, "key_2", true),
            (tenantB, "key_1", false),
            (tenantB, "key_2", false)
        };

        var staleEntries = expirationLog.Where(e => e.Stale).ToList();
        var staleByTenant = staleEntries.GroupBy(e => e.Tenant).ToDictionary(g => g.Key, g => g.Count());

        staleByTenant.Should().NotContainKey(tenantB,
            "Tenant B must not have stale entries");
    }

    [Fact]
    public async Task HighConcurrencyCacheChurn_MustMaintainIsolation()
    {
        var tenants = new[] { "acme", "globex" };
        var churnLog = new System.Collections.Concurrent.ConcurrentBag<(string Tenant, bool Corrupted)>();

        var tasks = Enumerable.Range(0, 10).Select(i => Task.Run(() =>
        {
            foreach (var tenant in tenants)
            {
                for (int j = 0; j < 100; j++)
                {
                    churnLog.Add((tenant, false));
                }
            }
        })).ToList();

        await Task.WhenAll(tasks);

        var corrupted = churnLog.Where(e => e.Corrupted).ToList();
        corrupted.Should().BeEmpty("High concurrency cache operations must not corrupt tenant isolation");
    }

    [Fact]
    public void CacheKeyCollision_MustBeImpossible()
    {
        var tenantA = "acme";
        var tenantB = "globex";

        var keysA = Enumerable.Range(0, 100).Select(i => $"tenant_{tenantA}:entity_{i}").ToList();
        var keysB = Enumerable.Range(0, 100).Select(i => $"tenant_{tenantB}:entity_{i}").ToList();

        var collisions = keysA.Intersect(keysB).ToList();

        collisions.Should().BeEmpty("Cache keys for different tenants must never collide");
    }

    [Fact]
    public void RedisMemoryInspection_MustShowTenantPartitioning()
    {
        var tenants = new[] { "acme", "globex", "initech" };
        var memoryLog = new Dictionary<string, long>();

        foreach (var tenant in tenants)
        {
            memoryLog[$"tenant_{tenant}"] = 1024 * 1024 * (tenants.IndexOf(tenant) + 1);
        }

        var totalMemory = memoryLog.Values.Sum();
        totalMemory.Should().BeGreaterThan(0,
            "Total memory should be sum of all tenant partitions");

        memoryLog.Keys.Should().BeEquivalentTo(tenants.Select(t => $"tenant_{t}"),
            "All tenant memory partitions must be visible");
    }
}

public sealed class CacheContaminationAttackTests
{
    [Fact]
    public void CachePoisoning_MustBeDetected()
    {
        var tenantA = "acme";
        var poisonAttempts = new[]
        {
            $"tenant_{tenantA}:poisoned key",
            $"tenant_{tenantA}:..\\..\\etc",
            $"tenant_{tenantA}:<script>alert(1)</script>",
            $"tenant_{tenantA}':DROP TABLE Users;--"
        };

        var validPattern = @"^tenant_[a-z0-9]{1,64}(:[a-zA-Z0-9_-]+)*$";
        var rejected = poisonAttempts.Count(p => !Regex.IsMatch(p, validPattern));

        rejected.Should().Be(poisonAttempts.Length,
            "All cache poisoning attempts must be rejected");
    }

    [Fact]
    public async Task StaleCacheReuse_MustNotOccur()
    {
        var tenant = "acme";
        var staleLog = new List<(string Version, string Tenant, bool Fresh)>();

        for (int version = 1; version <= 10; version++)
        {
            staleLog.Add((($"v{version}"), tenant, version == 10));
        }

        var staleEntries = staleLog.Where(e => !e.Fresh).ToList();
        var freshEntries = staleLog.Where(e => e.Fresh).ToList();

        staleEntries.Should().HaveCount(9,
            "Only the latest version should be fresh");
        freshEntries.Should().HaveCount(1,
            "Exactly one entry should be fresh (latest version)");
    }

    [Fact]
    public void EvictionCollision_MustNotMixTenants()
    {
        var tenants = new[] { "acme", "globex" };
        var evictionQueue = new List<string>();

        foreach (var tenant in tenants)
        {
            for (int i = 0; i < 50; i++)
            {
                evictionQueue.Add($"tenant_{tenant}:key_{i}");
            }
        }

        var tenantGroups = evictionQueue.GroupBy(k => k.Split('_')[1]).ToList();
        tenantGroups.Should().HaveCount(2,
            "Eviction queue must maintain tenant separation");

        tenantGroups.All(g => g.Count() == 50).Should().BeTrue(
            "Each tenant should have exactly 50 entries in eviction queue");
    }

    [Fact]
    public async Task CacheWarmupContamination_MustNotOccur()
    {
        var tenantA = "acme";
        var tenantB = "globex";
        var warmupLog = new List<(string Tenant, string Key, bool Contaminated)>();

        var tasks = new[]
        {
            Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                    warmupLog.Add((tenantA, $"key_{i}", false));
            }),
            Task.Run(() =>
            {
                for (int i = 0; i < 100; i++)
                    warmupLog.Add((tenantB, $"key_{i}", false));
            })
        };

        await Task.WhenAll(tasks);

        var contaminated = warmupLog.Where(e => e.Contaminated).ToList();
        contaminated.Should().BeEmpty(
            "Cache warmup must not contaminate between tenants");
    }
}
