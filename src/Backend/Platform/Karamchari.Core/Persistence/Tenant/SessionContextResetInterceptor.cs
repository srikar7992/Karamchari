using System.Data;
using System.Data.Common;
using Karamchari.Core.Multitenancy;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Persistence.Tenant;

public sealed class SessionContextResetInterceptor : DbCommandInterceptor
{
    private const string SessionContextKey = "TenantId";
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<SessionContextResetInterceptor> _logger;
    private string? _lastTenantId;

    public SessionContextResetInterceptor(ITenantProvider tenantProvider, ILogger<SessionContextResetInterceptor> logger)
    {
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        ValidateSessionContextBeforeExecution(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        ValidateSessionContextBeforeExecution(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        ValidateSessionContextBeforeExecution(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ValidateSessionContextBeforeExecution(command);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        ValidateSessionContextBeforeExecution(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        ValidateSessionContextBeforeExecution(command);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void ValidateSessionContextBeforeExecution(DbCommand command)
    {
        if (!_tenantProvider.TryGetTenant(out var tenant) || tenant is null)
        {
            return;
        }

        var expectedTenantId = tenant.TenantId;

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace(
                "Validating SESSION_CONTEXT before executing command: {CommandText}",
                command.CommandText);
        }

        var connection = command.Connection;
        if (connection == null)
        {
            return;
        }

        if (ShouldSkipValidation(command))
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Skipping SESSION_CONTEXT validation for command type.");
            }
            return;
        }

        var actualTenantId = GetSessionContextValue((SqlConnection)connection);

        if (string.IsNullOrEmpty(actualTenantId))
        {
            throw new TenantSessionContextNotSetException(
                expectedTenantId,
                "SESSION_CONTEXT is not set before query execution. This may indicate a connection pooling contamination issue.");
        }

        if (!string.Equals(expectedTenantId, actualTenantId, StringComparison.Ordinal))
        {
            throw new PooledConnectionContaminationException(
                expectedTenantId,
                actualTenantId,
                "Cross-tenant contamination detected: SESSION_CONTEXT tenant does not match expected tenant. Connection may be from a contaminated pool.");
        }

        if (_lastTenantId != null && !string.Equals(_lastTenantId, actualTenantId, StringComparison.Ordinal))
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "Connection reuse detected with different tenant context. Previous: {PreviousTenant}, Current: {CurrentTenant}",
                    _lastTenantId,
                    actualTenantId);
            }
        }

        _lastTenantId = actualTenantId;

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "SESSION_CONTEXT validated. Expected: {ExpectedTenant}, Actual: {ActualTenant}",
                expectedTenantId,
                actualTenantId);
        }
    }

    private static string? GetSessionContextValue(SqlConnection connection)
    {
        try
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT SESSION_CONTEXT(N'TenantId') AS TenantId;";
            cmd.CommandType = CommandType.Text;

            var result = cmd.ExecuteScalar();
            return result as string;
        }
        catch
        {
            return null;
        }
    }

    private static bool ShouldSkipValidation(DbCommand command)
    {
        var commandText = command.CommandText;
        if (string.IsNullOrWhiteSpace(commandText))
        {
            return true;
        }

        var upperCommandText = commandText.ToUpperInvariant();

        if (upperCommandText.Contains("SP_SET_SESSION_CONTEXT") ||
            upperCommandText.Contains("SESSION_CONTEXT"))
        {
            return true;
        }

        return false;
    }

    public void ResetLastTenantId()
    {
        _lastTenantId = null;
    }
}

public sealed class TenantSessionContextNotSetException : Exception
{
    public string TenantId { get; }

    public TenantSessionContextNotSetException(string tenantId, string message)
        : base(message)
    {
        TenantId = tenantId;
    }
}

