using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Caching.Tenant;

public sealed class TenantCacheKeyBuilder
{
    private const int MaxKeyLength = 256;
    private const int MaxTenantIdLength = 64;
    private static readonly char[] InvalidKeyChars = { '\0', '\n', '\r', '\t', '\v' };
    private static readonly char[] KeySeparators = { ':', '_', '/', '\\', '.', '[', ']' };
    private readonly ILogger<TenantCacheKeyBuilder>? _logger;

    private static readonly HashSet<string> ConfusableTenants = new(StringComparer.OrdinalIgnoreCase)
    {
        "\u200b",
        "\u200c",
        "\u200d",
        "\ufeff"
    };

    public TenantCacheKeyBuilder()
    {
        _logger = null;
    }

    public TenantCacheKeyBuilder(ILogger<TenantCacheKeyBuilder> logger)
    {
        _logger = logger;
    }

    public string Build(string tenantId, string key)
    {
        return Build(tenantId, null, key);
    }

    public string Build(string tenantId, string? category, string key)
    {
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
            if (_logger?.IsEnabled(LogLevel.Warning) == true)
            {
                _logger.LogWarning(
                    "Tenant ID '{TenantId}' contains Unicode characters that normalize differently. Original: {Normalized}",
                    tenantId,
                    normalized);
            }
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
            if (_logger?.IsEnabled(LogLevel.Warning) == true)
            {
                _logger.LogWarning(
                    "Key '{Key}' contains Unicode characters that normalize differently.",
                    key);
            }
        }
    }

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
