using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Karamchari.Core.Persistence.Tenant;

public sealed class RlsConnectionGuard : IDisposable
{
    private const string SessionContextKey = "TenantId";
    private static readonly ConcurrentDictionary<int, TrackedConnection> _trackedConnections = new();

    private readonly SqlConnection _connection;
    private readonly string _tenantId;
    private readonly ILogger<RlsConnectionGuard> _logger;
    private bool _disposed;

    private sealed class TrackedConnection
    {
        public string TenantId { get; set; } = string.Empty;
        public int AcquireCount { get; set; }
    }

    private RlsConnectionGuard(SqlConnection connection, string tenantId, ILogger<RlsConnectionGuard> logger)
    {
        _connection = connection;
        _tenantId = tenantId;
        _logger = logger;
    }

    public static RlsConnectionGuard Acquire(SqlConnection connection, string tenantId)
    {
        var logger = GetLogger(connection);
        ClearSessionContext(connection, logger);
        SetSessionContext(connection, tenantId, logger);
        ValidateSessionContext(connection, tenantId, logger);

        var trackedConnections = _trackedConnections;
        var connectionHash = connection.GetHashCode();
        trackedConnections.AddOrUpdate(
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

    private static void ClearSessionContext(SqlConnection connection, ILogger<RlsConnectionGuard> logger)
    {
        if (connection.State != ConnectionState.Open)
        {
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace("Skipping SESSION_CONTEXT clear: Connection state is {ConnectionState}.", connection.State);
            }
            return;
        }

        using var cmd = connection.CreateCommand();
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

    private static void SetSessionContext(SqlConnection connection, string tenantId, ILogger<RlsConnectionGuard> logger)
    {
        if (connection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException("Cannot set SESSION_CONTEXT on a closed connection.");
        }

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

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("SESSION_CONTEXT set for tenant {TenantId}.", tenantId);
        }
    }

    private static void ValidateSessionContext(SqlConnection connection, string expectedTenantId, ILogger<RlsConnectionGuard> logger)
    {
        if (connection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException("Cannot verify SESSION_CONTEXT on a closed connection.");
        }

        using var cmd = connection.CreateCommand();
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

    private static ILogger<RlsConnectionGuard> GetLogger(SqlConnection connection)
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

public sealed class TenantSessionContextMismatchException : Exception
{
    public string ExpectedTenantId { get; }
    public string? ActualTenantId { get; }

    public TenantSessionContextMismatchException(string expectedTenantId, string? actualTenantId, string message)
        : base(message)
    {
        ExpectedTenantId = expectedTenantId;
        ActualTenantId = actualTenantId;
    }
}
