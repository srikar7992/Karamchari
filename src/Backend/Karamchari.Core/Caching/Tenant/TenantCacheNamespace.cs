using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Karamchari.Core.Caching.Tenant;

public static class TenantCacheNamespace
{
    public const string DefaultPrefix = "tenant";
    private const char Separator = ':';
    private static readonly char[] Separators = { ':', '_', '/', '\\' };
    private static readonly Regex TenantKeyPattern = new(
        @"^tenant_[a-z0-9]{1,64}(:[a-zA-Z0-9_-]+)*$",
        RegexOptions.Compiled);

    public static string Build(string tenantId, string key)
    {
        return Build(tenantId, null, key);
    }

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
