using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Caching.Tenant;

public sealed class TenantCacheGuard
{
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ILogger<TenantCacheGuard>? _logger;

    public TenantCacheGuard(ITenantContextAccessor tenantContextAccessor)
        : this(tenantContextAccessor, null)
    {
    }

    public TenantCacheGuard(ITenantContextAccessor tenantContextAccessor, ILogger<TenantCacheGuard>? logger)
    {
        _tenantContextAccessor = tenantContextAccessor ?? throw new ArgumentNullException(nameof(tenantContextAccessor));
        _logger = logger;
    }

    public void ValidateGet(string cacheKey)
    {
        var tenantContext = _tenantContextAccessor.TenantContext;

        if (tenantContext == null || string.IsNullOrEmpty(tenantContext.TenantId))
        {
            throw new InvalidOperationException("Tenant context is not set. Cannot perform cache get operation.");
        }

        if (!TenantCacheNamespace.IsTenantKey(cacheKey))
        {
            if (_logger?.IsEnabled(LogLevel.Warning) == true)
            {
                _logger.LogWarning(
                    "Cache get attempted with non-tenant key: {CacheKey}",
                    cacheKey);
            }

            throw new ArgumentException(
                $"Cache key '{cacheKey}' does not match tenant namespace pattern. Cross-tenant cache access may be attempted.",
                nameof(cacheKey));
        }

        var (keyTenantId, _) = TenantCacheNamespace.Parse(cacheKey);

        if (!string.Equals(keyTenantId, tenantContext.TenantId, StringComparison.OrdinalIgnoreCase))
        {
            if (_logger?.IsEnabled(LogLevel.Error) == true)
            {
                _logger.LogError(
                    "CROSS-TENANT CACHE ACCESS DETECTED - Key tenant: {KeyTenantId}, Current tenant: {CurrentTenant}, Key: {CacheKey}",
                    keyTenantId,
                    tenantContext.TenantId,
                    cacheKey);
            }

            throw new CachePoisoningDetectionException(
                $"Cross-tenant cache access blocked. Key tenant '{keyTenantId}' does not match current tenant '{tenantContext.TenantId}'.");
        }

        if (_logger?.IsEnabled(LogLevel.Debug) == true)
        {
            _logger.LogDebug(
                "Cache get validated for tenant {TenantId}, key {CacheKey}",
                tenantContext.TenantId,
                cacheKey);
        }
    }

    public void ValidateSet(string cacheKey)
    {
        var tenantContext = _tenantContextAccessor.TenantContext;

        if (tenantContext == null || string.IsNullOrEmpty(tenantContext.TenantId))
        {
            throw new InvalidOperationException("Tenant context is not set. Cannot perform cache set operation.");
        }

        if (!TenantCacheNamespace.IsTenantKey(cacheKey))
        {
            if (_logger?.IsEnabled(LogLevel.Warning) == true)
            {
                _logger.LogWarning(
                    "Cache set attempted with non-tenant key: {CacheKey}",
                    cacheKey);
            }

            throw new ArgumentException(
                $"Cache key '{cacheKey}' does not match tenant namespace pattern. Cross-tenant cache access may be attempted.",
                nameof(cacheKey));
        }

        var (keyTenantId, _) = TenantCacheNamespace.Parse(cacheKey);

        if (!string.Equals(keyTenantId, tenantContext.TenantId, StringComparison.OrdinalIgnoreCase))
        {
            if (_logger?.IsEnabled(LogLevel.Error) == true)
            {
                _logger.LogError(
                    "CROSS-TENANT CACHE SET ATTEMPT DETECTED - Key tenant: {KeyTenantId}, Current tenant: {CurrentTenant}, Key: {CacheKey}",
                    keyTenantId,
                    tenantContext.TenantId,
                    cacheKey);
            }

            throw new CachePoisoningDetectionException(
                $"Cross-tenant cache set blocked. Key tenant '{keyTenantId}' does not match current tenant '{tenantContext.TenantId}'.");
        }

        if (_logger?.IsEnabled(LogLevel.Debug) == true)
        {
            _logger.LogDebug(
                "Cache set validated for tenant {TenantId}, key {CacheKey}",
                tenantContext.TenantId,
                cacheKey);
        }
    }

    public bool TryValidateGet(string cacheKey)
    {
        try
        {
            ValidateGet(cacheKey);
            return true;
        }
        catch (Exception ex) when (ex is CachePoisoningDetectionException || ex is ArgumentException)
        {
            if (_logger?.IsEnabled(LogLevel.Debug) == true)
            {
                _logger.LogDebug(
                    "Cache get validation failed for key {CacheKey}: {Error}",
                    cacheKey,
                    ex.Message);
            }
            return false;
        }
    }

    public bool TryValidateSet(string cacheKey)
    {
        try
        {
            ValidateSet(cacheKey);
            return true;
        }
        catch (Exception ex) when (ex is CachePoisoningDetectionException || ex is ArgumentException)
        {
            if (_logger?.IsEnabled(LogLevel.Debug) == true)
            {
                _logger.LogDebug(
                    "Cache set validation failed for key {CacheKey}: {Error}",
                    cacheKey,
                    ex.Message);
            }
            return false;
        }
    }

    public string BuildValidKey(string key)
    {
        var tenantContext = _tenantContextAccessor.TenantContext;

        if (tenantContext == null || string.IsNullOrEmpty(tenantContext.TenantId))
        {
            throw new InvalidOperationException("Tenant context is not set. Cannot build cache key.");
        }

        return TenantCacheNamespace.Build(tenantContext.TenantId, key);
    }

    public string BuildValidKey(string category, string key)
    {
        var tenantContext = _tenantContextAccessor.TenantContext;

        if (tenantContext == null || string.IsNullOrEmpty(tenantContext.TenantId))
        {
            throw new InvalidOperationException("Tenant context is not set. Cannot build cache key.");
        }

        return TenantCacheNamespace.Build(tenantContext.TenantId, category, key);
    }
}
