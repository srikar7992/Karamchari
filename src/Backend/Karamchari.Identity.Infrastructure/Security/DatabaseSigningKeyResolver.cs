using System.Text;
using Karamchari.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;

namespace Karamchari.Identity.Infrastructure.Security;

/// <summary>
/// Resolves signing keys from the database with a short-lived memory cache.
/// Supports key rotation by allowing multiple active keys for validation.
/// </summary>
public sealed class DatabaseSigningKeyResolver
{
    private readonly IMemoryCache _cache;
    private readonly IDbContextFactory<IdentityDbContext> _dbFactory;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);
    private const string CacheKey = "Identity:SigningKeys";

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseSigningKeyResolver"/> class.
    /// </summary>
    /// <param name="cache">The memory cache.</param>
    /// <param name="dbFactory">The DB context factory.</param>
    public DatabaseSigningKeyResolver(IMemoryCache cache, IDbContextFactory<IdentityDbContext> dbFactory)
    {
        _cache = cache;
        _dbFactory = dbFactory;
    }

    /// <summary>
    /// Gets all keys valid for JWT validation.
    /// </summary>
    /// <returns>A collection of security keys.</returns>
    public async Task<IEnumerable<SecurityKey>> GetValidationKeysAsync()
    {
        var keys = await _cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;

            using var db = await _dbFactory.CreateDbContextAsync();
            var metadata = await db.SigningKeys
                .Where(x => x.ExpiresAt == null || x.ExpiresAt > DateTimeOffset.UtcNow)
                .ToListAsync();

            return metadata.Select(m => new SymmetricSecurityKey(Encoding.UTF8.GetBytes(m.Secret))
            {
                KeyId = m.KeyId
            }).ToList();
        });

        return keys ?? Enumerable.Empty<SecurityKey>();
    }

    /// <summary>
    /// Gets the current active key for signing new JWTs.
    /// </summary>
    /// <returns>The active security key, or null if none found.</returns>
    public async Task<SecurityKey?> GetCurrentSigningKeyAsync()
    {
        var validationKeys = await GetValidationKeysAsync();

        // The "current" key is the one that is active and hasn't expired, 
        // preferably the newest one.
        using var db = await _dbFactory.CreateDbContextAsync();
        var newestMetadata = await db.SigningKeys
            .Where(x => x.ActivatedAt <= DateTimeOffset.UtcNow && (x.ExpiresAt == null || x.ExpiresAt > DateTimeOffset.UtcNow))
            .OrderByDescending(x => x.ActivatedAt)
            .FirstOrDefaultAsync();

        if (newestMetadata == null) return null;

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(newestMetadata.Secret))
        {
            KeyId = newestMetadata.KeyId
        };
    }
}
