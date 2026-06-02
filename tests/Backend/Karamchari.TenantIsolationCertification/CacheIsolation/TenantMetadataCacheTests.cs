using System.Text.Json;
using FluentAssertions;
using Karamchari.Core.Caching.Tenant;
using Karamchari.Core.Multitenancy;
using Karamchari.TenantIsolationCertification.Infrastructure;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Karamchari.TenantIsolationCertification.CacheIsolation;

public sealed class TenantMetadataCacheTests : IDisposable
{
    private readonly TenantTestContext _context;
    private readonly Mock<ITenantProvider> _tenantProviderMock;
    private readonly TenantCacheGuard _guard;
    private readonly IDistributedCache _cache;
    private readonly TenantMetadataCache _metadataCache;

    public TenantMetadataCacheTests()
    {
        _context = TenantTestContext.Create("acme");
        _tenantProviderMock = new Mock<ITenantProvider>();
        _tenantProviderMock.Setup(p => p.GetTenant())
            .Returns(new TenantExecutionEnvelope("acme", "corr-123", "req-123", ExecutionSource.Test, TenantSource.JwtClaim));

        _guard = new TenantCacheGuard(_tenantProviderMock.Object);
        _cache = new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
        _metadataCache = new TenantMetadataCache(_cache, _guard);
    }

    [Fact]
    public async Task SetAndGet_ShouldStoreAndRetrieveMetadata()
    {
        // Arrange
        var key = "acme-details";
        var metadata = new TenantMetadata("acme", "ACME Corp", "Enterprise", true, DateTime.UtcNow);

        // Act
        await _metadataCache.SetAsync(key, metadata);
        var retrieved = await _metadataCache.GetAsync(key);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.TenantId.Should().Be("acme");
        retrieved.Name.Should().Be("ACME Corp");
        retrieved.Plan.Should().Be("Enterprise");
        retrieved.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Remove_ShouldInvalidateCache()
    {
        // Arrange
        var key = "acme-details";
        var metadata = new TenantMetadata("acme", "ACME Corp", "Enterprise", true, DateTime.UtcNow);
        await _metadataCache.SetAsync(key, metadata);

        // Act
        await _metadataCache.RemoveAsync(key);
        var retrieved = await _metadataCache.GetAsync(key);

        // Assert
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task SetWithExpiration_ShouldExpireCorrectly()
    {
        // Arrange
        var key = "acme-expiring";
        var metadata = new TenantMetadata("acme", "ACME Expiring", "Trial", true, DateTime.UtcNow);

        // Act & Assert
        // Set with a very short expiration
        await _metadataCache.SetAsync(key, metadata, TimeSpan.FromMilliseconds(50));

        // Immediate get should succeed
        var retrievedBefore = await _metadataCache.GetAsync(key);
        retrievedBefore.Should().NotBeNull();

        // Wait for it to expire
        await Task.Delay(150);

        // Subsequent get should return null
        var retrievedAfter = await _metadataCache.GetAsync(key);
        retrievedAfter.Should().BeNull();
    }

    [Fact]
    public async Task Get_WithChangedTenantContext_ShouldThrowOrBlock()
    {
        // Arrange
        var key = "details";
        var metadata = new TenantMetadata("acme", "ACME Corp", "Enterprise", true, DateTime.UtcNow);
        await _metadataCache.SetAsync(key, metadata);

        // Change active tenant to globex
        _tenantProviderMock.Setup(p => p.GetTenant())
            .Returns(new TenantExecutionEnvelope("globex", "corr-123", "req-123", ExecutionSource.Test, TenantSource.JwtClaim));

        // Act & Assert
        // A request for key "details" as globex should not see acme's details.
        // It will look up "tenant_globex:metadata:details" which does not exist yet.
        var globexRetrieved = await _metadataCache.GetAsync(key);
        globexRetrieved.Should().BeNull();

        // If we try to validate a key belonging to acme while context is globex, the guard must throw
        var acmeNamespacedKey = TenantCacheNamespace.Build("acme", "metadata", "details");
        var action = () => _guard.ValidateGet(acmeNamespacedKey);
        action.Should().Throw<CachePoisoningDetectionException>();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
