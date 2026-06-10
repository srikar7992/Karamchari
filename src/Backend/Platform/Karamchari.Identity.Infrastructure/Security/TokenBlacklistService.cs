// -----------------------------------------------------------------------
// <copyright file="TokenBlacklistService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Identity.Domain;
using Karamchari.Identity.Infrastructure.Persistence;
using Karamchari.Identity.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Karamchari.Identity.Infrastructure.Security;

internal sealed class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IdentityDbContext _db;
    private readonly IMemoryCache _cache;
    private const string CachePrefix = "revoked_";

    public TokenBlacklistService(IdentityDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task RevokeTokenAsync(string jti, DateTimeOffset expiresAt)
    {
        if (string.IsNullOrEmpty(jti)) return;

        var revoked = RevokedToken.Create(jti, expiresAt);
        _db.RevokedTokens.Add(revoked);
        await _db.SaveChangesAsync();

        // Add to local cache for fast lookup
        var ttl = expiresAt - DateTimeOffset.UtcNow;
        if (ttl > TimeSpan.Zero)
        {
            _cache.Set(CachePrefix + jti, true, ttl);
        }
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public async Task<bool> IsRevokedAsync(string jti)
    {
        if (string.IsNullOrEmpty(jti)) return false;

        // 1. Check local cache
        if (_cache.TryGetValue(CachePrefix + jti, out _))
        {
            return true;
        }

        // 2. Check DB
        var exists = await _db.RevokedTokens.AnyAsync(t => t.Jti == jti);
        if (exists)
        {
            // Backfill cache
            _cache.Set(CachePrefix + jti, true, TimeSpan.FromMinutes(5));
        }

        return exists;
    }
}
