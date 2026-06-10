// -----------------------------------------------------------------------
// <copyright file="TenantCacheGuard.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Caching.Tenant;

/// <summary>
/// Defensive guard for cache operations that ensures all cache keys are correctly namespaced
/// by tenant and prevents cross-tenant cache poisoning or data leakage.
/// </summary>
public sealed class TenantCacheGuard
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<TenantCacheGuard>? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantCacheGuard"/> class.
    /// </summary>
    /// <param name="tenantProvider">The tenant provider to resolve active tenant context.</param>
    public TenantCacheGuard(ITenantProvider tenantProvider)
        : this(tenantProvider, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantCacheGuard"/> class with logging.
    /// </summary>
    /// <param name="tenantProvider">The tenant provider.</param>
    /// <param name="logger">The logger for security events.</param>
    public TenantCacheGuard(ITenantProvider tenantProvider, ILogger<TenantCacheGuard>? logger)
    {
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _logger = logger;
    }

    /// <summary>
    /// Validates a cache key for a GET operation, ensuring it belongs to the current tenant.
    /// </summary>
    /// <param name="cacheKey">The cache key to validate.</param>
    /// <exception cref="CachePoisoningDetectionException">Thrown when a cross-tenant access attempt is detected.</exception>
    public void ValidateGet(string cacheKey)
    {
        var tenantContext = _tenantProvider.GetTenant();

        if (!TenantCacheNamespace.IsTenantKey(cacheKey))
        {
            _logger?.LogWarning("Cache get attempted with non-tenant key: {CacheKey}", cacheKey);

            throw new ArgumentException(
                $"Cache key '{cacheKey}' does not match tenant namespace pattern.",
                nameof(cacheKey));
        }

        var (keyTenantId, _) = TenantCacheNamespace.Parse(cacheKey);

        if (!string.Equals(keyTenantId, tenantContext.TenantId, StringComparison.OrdinalIgnoreCase))
        {
            _logger?.LogError(
                "CROSS-TENANT CACHE ACCESS DETECTED - Key tenant: {KeyTenantId}, Current tenant: {CurrentTenant}, Key: {CacheKey}",
                keyTenantId,
                tenantContext.TenantId,
                cacheKey);

            throw new CachePoisoningDetectionException(
                $"Cross-tenant cache access blocked. Key tenant '{keyTenantId}' does not match current tenant '{tenantContext.TenantId}'.");
        }

        _logger?.LogDebug("Cache get validated for tenant {TenantId}, key {CacheKey}", tenantContext.TenantId, cacheKey);
    }

    /// <summary>
    /// Validates a cache key for a SET operation.
    /// </summary>
    /// <param name="cacheKey">The cache key to validate.</param>
    public void ValidateSet(string cacheKey)
    {
        var tenantContext = _tenantProvider.GetTenant();

        if (!TenantCacheNamespace.IsTenantKey(cacheKey))
        {
            _logger?.LogWarning("Cache set attempted with non-tenant key: {CacheKey}", cacheKey);

            throw new ArgumentException(
                $"Cache key '{cacheKey}' does not match tenant namespace pattern.",
                nameof(cacheKey));
        }

        var (keyTenantId, _) = TenantCacheNamespace.Parse(cacheKey);

        if (!string.Equals(keyTenantId, tenantContext.TenantId, StringComparison.OrdinalIgnoreCase))
        {
            _logger?.LogError(
                "CROSS-TENANT CACHE SET ATTEMPT DETECTED - Key tenant: {KeyTenantId}, Current tenant: {CurrentTenant}, Key: {CacheKey}",
                keyTenantId,
                tenantContext.TenantId,
                cacheKey);

            throw new CachePoisoningDetectionException(
                $"Cross-tenant cache set blocked. Key tenant '{keyTenantId}' does not match current tenant '{tenantContext.TenantId}'.");
        }

        _logger?.LogDebug("Cache set validated for tenant {TenantId}, key {CacheKey}", tenantContext.TenantId, cacheKey);
    }

    /// <summary>
    /// Attempts to validate a GET operation without throwing, returning the result as a boolean.
    /// </summary>
    public bool TryValidateGet(string cacheKey)
    {
        try
        {
            ValidateGet(cacheKey);
            return true;
        }
        catch (Exception ex) when (ex is CachePoisoningDetectionException || ex is ArgumentException)
        {
            _logger?.LogDebug("Cache get validation failed for key {CacheKey}: {Error}", cacheKey, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Attempts to validate a SET operation without throwing.
    /// </summary>
    public bool TryValidateSet(string cacheKey)
    {
        try
        {
            ValidateSet(cacheKey);
            return true;
        }
        catch (Exception ex) when (ex is CachePoisoningDetectionException || ex is ArgumentException)
        {
            _logger?.LogDebug("Cache set validation failed for key {CacheKey}: {Error}", cacheKey, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Builds a valid tenant-namespaced cache key for the current tenant.
    /// </summary>
    public string BuildValidKey(string key)
    {
        var tenantContext = _tenantProvider.GetTenant();
        return TenantCacheNamespace.Build(tenantContext.TenantId, key);
    }

    /// <summary>
    /// Builds a valid tenant-namespaced cache key with a category.
    /// </summary>
    public string BuildValidKey(string category, string key)
    {
        var tenantContext = _tenantProvider.GetTenant();
        return TenantCacheNamespace.Build(tenantContext.TenantId, category, key);
    }
}
