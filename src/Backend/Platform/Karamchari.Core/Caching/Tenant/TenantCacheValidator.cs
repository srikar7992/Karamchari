// -----------------------------------------------------------------------
// <copyright file="TenantCacheValidator.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Caching.Tenant;

/// <summary>
/// Validator for tenant cache keys that performs deep inspection for security violations,
/// including Unicode homograph attacks, key injection, and format inconsistencies.
/// </summary>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantCacheValidator"/> class.
    /// </summary>
    public TenantCacheValidator()
    {
        _logger = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantCacheValidator"/> class with logging.
    /// </summary>
    /// <param name="logger">The logger for diagnostic output.</param>
    public TenantCacheValidator(ILogger<TenantCacheValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates a namespaced cache key against strict security and format rules.
    /// </summary>
    /// <param name="cacheKey">The namespaced cache key to validate.</param>
    /// <returns>A validation result indicating success or failure with detailed errors.</returns>
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

        string tenantId;
        string key;
        try
        {
            (tenantId, key) = TenantCacheNamespace.Parse(cacheKey);
        }
        catch (ArgumentException ex)
        {
            result.MarkInvalid(TenantCacheValidationError.InvalidTenantIdFormat, ex.Message);
            return result;
        }

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

        _logger?.LogDebug("Cache key validation succeeded for tenant {TenantId}", tenantId);

        return result;
    }

    /// <summary>
    /// Validates the key and adds diagnostic information if it's invalid.
    /// </summary>
    public TenantCacheValidationResult ValidateWithDiagnostics(string cacheKey)
    {
        var result = Validate(cacheKey);

        if (!result.IsValid)
        {
            _logger?.LogWarning("Cache key validation failed for key {CacheKey}: {Error}", cacheKey, result.ErrorMessage);

            result.AddDiagnostic($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC");
            result.AddDiagnostic($"Raw key: {cacheKey}");
            result.AddDiagnostic($"Key length: {cacheKey.Length}");
        }

        return result;
    }

    /// <summary>
    /// Generates a human-readable diagnostics report for a cache key.
    /// </summary>
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
        var filteredForms = new[]
        {
            normalized.Replace("\ufeff", ""),
            normalized.Replace("\u200b", ""),
            normalized.Replace("\u200c", ""),
            normalized.Replace("\u200d", "")
        };

        // Check if any filtered form is different from the normalized one
        foreach (var form in filteredForms)
        {
            if (form != normalized)
            {
                collidingForm = form;
                return true;
            }
        }

        // Also check for case folding collisions if needed, but tenant IDs are lowercase alphanumeric
        return false;
    }

    private static bool DetectKeyInjection(string cacheKey)
    {
        var firstSepIdx = cacheKey.IndexOfAny(Separators);
        if (firstSepIdx > 0)
        {
            var keyPart = cacheKey.Substring(firstSepIdx + 1);
            if (keyPart.Contains("tenant_", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

/// <summary>
/// Possible validation errors for tenant cache keys.
/// </summary>
public enum TenantCacheValidationError
{
    /// <summary>No error.</summary>
    None,
    /// <summary>The key is null or empty.</summary>
    NullOrEmpty,
    /// <summary>The key is missing the mandatory 'tenant_' prefix.</summary>
    MissingTenantPrefix,
    /// <summary>The tenant identifier is missing or empty.</summary>
    MissingTenantId,
    /// <summary>The tenant identifier exceeds the maximum allowed length.</summary>
    TenantIdTooLong,
    /// <summary>The tenant identifier format is invalid.</summary>
    InvalidTenantIdFormat,
    /// <summary>The tenant identifier contains confusable Unicode characters.</summary>
    UnicodeConfusable,
    /// <summary>The tenant identifier has multiple Unicode representations.</summary>
    UnicodeCollision,
    /// <summary>The base cache key part is missing.</summary>
    MissingKey,
    /// <summary>The key contains a potential injection pattern.</summary>
    KeyInjection
}

/// <summary>
/// Result of a tenant cache key validation attempt.
/// </summary>
public sealed class TenantCacheValidationResult
{
    /// <summary>Gets whether the validation passed.</summary>
    public bool IsValid { get; private set; } = true;

    /// <summary>Gets whether any error occurred.</summary>
    public bool HasError => Error != TenantCacheValidationError.None;

    /// <summary>Gets the specific validation error type.</summary>
    public TenantCacheValidationError Error { get; private set; }

    /// <summary>Gets the human-readable error message.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Gets the collection of diagnostic messages.</summary>
    public List<string> Diagnostics { get; } = new();

    /// <summary>Marks the result as invalid with an error and message.</summary>
    public void MarkInvalid(TenantCacheValidationError error, string message)
    {
        Error = error;
        ErrorMessage = message;
        IsValid = false;
    }

    /// <summary>Adds a diagnostic message to the result.</summary>
    public void AddDiagnostic(string diagnostic)
    {
        Diagnostics.Add(diagnostic);
    }

    /// <summary>Throws a <see cref="TenantCacheValidationException"/> if the result is invalid.</summary>
    public void ThrowIfInvalid()
    {
        if (!IsValid)
        {
            throw new TenantCacheValidationException(this);
        }
    }
}

/// <summary>
/// Exception thrown when a tenant cache key validation fails.
/// </summary>
public sealed class TenantCacheValidationException : Exception
{
    /// <summary>Gets the detailed validation result.</summary>
    public TenantCacheValidationResult ValidationResult { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantCacheValidationException"/> class.
    /// </summary>
    public TenantCacheValidationException(TenantCacheValidationResult result)
        : base(result.ErrorMessage)
    {
        ValidationResult = result;
    }
}
