using System.Data.Common;
using System.Globalization;
using System.Text.RegularExpressions;
using Karamchari.Core.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Persistence.Filters;

/// <summary>
/// EF Core interceptor that validates database commands to ensure tenant isolation,
/// blocking unsafe patterns and ensuring tenant context is present for all queries.
/// </summary>
public sealed class TenantQueryValidationInterceptor : DbCommandInterceptor
{
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<TenantQueryValidationInterceptor> _logger;

    private static readonly Regex DangerousSqlPattern = new(
        @"(\bUNION\b|\bSELECT\b.*\bFROM\b|\bDROP\b|\bDELETE\b.*\bWHERE\b|\bINSERT\b.*\bVALUES\b.*\bSELECT\b)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantQueryValidationInterceptor"/> class.
    /// </summary>
    /// <param name="tenantProvider">The tenant provider to resolve current tenant context.</param>
    /// <param name="logger">The logger for validation events.</param>
    public TenantQueryValidationInterceptor(
        ITenantProvider tenantProvider,
        ILogger<TenantQueryValidationInterceptor> logger)
    {
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        ValidateQueryExecution(command.CommandText, isAsync: false);
        return result;
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        ValidateQueryExecution(command.CommandText, isAsync: true);
        return new ValueTask<InterceptionResult<DbDataReader>>(result);
    }

    /// <inheritdoc/>
    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        ValidateQueryExecution(command.CommandText, isAsync: false);
        return result;
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ValidateQueryExecution(command.CommandText, isAsync: true);
        return new ValueTask<InterceptionResult<int>>(result);
    }

    /// <inheritdoc/>
    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        ValidateQueryExecution(command.CommandText, isAsync: false);
        return result;
    }

    /// <inheritdoc/>
    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        ValidateQueryExecution(command.CommandText, isAsync: true);
        return new ValueTask<InterceptionResult<object>>(result);
    }

    private void ValidateQueryExecution(string commandText, bool isAsync)
    {
        if (string.IsNullOrWhiteSpace(commandText))
        {
            return;
        }

        if (!_tenantProvider.TryGetTenant(out var tenant) || tenant is null)
        {
            _logger.LogCritical(
                "CRITICAL: Query attempted without tenant context. Stack: {StackTrace}",
                Environment.StackTrace);
            throw new UnsafeQueryAccessException(
                "Query execution requires an active tenant context.");
        }

        DetectIgnoreQueryFilters(commandText);
        BlockUnsafeRawSql(commandText, tenant.TenantId);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "TenantId={TenantId} executing query (async={IsAsync}): {CommandText}",
                tenant.TenantId,
                isAsync.ToString(CultureInfo.InvariantCulture),
                SanitizeForLog(commandText));
        }
    }

    private void DetectIgnoreQueryFilters(string commandText)
    {
        if (commandText.Contains("IgnoreQueryFilters", StringComparison.OrdinalIgnoreCase) ||
            commandText.Contains("IgnoreAllQueryFilters", StringComparison.OrdinalIgnoreCase))
        {
            if (_tenantProvider.TryGetTenant(out var tenant))
            {
                _logger.LogWarning(
                    "SECURITY: IgnoreQueryFilters called with tenant context {TenantId}. Stack: {StackTrace}",
                    tenant?.TenantId ?? "Unknown",
                    Environment.StackTrace);
            }
        }
    }

    private void BlockUnsafeRawSql(string commandText, string currentTenantId)
    {
        if (DangerousSqlPattern.IsMatch(commandText))
        {
            _logger.LogCritical(
                "CRITICAL: Potentially unsafe SQL pattern detected. Tenant={TenantId}. Pattern matched in query.",
                currentTenantId);
            throw new UnsafeQueryAccessException(
                $"Blocked unsafe SQL pattern. Tenant={currentTenantId}");
        }

        if (commandText.Contains("dbo.", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogCritical(
                "CRITICAL: Cross-schema reference 'dbo.' detected without explicit tenant filtering. Tenant={TenantId}",
                currentTenantId);
            throw new UnsafeQueryAccessException(
                "Cross-schema queries without explicit tenant filtering are not allowed.");
        }
    }

    private static string SanitizeForLog(string commandText)
    {
        if (string.IsNullOrWhiteSpace(commandText))
        {
            return "[empty]";
        }

        const int maxLength = 500;
        if (commandText.Length <= maxLength)
        {
            return commandText;
        }

        return commandText[..maxLength] + "...[truncated]";
    }
}
