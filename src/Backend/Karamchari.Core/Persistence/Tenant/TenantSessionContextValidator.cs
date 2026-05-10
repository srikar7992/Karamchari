using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Persistence.Tenant;

public sealed class TenantSessionContextValidator
{
    private const string SessionContextKey = "TenantId";
    private readonly ILogger<TenantSessionContextValidator> _logger;

    public TenantSessionContextValidator(ILogger<TenantSessionContextValidator> logger)
    {
        _logger = logger;
    }

    public TenantSessionContextValidationResult Validate(SqlConnection connection, string expectedTenantId)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "Validating SESSION_CONTEXT for expected tenant {ExpectedTenantId}",
                expectedTenantId);
        }

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

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "SESSION_CONTEXT validation succeeded for tenant {TenantId}",
                    expectedTenantId);
            }
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

            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "Error validating SESSION_CONTEXT");
            }
        }

        return result;
    }

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

public enum TenantSessionContextError
{
    None,
    SessionContextNotSet,
    TenantMismatch,
    UnexpectedError
}

public sealed class TenantSessionContextValidationResult
{
    public bool IsValid { get; private set; } = true;
    public bool HasError => Error != TenantSessionContextError.None;
    public TenantSessionContextError Error { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string? ExpectedTenantId { get; set; }
    public string? ActualTenantId { get; set; }
    public int? QueryExecutionTimeMs { get; set; }
    public int? SqlErrorNumber { get; set; }
    public string? SqlErrorMessage { get; set; }
    public Exception? Exception { get; set; }
    public List<string> Diagnostics { get; } = new();

    public void MarkError(TenantSessionContextError error, string message)
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
            throw new TenantSessionContextValidationException(this);
        }
    }
}

public sealed class TenantSessionContextValidationException : Exception
{
    public TenantSessionContextValidationResult ValidationResult { get; }

    public TenantSessionContextValidationException(TenantSessionContextValidationResult result)
        : base(result.ErrorMessage)
    {
        ValidationResult = result;
    }
}
