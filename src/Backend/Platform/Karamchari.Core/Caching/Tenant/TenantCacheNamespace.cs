using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Karamchari.Core.Caching.Tenant;

/// <summary>
/// Defines the structure and naming conventions for tenant-scoped cache keys.
/// Provides mechanisms for building, parsing, and validating tenant namespaces in cache storage.
/// </summary>
public static class TenantCacheNamespace
{
    /// <summary>The default prefix for all tenant-scoped cache keys.</summary>
    public const string DefaultPrefix = "tenant";
    private const char Separator = ':';
    private static readonly char[] Separators = { ':', '_', '/', '\\' };
    private static readonly Regex TenantKeyPattern = new(
        @"^tenant_[a-z0-9]{1,64}(:[a-zA-Z0-9_-]+)*$",
        RegexOptions.Compiled);

    /// <summary>
    /// Builds a namespaced cache key for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="key">The base cache key.</param>
    /// <returns>A formatted namespaced key.</returns>
    public static string Build(string tenantId, string key)
    {
        return Build(tenantId, null, key);
    }

    /// <summary>
    /// Builds a namespaced cache key with a category for a specific tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="category">The category identifier.</param>
    /// <param name="key">The base cache key.</param>
    /// <returns>A formatted namespaced key.</returns>
    public static string Build(string tenantId, string? category, string key)
    {
        var normalizedTenantId = NormalizeTenantId(tenantId);
        var normalizedKey = NormalizeKey(key);

        if (string.IsNullOrEmpty(category))
        {
            return $"{DefaultPrefix}_{normalizedTenantId}{Separator}{normalizedKey}";
        }

        var normalizedCategory = NormalizeKey(category);
        return $"{DefaultPrefix}_{normalizedTenantId}{Separator}{normalizedCategory}{Separator}{normalizedKey}";
    }

    /// <summary>
    /// Parses a namespaced key back into its tenant ID and original key components.
    /// </summary>
    /// <param name="namespacedKey">The full namespaced key to parse.</param>
    /// <returns>A tuple containing the tenant ID and the original key.</returns>
    /// <exception cref="ArgumentException">Thrown when the key is not in a valid tenant namespace format.</exception>
    public static (string TenantId, string Key) Parse(string namespacedKey)
    {
        if (!IsTenantKey(namespacedKey))
        {
            throw new ArgumentException(
                $"Invalid tenant cache key format: '{namespacedKey}'. Key must match tenant namespace pattern.",
                nameof(namespacedKey));
        }

        var withoutPrefix = namespacedKey.Substring(DefaultPrefix.Length + 1);
        var firstSeparatorIdx = withoutPrefix.IndexOf(Separator);

        if (firstSeparatorIdx < 0)
        {
            return (withoutPrefix, string.Empty);
        }

        var tenantId = withoutPrefix.Substring(0, firstSeparatorIdx);
        var key = withoutPrefix.Substring(firstSeparatorIdx + 1);

        return (tenantId, key);
    }

    /// <summary>
    /// Validates whether a string matches the required tenant cache key pattern.
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>True if the key is a valid tenant-scoped key.</returns>
    public static bool IsTenantKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        return TenantKeyPattern.IsMatch(key);
    }

    private static string NormalizeTenantId(string tenantId)
    {
        if (string.IsNullOrEmpty(tenantId))
        {
            throw new ArgumentException("Tenant ID cannot be null or empty.", nameof(tenantId));
        }

        var normalized = tenantId.Trim().ToLowerInvariant();
        normalized = NormalizeUnicode(normalized);

        if (normalized.Length < 1 || normalized.Length > 64)
        {
            throw new ArgumentException(
                $"Tenant ID must be 1-64 characters after normalization. Got: {normalized.Length}",
                nameof(tenantId));
        }

        if (!Regex.IsMatch(normalized, @"^[a-z0-9]+$"))
        {
            throw new ArgumentException(
                $"Tenant ID must contain only lowercase alphanumeric characters after normalization. Got: '{normalized}'",
                nameof(tenantId));
        }

        return normalized;
    }

    private static string NormalizeKey(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));
        }

        var normalized = key.Trim();

        normalized = NormalizeUnicode(normalized);

        normalized = normalized.Replace(" ", "_");

        if (normalized.Length > 128)
        {
            throw new ArgumentException(
                $"Key must be 128 characters or less after normalization. Got: {normalized.Length}",
                nameof(key));
        }

        foreach (var sep in Separators)
        {
            if (normalized.Contains(sep))
            {
                throw new ArgumentException(
                    $"Key cannot contain separator character '{sep}'.",
                    nameof(key));
            }
        }

        return normalized;
    }

    private static string NormalizeUnicode(string input)
    {
        return input.Normalize(NormalizationForm.FormC);
    }
}
