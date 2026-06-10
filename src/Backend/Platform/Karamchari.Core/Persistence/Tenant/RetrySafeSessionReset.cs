// -----------------------------------------------------------------------
// <copyright file="RetrySafeSessionReset.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data;
using System.Data.Common;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Persistence.Tenant;

/// <summary>
/// Provides retry-aware mechanisms for resetting and re-establishing SQL SESSION_CONTEXT,
/// specifically designed to handle transaction rollbacks and connection reuse scenarios.
/// </summary>
public sealed class RetrySafeSessionReset
{
    private const string SessionContextKey = "TenantId";
    private readonly ILogger<RetrySafeSessionReset> _logger;
    private int _retryCount;
    private readonly int _maxRetries;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetrySafeSessionReset"/> class.
    /// </summary>
    /// <param name="logger">The logger for reset events.</param>
    /// <param name="maxRetries">Maximum number of retry attempts allowed.</param>
    public RetrySafeSessionReset(ILogger<RetrySafeSessionReset> logger, int maxRetries = 3)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxRetries = maxRetries;
    }

    /// <summary>Gets the number of retries attempted so far.</summary>
    public int RetryCount => _retryCount;

    /// <summary>Gets the maximum number of retries allowed.</summary>
    public int MaxRetries => _maxRetries;

    /// <summary>Gets a value indicating whether more retries are possible.</summary>
    public bool CanRetry => _retryCount < _maxRetries;

    /// <summary>
    /// Resets the session context by clearing and then setting it to the provided tenant ID.
    /// </summary>
    /// <param name="connection">The SQL connection to reset.</param>
    /// <param name="tenantId">The tenant ID to establish.</param>
    public void ResetAndEstablishContext(SqlConnection connection, string tenantId)
    {
        ClearSessionContext(connection);
        SetSessionContext(connection, tenantId);
        VerifySessionContext(connection, tenantId);

        _logger.LogDebug(
            "Session context reset and re-established for tenant {TenantId}. Retry count: {RetryCount}",
            tenantId,
            _retryCount);
    }

    /// <summary>
    /// Asynchronously resets the session context.
    /// </summary>
    /// <param name="connection">The SQL connection to reset.</param>
    /// <param name="tenantId">The tenant ID to establish.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task ResetAndEstablishContextAsync(SqlConnection connection, string tenantId, CancellationToken cancellationToken = default)
    {
        await ClearSessionContextAsync(connection, cancellationToken).ConfigureAwait(false);
        await SetSessionContextAsync(connection, tenantId, cancellationToken).ConfigureAwait(false);
        await VerifySessionContextAsync(connection, tenantId, cancellationToken).ConfigureAwait(false);

        _logger.LogDebug(
            "Session context reset and re-established for tenant {TenantId} (async). Retry count: {RetryCount}",
            tenantId,
            _retryCount);
    }

    /// <summary>
    /// Handles a retry attempt after a transaction rollback, ensuring the session context is restored.
    /// </summary>
    /// <param name="connection">The SQL connection.</param>
    /// <param name="transaction">The transaction that was rolled back.</param>
    /// <param name="tenantId">The tenant ID to re-establish.</param>
    public void HandleRetryAfterRollback(SqlConnection connection, SqlTransaction? transaction, string tenantId)
    {
        if (transaction != null)
        {
            try
            {
                transaction.Rollback();
                _logger.LogDebug("Transaction rolled back for retry.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to rollback transaction during retry handling.");
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

        _logger.LogInformation(
            "Retrying after rollback. Attempt {RetryCount} of {MaxRetries}",
            _retryCount,
            _maxRetries);
    }

    /// <summary>
    /// Asynchronously handles a retry attempt after a transaction rollback.
    /// </summary>
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
                _logger.LogDebug("Transaction rolled back for retry (async).");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to rollback transaction during retry handling (async).");
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

        _logger.LogInformation(
            "Retrying after rollback (async). Attempt {RetryCount} of {MaxRetries}",
            _retryCount,
            _maxRetries);
    }

    /// <summary>
    /// Handles re-establishing context after a nested transaction rollback.
    /// </summary>
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

        _logger.LogDebug(
            "Session context re-established after nested transaction rollback. Attempt: {RetryCount}",
            _retryCount);
    }

    /// <summary>Resets the retry counter.</summary>
    public void ResetRetryCount()
    {
        _retryCount = 0;
        _logger.LogDebug("Retry count reset to 0.");
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

/// <summary>
/// Exception thrown when all retry attempts for resetting session context have been exhausted.
/// </summary>
public sealed class RetryExhaustedException : Exception
{
    /// <summary>Gets the number of retries attempted.</summary>
    public int RetryCount { get; }

    /// <summary>Gets the maximum allowed retries.</summary>
    public int MaxRetries { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RetryExhaustedException"/> class.
    /// </summary>
    public RetryExhaustedException(int retryCount, int maxRetries, string message)
        : base(message)
    {
        RetryCount = retryCount;
        MaxRetries = maxRetries;
    }
}
