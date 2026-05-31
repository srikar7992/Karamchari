using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Caching.Tenant;

/// <summary>
/// Builder for constructing secure, normalized, and tenant-namespaced cache keys.
/// Enforces character validation, length limits, and Unicode normalization to prevent
/// collision attacks and key injection.
/// </summary>
public sealed class TenantCacheKeyBuilder
{
    private const int MaxKeyLength = 256;
    private const int MaxTenantIdLength = 64;
    private static readonly char[] InvalidKeyChars = { '\0', '\n', '\r', '\t', '\v' };
    private readonly ILogger<TenantCacheKeyBuilder>? _logger;

    private static readonly HashSet<string> ConfusableTenants = new(StringComparer.OrdinalIgnoreCase)
    {
        "\u200b",
        "\u200c",
        "\u200d",
        "\ufeff"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantCacheKeyBuilder"/> class.
    /// </summary>
    public TenantCacheKeyBuilder()
    {
        _logger = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantCacheKeyBuilder"/> class with logging.
    /// </summary>
    /// <param name="logger">The logger for validation warnings.</param>
    public TenantCacheKeyBuilder(ILogger<TenantCacheKeyBuilder> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Builds a tenant-namespaced cache key.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="key">The base cache key.</param>
    /// <returns>A formatted tenant-namespaced key.</returns>
    public string Build(string tenantId, string key)
    {
        return Build(tenantId, null, key);
    }

    /// <summary>
    /// Builds a tenant-namespaced cache key with a category.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="category">The category (namespace within tenant).</param>
    /// <param name="key">The base cache key.</param>
    /// <returns>A formatted tenant-namespaced key.</returns>
    public string Build(string tenantId, string? category, string key)
    {
        tenantId = tenantId?.Trim() ?? string.Empty;
        key = key?.Trim() ?? string.Empty;
        category = category?.Trim();

        ValidateTenantId(tenantId);
        ValidateKey(key);

        if (!string.IsNullOrEmpty(category))
        {
            ValidateKey(category);
        }

        var normalizedTenantId = NormalizeTenantId(tenantId);
        var normalizedKey = NormalizeKey(key);

        if (string.IsNullOrEmpty(category))
        {
            return $"tenant_{normalizedTenantId}:{normalizedKey}";
        }

        var normalizedCategory = NormalizeKey(category);
        return $"tenant_{normalizedTenantId}:{normalizedCategory}:{normalizedKey}";
    }

    /// <summary>
    /// Validates a tenant identifier for use in cache keys.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
    public void ValidateTenantId(string tenantId)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant ID cannot be null or whitespace.", nameof(tenantId));
        }

        var trimmed = tenantId.Trim();

        if (trimmed.Length > MaxTenantIdLength)
        {
            throw new ArgumentException(
                $"Tenant ID exceeds maximum length of {MaxTenantIdLength}. Got: {trimmed.Length}",
                nameof(tenantId));
        }

        if (!Regex.IsMatch(trimmed, @"^[a-zA-Z0-9_-]+$"))
        {
            throw new ArgumentException(
                $"Tenant ID must contain only alphanumeric characters, underscores, and hyphens. Got: '{trimmed}'",
                nameof(tenantId));
        }

        var normalized = NormalizeUnicode(trimmed.ToLowerInvariant());
        if (normalized != trimmed.ToLowerInvariant())
        {
            _logger?.LogWarning(
                "Tenant ID '{TenantId}' contains Unicode characters that normalize differently. Original: {Normalized}",
                tenantId,
                normalized);
        }

        foreach (var confusable in ConfusableTenants)
        {
            if (normalized.Contains(confusable))
            {
                throw new ArgumentException(
                    $"Tenant ID contains confusable Unicode character. ID: '{tenantId}'",
                    nameof(tenantId));
            }
        }
    }

    /// <summary>
    /// Validates a cache key part for length and invalid characters.
    /// </summary>
    /// <param name="key">The key part to validate.</param>
    public void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or whitespace.", nameof(key));
        }

        if (key.Length > MaxKeyLength)
        {
            throw new ArgumentException(
                $"Key exceeds maximum length of {MaxKeyLength}. Got: {key.Length}",
                nameof(key));
        }

        foreach (var invalidChar in InvalidKeyChars)
        {
            if (key.Contains(invalidChar))
            {
                throw new ArgumentException(
                    $"Key contains invalid control character 0x{(int)invalidChar:X2}.",
                    nameof(key));
            }
        }

        if (key.StartsWith(" ", StringComparison.Ordinal) || key.EndsWith(" ", StringComparison.Ordinal))
        {
            throw new ArgumentException("Key cannot start or end with whitespace.", nameof(key));
        }

        var normalized = NormalizeUnicode(key);
        if (normalized != key)
        {
            _logger?.LogWarning("Key '{Key}' contains Unicode characters that normalize differently.", key);
        }
    }

    /// <summary>
    /// Attempts to build a key without throwing exceptions.
    /// </summary>
    public bool TryBuild(string tenantId, string key, out string? result)
    {
        try
        {
            result = Build(tenantId, key);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    /// <summary>
    /// Attempts to build a key with category without throwing exceptions.
    /// </summary>
    public bool TryBuild(string tenantId, string category, string key, out string? result)
    {
        try
        {
            result = Build(tenantId, category, key);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    private static string NormalizeTenantId(string tenantId)
    {
        return NormalizeUnicode(tenantId.Trim().ToLowerInvariant());
    }

    private static string NormalizeKey(string key)
    {
        var normalized = NormalizeUnicode(key.Trim());
        normalized = normalized.Replace(" ", "_");
        return normalized;
    }

    private static string NormalizeUnicode(string input)
    {
        return input.Normalize(NormalizationForm.FormC);
    }
}
