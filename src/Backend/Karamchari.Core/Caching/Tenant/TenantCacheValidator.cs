using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Caching.Tenant;

public sealed class TenantCacheValidator
{
    private readonly ILogger<TenantCacheValidator>? _logger;
    private static readonly char[] Separators = { ':', '_', '/', '\\' };
    private static readonly HashSet<string> ConfusableChars = new(StringComparer.OrdinalIgnoreCase)
    {
        "\u200b",
        "\u200c",
        "\u200d",
        "\ufeff"
    };

    public TenantCacheValidator()
    {
        _logger = null;
    }

    public TenantCacheValidator(ILogger<TenantCacheValidator> logger)
    {
        _logger = logger;
    }

    public TenantCacheValidationResult Validate(string cacheKey)
    {
        var result = new TenantCacheValidationResult();

        if (string.IsNullOrWhiteSpace(cacheKey))
        {
            result.MarkInvalid(TenantCacheValidationError.NullOrEmpty, "Cache key is null or empty.");
            return result;
        }

        if (!cacheKey.StartsWith("tenant_", StringComparison.OrdinalIgnoreCase))
        {
            result.MarkInvalid(
                TenantCacheValidationError.MissingTenantPrefix,
                "Cache key does not start with 'tenant_' prefix.");
            result.AddDiagnostic("Key must have 'tenant_' prefix for tenant isolation.");
            return result;
        }

        var (tenantId, key) = TenantCacheNamespace.Parse(cacheKey);

        if (string.IsNullOrEmpty(tenantId))
        {
            result.MarkInvalid(TenantCacheValidationError.MissingTenantId, "Tenant ID is empty after parsing.");
            return result;
        }

        if (tenantId.Length > 64)
        {
            result.MarkInvalid(
                TenantCacheValidationError.TenantIdTooLong,
                $"Tenant ID '{tenantId}' exceeds maximum length of 64 characters.");
            return result;
        }

        if (!Regex.IsMatch(tenantId, @"^[a-z0-9]+$", RegexOptions.None))
        {
            result.MarkInvalid(
                TenantCacheValidationError.InvalidTenantIdFormat,
                $"Tenant ID '{tenantId}' contains invalid characters.");
            result.AddDiagnostic("Tenant ID must contain only lowercase alphanumeric characters.");
            return result;
        }

        var normalizedTenantId = tenantId.Normalize(NormalizationForm.FormC);
        var hasConfusable = ConfusableChars.Any(c => normalizedTenantId.Contains(c));

        if (hasConfusable)
        {
            result.MarkInvalid(
                TenantCacheValidationError.UnicodeConfusable,
                $"Tenant ID '{tenantId}' contains Unicode confusable characters.");
            result.AddDiagnostic("Detection: potential Unicode homograph attack in tenant ID.");
            return result;
        }

        if (DetectUnicodeCollision(tenantId, out var collision))
        {
            result.MarkInvalid(
                TenantCacheValidationError.UnicodeCollision,
                $"Tenant ID '{tenantId}' is a Unicode confusable variant.");
            result.AddDiagnostic($"Detected Unicode variant: {collision}");
            return result;
        }

        if (string.IsNullOrEmpty(key))
        {
            result.MarkInvalid(TenantCacheValidationError.MissingKey, "Cache key segment is empty after tenant ID.");
            return result;
        }

        if (DetectKeyInjection(cacheKey))
        {
            result.MarkInvalid(
                TenantCacheValidationError.KeyInjection,
                "Cache key contains potential injection pattern.");
            result.AddDiagnostic("Detection: separator injection attempt in cache key.");
            return result;
        }

        if (_logger?.IsEnabled(LogLevel.Debug) == true)
        {
            _logger.LogDebug(
                "Cache key validation succeeded for tenant {TenantId}",
                tenantId);
        }

        return result;
    }

