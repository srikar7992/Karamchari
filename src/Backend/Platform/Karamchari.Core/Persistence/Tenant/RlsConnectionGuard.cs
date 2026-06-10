// -----------------------------------------------------------------------
// <copyright file="RlsConnectionGuard.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Karamchari.Core.Persistence.Tenant;

/// <summary>
/// Defensive guard for database connections that ensures SESSION_CONTEXT is correctly established
/// for the current tenant. Provides protection against connection pool contamination.
/// </summary>
public sealed class RlsConnectionGuard : IDisposable
{
    private const string SessionContextKey = "TenantId";
    private static readonly ConcurrentDictionary<int, TrackedConnection> _trackedConnections = new();

    private readonly DbConnection _connection;
    private readonly string _tenantId;
    private readonly ILogger<RlsConnectionGuard> _logger;
    private bool _disposed;

    private sealed class TrackedConnection
    {
        public string TenantId { get; set; } = string.Empty;
        public int AcquireCount { get; set; }
    }

    private RlsConnectionGuard(DbConnection connection, string tenantId, ILogger<RlsConnectionGuard> logger)
    {
        _connection = connection;
        _tenantId = tenantId;
        _logger = logger;
    }

    /// <summary>
    /// Acquires a guard for the provided connection, establishing tenant session context.
    /// </summary>
    /// <param name="connection">The database connection.</param>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>A new <see cref="RlsConnectionGuard"/> instance.</returns>
    public static RlsConnectionGuard Acquire(DbConnection connection, string tenantId)
    {
        var logger = GetLogger(connection);
        ClearSessionContext(connection, logger);
        SetSessionContext(connection, tenantId, logger);
        ValidateSessionContext(connection, tenantId, logger);

        var connectionHash = connection.GetHashCode();
        _trackedConnections.AddOrUpdate(
            connectionHash,
            _ => new TrackedConnection { TenantId = tenantId, AcquireCount = 1 },
            (_, existing) =>
            {
                existing.TenantId = tenantId;
                existing.AcquireCount++;
                return existing;
            });

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug(
                "RLS connection guard acquired for tenant {TenantId}. Connection hash: {ConnectionHash}",
                tenantId,
                connectionHash);
        }

        return new RlsConnectionGuard(connection, tenantId, logger);
    }

    /// <summary>
    /// Validates that the connection's session context matches the expected tenant.
    /// </summary>
    /// <param name="expectedTenantId">The expected tenant identifier.</param>
    /// <exception cref="TenantSessionContextMismatchException">Thrown when a mismatch is detected.</exception>
    public void ValidateSessionContext(string expectedTenantId)
    {
        ThrowIfDisposed();

        if (_connection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException("Cannot validate SESSION_CONTEXT on a closed connection.");
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT SESSION_CONTEXT(N'TenantId') AS TenantId;";
        cmd.CommandType = CommandType.Text;

        var result = cmd.ExecuteScalar();
        var actualTenantId = result as string;

        if (!string.Equals(expectedTenantId, actualTenantId, StringComparison.Ordinal))
        {
            throw new TenantSessionContextMismatchException(
                expectedTenantId,
                actualTenantId,
                "SESSION_CONTEXT tenant ID mismatch during validation.");
        }

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace(
                "SESSION_CONTEXT validated for tenant {TenantId}. Actual value: {ActualTenantId}",
                expectedTenantId,
                actualTenantId ?? "(null)");
        }
    }

    /// <summary>
    /// Manually clears the session context from the connection.
    /// </summary>
    public void ClearSessionContext()
    {
        ThrowIfDisposed();
        ClearSessionContext(_connection, _logger);
        var connectionHash = _connection.GetHashCode();
        _trackedConnections.TryRemove(connectionHash, out _);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "SESSION_CONTEXT cleared for tenant {TenantId}. Connection hash: {ConnectionHash}",
                _tenantId,
                connectionHash);
        }
    }

    private static void ClearSessionContext(DbConnection connection, ILogger<RlsConnectionGuard>? logger)
    {
        ArgumentNullException.ThrowIfNull(connection);
        logger ??= NullLogger<RlsConnectionGuard>.Instance;

        if (connection.State != ConnectionState.Open)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace("Skipping SESSION_CONTEXT clear: Connection state is {ConnectionState}.", connection.State);
            }
            return;
        }

        using var cmd = connection.CreateCommand();
        if (cmd == null) return;

        cmd.CommandText = "EXEC sp_set_session_context @key = @key, @value = NULL;";
        cmd.CommandType = CommandType.Text;

        var keyParam = cmd.CreateParameter();
        keyParam.ParameterName = "@key";
        keyParam.DbType = DbType.String;
        keyParam.Value = SessionContextKey;
        cmd.Parameters.Add(keyParam);

        cmd.ExecuteNonQuery();

        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("SESSION_CONTEXT cleared.");
        }
    }

    private static void SetSessionContext(DbConnection connection, string tenantId, ILogger<RlsConnectionGuard>? logger)
    {
        ArgumentNullException.ThrowIfNull(connection);
        logger ??= NullLogger<RlsConnectionGuard>.Instance;

        if (connection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException("Cannot set SESSION_CONTEXT on a closed connection.");
        }

        using var cmd = connection.CreateCommand();
        if (cmd == null) throw new InvalidOperationException("Failed to create database command.");

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

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("SESSION_CONTEXT set for tenant {TenantId}.", tenantId);
        }
    }

    private static void ValidateSessionContext(DbConnection connection, string expectedTenantId, ILogger<RlsConnectionGuard>? logger)
    {
        ArgumentNullException.ThrowIfNull(connection);
        logger ??= NullLogger<RlsConnectionGuard>.Instance;

        if (connection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException("Cannot verify SESSION_CONTEXT on a closed connection.");
        }

        using var cmd = connection.CreateCommand();
        if (cmd == null) throw new InvalidOperationException("Failed to create database command.");

        cmd.CommandText = "SELECT SESSION_CONTEXT(N'TenantId') AS TenantId;";
        cmd.CommandType = CommandType.Text;

        var result = cmd.ExecuteScalar();
        var actualTenantId = result as string;

        if (!string.Equals(expectedTenantId, actualTenantId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "Failed to set SESSION_CONTEXT. Expected: {0}, Actual: {1}",
                    expectedTenantId,
                    actualTenantId ?? "(null)"));
        }

        if (logger.IsEnabled(LogLevel.Trace))
        {
            logger.LogTrace("SESSION_CONTEXT verified after set operation.");
        }
    }

    private static ILogger<RlsConnectionGuard> GetLogger(DbConnection connection)
    {
        return NullLogger<RlsConnectionGuard>.Instance;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(RlsConnectionGuard));
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        ClearSessionContext();
        _disposed = true;

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("RLS connection guard disposed for tenant {TenantId}.", _tenantId);
        }
    }
}

/// <summary>
/// Exception thrown when the session context tenant ID does not match the expected identifier.
/// </summary>
public sealed class TenantSessionContextMismatchException : Exception
{
    /// <summary>Gets the expected tenant identifier.</summary>
    public string ExpectedTenantId { get; }

    /// <summary>Gets the actual tenant identifier found in session context.</summary>
    public string? ActualTenantId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantSessionContextMismatchException"/> class.
    /// </summary>
    public TenantSessionContextMismatchException(string expectedTenantId, string? actualTenantId, string message)
        : base(message)
    {
        ExpectedTenantId = expectedTenantId;
        ActualTenantId = actualTenantId;
    }
}
