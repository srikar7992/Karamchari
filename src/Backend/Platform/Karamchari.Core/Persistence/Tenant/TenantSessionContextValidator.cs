// -----------------------------------------------------------------------
// <copyright file="TenantSessionContextValidator.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Persistence.Tenant;

/// <summary>
/// Provides validation and diagnostic capabilities for SQL SESSION_CONTEXT,
/// ensuring that the connection is correctly scoped to the expected tenant.
/// </summary>
public sealed class TenantSessionContextValidator
{
    private const string SessionContextKey = "TenantId";
    private readonly ILogger<TenantSessionContextValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantSessionContextValidator"/> class.
    /// </summary>
    /// <param name="logger">The logger for validation events.</param>
    public TenantSessionContextValidator(ILogger<TenantSessionContextValidator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates that the provided connection has the expected tenant ID set in its SESSION_CONTEXT.
    /// </summary>
    /// <param name="connection">The SQL connection to validate.</param>
    /// <param name="expectedTenantId">The expected tenant identifier.</param>
    /// <returns>A validation result indicating success or failure with details.</returns>
    public TenantSessionContextValidationResult Validate(SqlConnection connection, string expectedTenantId)
    {
        _logger.LogDebug(
            "Validating SESSION_CONTEXT for expected tenant {ExpectedTenantId}",
            expectedTenantId);

        var result = new TenantSessionContextValidationResult();

        try
        {
            var actualTenantId = GetSessionContextValue(connection, result);

            if (result.HasError)
            {
                return result;
            }

            if (string.IsNullOrEmpty(actualTenantId))
            {
                result.MarkError(
                    TenantSessionContextError.SessionContextNotSet,
                    "SESSION_CONTEXT is not set. Connection may be from a fresh pool or context was cleared.");
                return result;
            }

            if (!string.Equals(expectedTenantId, actualTenantId, StringComparison.Ordinal))
            {
                result.MarkError(
                    TenantSessionContextError.TenantMismatch,
                    string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "Tenant mismatch: expected '{0}', actual '{1}'",
                        expectedTenantId,
                        actualTenantId));
                result.ActualTenantId = actualTenantId;
                result.ExpectedTenantId = expectedTenantId;
                return result;
            }

            _logger.LogDebug(
                "SESSION_CONTEXT validation succeeded for tenant {TenantId}",
                expectedTenantId);
        }
        catch (Exception ex)
        {
            result.MarkError(
                TenantSessionContextError.UnexpectedError,
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "Unexpected error during validation: {0}",
                    ex.Message));
            result.Exception = ex;

            _logger.LogError(ex, "Error validating SESSION_CONTEXT");
        }

