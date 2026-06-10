// -----------------------------------------------------------------------
// <copyright file="TenantMetadataCache.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Karamchari.Core.Caching.Tenant;

/// <summary>
/// A production-grade tenant metadata cache that uses IDistributedCache and enforces tenant isolation via TenantCacheGuard.
/// </summary>
public sealed class TenantMetadataCache
{
    private readonly IDistributedCache _cache;
    private readonly TenantCacheGuard _guard;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantMetadataCache"/> class.
    /// </summary>
    public TenantMetadataCache(IDistributedCache cache, TenantCacheGuard guard)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _guard = guard ?? throw new ArgumentNullException(nameof(guard));
    }

    /// <summary>
    /// Retrieves tenant metadata from cache.
    /// </summary>
    public async Task<TenantMetadata?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var namespacedKey = _guard.BuildValidKey("metadata", key);
        _guard.ValidateGet(namespacedKey);

        var data = await _cache.GetStringAsync(namespacedKey, cancellationToken);
        if (data == null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<TenantMetadata>(data);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Stores tenant metadata in cache.
    /// </summary>
    public async Task SetAsync(string key, TenantMetadata metadata, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var namespacedKey = _guard.BuildValidKey("metadata", key);
        _guard.ValidateSet(namespacedKey);

        var options = new DistributedCacheEntryOptions();
        if (expiration.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiration.Value;
        }

        var data = JsonSerializer.Serialize(metadata);
        await _cache.SetStringAsync(namespacedKey, data, options, cancellationToken);
    }

    /// <summary>
    /// Invalidates tenant metadata in cache.
    /// </summary>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var namespacedKey = _guard.BuildValidKey("metadata", key);
        _guard.ValidateSet(namespacedKey);

        await _cache.RemoveAsync(namespacedKey, cancellationToken);
    }
}