    public TenantCacheValidationResult ValidateWithDiagnostics(string cacheKey)
    {
        var result = Validate(cacheKey);

        if (!result.IsValid)
        {
            if (_logger?.IsEnabled(LogLevel.Warning) == true)
            {
                _logger.LogWarning(
                    "Cache key validation failed for key {CacheKey}: {Error}",
                    cacheKey,
                    result.ErrorMessage);
            }

            result.AddDiagnostic($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC");
            result.AddDiagnostic($"Raw key: {cacheKey}");
            result.AddDiagnostic($"Key length: {cacheKey.Length}");
        }

        return result;
    }

    public string GenerateDiagnosticsReport(string cacheKey)
    {
        var result = ValidateWithDiagnostics(cacheKey);

        var report = new StringBuilder();
        report.AppendLine("=== Cache Key Validation Diagnostics ===");
        report.AppendLine();
        report.AppendLine($"Validation Status: {(result.IsValid ? "VALID" : "INVALID")}");
        report.AppendLine($"Cache Key: {cacheKey}");
        report.AppendLine($"Key Length: {cacheKey.Length}");
        report.AppendLine();

        if (!result.IsValid)
        {
            report.AppendLine($"Error Type: {result.Error}");
            report.AppendLine($"Error Message: {result.ErrorMessage}");
            report.AppendLine();
            report.AppendLine("Diagnostics:");
            foreach (var diagnostic in result.Diagnostics)
            {
                report.AppendLine($"  - {diagnostic}");
            }
        }
        else
        {
            var (tenantId, key) = TenantCacheNamespace.Parse(cacheKey);
            report.AppendLine($"Tenant ID: {tenantId}");
            report.AppendLine($"Cache Key: {key}");
        }

        return report.ToString();
    }

    private static bool DetectUnicodeCollision(string tenantId, out string? collidingForm)
    {
        collidingForm = null;

        var normalized = tenantId.Normalize(NormalizationForm.FormC);
        var forms = new[]
        {
            normalized.ToLowerInvariant(),
            normalized.ToUpperInvariant(),
            normalized.Replace("\ufeff", ""),
            normalized.Replace("\u200b", ""),
            normalized.Replace("\u200c", ""),
            normalized.Replace("\u200d", "")
        };

        var uniqueForms = forms.Distinct().ToList();
        if (uniqueForms.Count < forms.Length)
        {
            collidingForm = tenantId;
            return true;
        }

        return false;
    }

    private static bool DetectKeyInjection(string cacheKey)
    {
        foreach (var sep in Separators)
        {
            var idx = cacheKey.IndexOf(sep);
            if (idx > 0)
            {
                var afterPrefix = cacheKey.Substring(idx + 1);
                if (afterPrefix.StartsWith("tenant_", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }
}

public enum TenantCacheValidationError
{
    None,
    NullOrEmpty,
    MissingTenantPrefix,
    MissingTenantId,
    TenantIdTooLong,
    InvalidTenantIdFormat,
    UnicodeConfusable,
    UnicodeCollision,
    MissingKey,
    KeyInjection
}

public sealed class TenantCacheValidationResult
{
    public bool IsValid { get; private set; } = true;
    public bool HasError => Error != TenantCacheValidationError.None;
    public TenantCacheValidationError Error { get; private set; }
    public string? ErrorMessage { get; private set; }
    public List<string> Diagnostics { get; } = new();

    public void MarkInvalid(TenantCacheValidationError error, string message)
    {
        Error = error;
        ErrorMessage = message;
        IsValid = false;
    }

    public void AddDiagnostic(string diagnostic)
    {
        Diagnostics.Add(diagnostic);
    }

    public void ThrowIfInvalid()
    {
        if (!IsValid)
        {
            throw new TenantCacheValidationException(this);
        }
    }
}

public sealed class TenantCacheValidationException : Exception
{
    public TenantCacheValidationResult ValidationResult { get; }

    public TenantCacheValidationException(TenantCacheValidationResult result)
        : base(result.ErrorMessage)
    {
        ValidationResult = result;
    }
}