        return result;
    }

    /// <summary>
    /// Validates the connection and performs additional checks for connection pool contamination.
    /// </summary>
    /// <param name="connection">The SQL connection.</param>
    /// <param name="expectedTenantId">The expected tenant ID.</param>
    /// <returns>A validation result with diagnostic information.</returns>
    public TenantSessionContextValidationResult ValidateWithContaminationCheck(SqlConnection connection, string expectedTenantId)
    {
        var result = Validate(connection, expectedTenantId);

        if (result.IsValid)
        {
            return result;
        }

        if (result.Error == TenantSessionContextError.TenantMismatch)
        {
            if (IsConnectionFromPool(connection))
            {
                result.AddDiagnostic(
                    "DETECTED: Connection was likely reused from connection pool with stale tenant context.");
                result.AddDiagnostic(
                    "RECOMMENDATION: Clear SESSION_CONTEXT before returning connection to pool, or implement connection reset on acquire.");
            }
            else
            {
                result.AddDiagnostic(
                    "DETECTED: Connection was freshly opened but has unexpected tenant context.");
                result.AddDiagnostic(
                    "RECOMMENDATION: Review interceptor configuration and connection acquisition flow.");
            }

            result.AddDiagnostic(
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "Cross-tenant contamination risk: Expected tenant '{0}', found tenant '{1}'",
                    expectedTenantId,
                    result.ActualTenantId ?? "(null)"));
        }

        return result;
    }

    private static string? GetSessionContextValue(SqlConnection connection, TenantSessionContextValidationResult result)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT SESSION_CONTEXT(N'TenantId') AS TenantId;";
        cmd.CommandType = CommandType.Text;

        var executionStart = DateTime.UtcNow;

        try
        {
            var value = cmd.ExecuteScalar();
            result.QueryExecutionTimeMs = (int)(DateTime.UtcNow - executionStart).TotalMilliseconds;
            return value as string;
        }
        catch (SqlException ex)
        {
            result.SqlErrorNumber = ex.Number;
            result.SqlErrorMessage = ex.Message;
            throw;
        }
    }

    private static bool IsConnectionFromPool(SqlConnection connection)
    {
        try
        {
            var poolGroup = connection.GetType()
                .GetProperty("PoolGroup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                .GetValue(connection);

            if (poolGroup != null)
            {
                return true;
            }
        }
        catch
        {
        }

        return connection.ConnectionString.Contains("Pooling=true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Generates a human-readable diagnostic report for a connection's session context state.
    /// </summary>
    public string GenerateDiagnosticsReport(SqlConnection connection, string expectedTenantId)
    {
        var result = ValidateWithContaminationCheck(connection, expectedTenantId);

        var report = new System.Text.StringBuilder();
        report.AppendLine("=== SESSION_CONTEXT Validation Diagnostics ===");
        report.AppendLine();
        report.AppendLine($"Validation Status: {(result.IsValid ? "VALID" : "INVALID")}");
        report.AppendLine($"Expected Tenant: {expectedTenantId}");
        report.AppendLine($"Actual Tenant: {result.ActualTenantId ?? "(not set)"}");
        report.AppendLine();

        if (result.HasError)
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

        if (result.QueryExecutionTimeMs.HasValue)
        {
            report.AppendLine();
            report.AppendLine($"Query Execution Time: {result.QueryExecutionTimeMs}ms");
        }

        if (result.SqlErrorNumber.HasValue)
        {
            report.AppendLine();
            report.AppendLine($"SQL Error Number: {result.SqlErrorNumber}");
            report.AppendLine($"SQL Error Message: {result.SqlErrorMessage}");
        }

        return report.ToString();
    }
}

/// <summary>
/// Possible error types during session context validation.
/// </summary>
public enum TenantSessionContextError
{
    /// <summary>No error occurred.</summary>
    None,
    /// <summary>The session context was not set on the connection.</summary>
    SessionContextNotSet,
    /// <summary>The tenant ID in session context does not match expected ID.</summary>
    TenantMismatch,
    /// <summary>An unexpected error occurred during validation.</summary>
    UnexpectedError
}

/// <summary>
/// Result of a tenant session context validation attempt.
/// </summary>
public sealed class TenantSessionContextValidationResult
{
    /// <summary>Gets whether the validation passed.</summary>
    public bool IsValid { get; private set; } = true;

    /// <summary>Gets whether an error occurred.</summary>
    public bool HasError => Error != TenantSessionContextError.None;

    /// <summary>Gets the specific error type.</summary>
    public TenantSessionContextError Error { get; private set; }

    /// <summary>Gets the error message, if any.</summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>Gets or sets the expected tenant ID.</summary>
    public string? ExpectedTenantId { get; set; }

    /// <summary>Gets or sets the actual tenant ID found.</summary>
    public string? ActualTenantId { get; set; }

    /// <summary>Gets or sets the query execution time in milliseconds.</summary>
    public int? QueryExecutionTimeMs { get; set; }

    /// <summary>Gets or sets the SQL error number if a database error occurred.</summary>
    public int? SqlErrorNumber { get; set; }

    /// <summary>Gets or sets the SQL error message.</summary>
    public string? SqlErrorMessage { get; set; }

    /// <summary>Gets or sets the exception that occurred during validation.</summary>
    public Exception? Exception { get; set; }

    /// <summary>Gets the collection of diagnostic messages.</summary>
    public List<string> Diagnostics { get; } = new();

    /// <summary>Marks the result as an error.</summary>
    public void MarkError(TenantSessionContextError error, string message)
    {
        Error = error;
        ErrorMessage = message;
        IsValid = false;
    }

    /// <summary>Adds a diagnostic message.</summary>
    public void AddDiagnostic(string diagnostic)
    {
        Diagnostics.Add(diagnostic);
    }

    /// <summary>Throws a <see cref="TenantSessionContextValidationException"/> if the result is invalid.</summary>
    public void ThrowIfInvalid()
    {
        if (!IsValid)
        {
            throw new TenantSessionContextValidationException(this);
        }
    }
}

/// <summary>
/// Exception thrown when tenant session context validation fails.
/// </summary>
public sealed class TenantSessionContextValidationException : Exception
{
    /// <summary>Gets the detailed validation result.</summary>
    public TenantSessionContextValidationResult ValidationResult { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantSessionContextValidationException"/> class.
    /// </summary>
    public TenantSessionContextValidationException(TenantSessionContextValidationResult result)
        : base(result.ErrorMessage)
    {
        ValidationResult = result;
    }
}
