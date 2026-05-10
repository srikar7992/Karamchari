using System.Data;
using System.Data.Common;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Persistence.Tenant;

public sealed class RetrySafeSessionReset
{
    private const string SessionContextKey = "TenantId";
    private readonly ILogger<RetrySafeSessionReset> _logger;
    private int _retryCount;
    private int _maxRetries;

    public RetrySafeSessionReset(ILogger<RetrySafeSessionReset> logger, int maxRetries = 3)
    {
        _logger = logger;
        _maxRetries = maxRetries;
    }

    public int RetryCount => _retryCount;
    public int MaxRetries => _maxRetries;
    public bool CanRetry => _retryCount < _maxRetries;

    public void ResetAndEstablishContext(SqlConnection connection, string tenantId)
    {
        ClearSessionContext(connection);
        SetSessionContext(connection, tenantId);
        VerifySessionContext(connection, tenantId);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "Session context reset and re-established for tenant {TenantId}. Retry count: {RetryCount}",
                tenantId,
                _retryCount);
        }
    }

    public async Task ResetAndEstablishContextAsync(SqlConnection connection, string tenantId, CancellationToken cancellationToken = default)
    {
        await ClearSessionContextAsync(connection, cancellationToken).ConfigureAwait(false);
        await SetSessionContextAsync(connection, tenantId, cancellationToken).ConfigureAwait(false);
        await VerifySessionContextAsync(connection, tenantId, cancellationToken).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "Session context reset and re-established for tenant {TenantId} (async). Retry count: {RetryCount}",
                tenantId,
                _retryCount);
        }
    }

    public void HandleRetryAfterRollback(SqlConnection connection, SqlTransaction? transaction, string tenantId)
    {
        if (transaction != null)
        {
            try
            {
                transaction.Rollback();
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Transaction rolled back for retry.");
                }
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(ex, "Failed to rollback transaction during retry handling.");
                }
            }
        }

        _retryCount++;

        if (!CanRetry)
        {
            throw new RetryExhaustedException(
                _retryCount,
                _maxRetries,
                "Maximum retry attempts exhausted for session context reset.");
        }

        ResetAndEstablishContext(connection, tenantId);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Retrying after rollback. Attempt {RetryCount} of {MaxRetries}",
                _retryCount,
                _maxRetries);
        }
    }

    public async Task HandleRetryAfterRollbackAsync(
        SqlConnection connection,
        SqlTransaction? transaction,
        string tenantId,
        CancellationToken cancellationToken = default)
    {
        if (transaction != null)
        {
            try
            {
                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Transaction rolled back for retry (async).");
                }
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(ex, "Failed to rollback transaction during retry handling (async).");
                }
            }
        }

        _retryCount++;

        if (!CanRetry)
        {
            throw new RetryExhaustedException(
                _retryCount,
                _maxRetries,
                "Maximum retry attempts exhausted for session context reset.");
        }

        await ResetAndEstablishContextAsync(connection, tenantId, cancellationToken).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Retrying after rollback (async). Attempt {RetryCount} of {MaxRetries}",
                _retryCount,
                _maxRetries);
        }
    }

    public void HandleNestedTransactionRollback(SqlConnection connection, string tenantId)
    {
        ClearSessionContext(connection);

        _retryCount++;

        if (!CanRetry)
        {
            throw new RetryExhaustedException(
                _retryCount,
                _maxRetries,
                "Maximum retry attempts exhausted in nested transaction scenario.");
        }

        SetSessionContext(connection, tenantId);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "Session context re-established after nested transaction rollback. Attempt: {RetryCount}",
                _retryCount);
        }
    }

    public void ResetRetryCount()
    {
        _retryCount = 0;

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Retry count reset to 0.");
        }
    }

    private static void ClearSessionContext(SqlConnection connection)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "EXEC sp_set_session_context @key = @key, @value = NULL;";
        cmd.CommandType = CommandType.Text;

        var keyParam = cmd.CreateParameter();
        keyParam.ParameterName = "@key";
        keyParam.DbType = DbType.String;
        keyParam.Value = SessionContextKey;
        cmd.Parameters.Add(keyParam);

        cmd.ExecuteNonQuery();
    }

    private static async Task ClearSessionContextAsync(SqlConnection connection, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "EXEC sp_set_session_context @key = @key, @value = NULL;";
        cmd.CommandType = CommandType.Text;

        var keyParam = cmd.CreateParameter();
        keyParam.ParameterName = "@key";
        keyParam.DbType = DbType.String;
        keyParam.Value = SessionContextKey;
        cmd.Parameters.Add(keyParam);

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void SetSessionContext(SqlConnection connection, string tenantId)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "EXEC sp_set_session_context @key = @key, @value = @value;";
        cmd.CommandType = CommandType.Text;

        var keyParam = cmd.CreateParameter();
        keyParam.ParameterName = "@key";
        keyParam.DbType = DbType.String;
        keyParam.Value = SessionContextKey;
        cmd.Parameters.Add(keyParam);

        var valueParam = cmd.CreateParameter();
        valueParam.ParameterName = "@value";
        valueParam.DbType = DbType.String;
        valueParam.Value = tenantId;
        cmd.Parameters.Add(valueParam);

        cmd.ExecuteNonQuery();
    }

    private static async Task SetSessionContextAsync(SqlConnection connection, string tenantId, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "EXEC sp_set_session_context @key = @key, @value = @value;";
        cmd.CommandType = CommandType.Text;

        var keyParam = cmd.CreateParameter();
        keyParam.ParameterName = "@key";
        keyParam.DbType = DbType.String;
        keyParam.Value = SessionContextKey;
        cmd.Parameters.Add(keyParam);

        var valueParam = cmd.CreateParameter();
        valueParam.ParameterName = "@value";
        valueParam.DbType = DbType.String;
        valueParam.Value = tenantId;
        cmd.Parameters.Add(valueParam);

        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void VerifySessionContext(SqlConnection connection, string expectedTenantId)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT SESSION_CONTEXT(N'TenantId') AS TenantId;";
        cmd.CommandType = CommandType.Text;

        var result = cmd.ExecuteScalar();
        var actualTenantId = result as string;

        if (!string.Equals(expectedTenantId, actualTenantId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Failed to verify SESSION_CONTEXT after reset. Expected: {0}, Actual: {1}",
                    expectedTenantId,
                    actualTenantId ?? "(null)"));
        }
    }

    private static async Task VerifySessionContextAsync(SqlConnection connection, string expectedTenantId, CancellationToken cancellationToken)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT SESSION_CONTEXT(N'TenantId') AS TenantId;";
        cmd.CommandType = CommandType.Text;

        var result = await cmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        var actualTenantId = result as string;

        if (!string.Equals(expectedTenantId, actualTenantId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "Failed to verify SESSION_CONTEXT after reset (async). Expected: {0}, Actual: {1}",
                    expectedTenantId,
                    actualTenantId ?? "(null)"));
        }
    }
}

public sealed class RetryExhaustedException : Exception
{
    public int RetryCount { get; }
    public int MaxRetries { get; }

    public RetryExhaustedException(int retryCount, int maxRetries, string message)
        : base(message)
    {
        RetryCount = retryCount;
        MaxRetries = maxRetries;
    }
}
