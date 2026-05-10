using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Karamchari.Core.Persistence.Tenant;

public sealed class TenantSqlConnectionScope : IDisposable
{
    private readonly DbConnection _connection;
    private readonly RlsConnectionGuard _guard;
    private readonly ILogger<TenantSqlConnectionScope> _logger;
    private DbTransaction? _transaction;
    private bool _disposed;
    private bool _transactionRolledBack;

    private TenantSqlConnectionScope(DbConnection connection, string tenantId, ILogger<TenantSqlConnectionScope> logger)
    {
        _connection = connection;
        _guard = RlsConnectionGuard.Acquire(connection, tenantId);
        _logger = logger;
    }

    public static TenantSqlConnectionScope Acquire(DbConnection connection, string tenantId)
    {
        var logger = NullLogger<TenantSqlConnectionScope>.Instance;
        return new TenantSqlConnectionScope(connection, tenantId, logger);
    }

    public static TenantSqlConnectionScope Acquire(DbConnection connection, string tenantId, ILogger<TenantSqlConnectionScope> logger)
    {
        return new TenantSqlConnectionScope(connection, tenantId, logger);
    }

    public DbConnection Connection => _connection;

    public void BeginTransaction()
    {
        ThrowIfDisposed();

        if (_transaction != null)
        {
            throw new InvalidOperationException("Transaction already in progress.");
        }

        _transaction = _connection.BeginTransaction();
        _transactionRolledBack = false;

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Transaction started for tenant scope.");
        }
    }

    public void BeginTransaction(IsolationLevel isolationLevel)
    {
        ThrowIfDisposed();

        if (_transaction != null)
        {
            throw new InvalidOperationException("Transaction already in progress.");
        }

        _transaction = _connection.BeginTransaction(isolationLevel);
        _transactionRolledBack = false;

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "Transaction started with isolation level {IsolationLevel} for tenant scope.",
                isolationLevel);
        }
    }

    public DbTransaction? GetTransaction() => _transaction;

    public void Commit()
    {
        ThrowIfDisposed();

        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction in progress.");
        }

        _transaction.Commit();
        _transaction.Dispose();
        _transaction = null;
        _transactionRolledBack = false;

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Transaction committed for tenant scope.");
        }
    }

    public void Rollback()
    {
        ThrowIfDisposed();

        if (_transaction == null)
        {
            return;
        }

        _transaction.Rollback();
        _transactionRolledBack = true;
        _transaction.Dispose();
        _transaction = null;

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Transaction rolled back for tenant scope.");
        }
    }

    public void ValidateTenantContext(string expectedTenantId)
    {
        ThrowIfDisposed();
        _guard.ValidateSessionContext(expectedTenantId);
    }

    public void ResetForRetry()
    {
        ThrowIfDisposed();

        _guard.ClearSessionContext();

        if (_transaction != null && !_transactionRolledBack)
        {
            _transaction.Rollback();
            _transactionRolledBack = true;
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Tenant scope reset for retry.");
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TenantSqlConnectionScope));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_transaction != null)
        {
            if (!_transactionRolledBack)
            {
                try
                {
                    _transaction.Rollback();
                }
                catch (Exception ex)
                {
                    if (_logger.IsEnabled(LogLevel.Warning))
                    {
                        _logger.LogWarning(ex, "Failed to rollback transaction during dispose.");
                    }
                }
            }

            _transaction.Dispose();
            _transaction = null;
        }

        _guard.Dispose();
        _disposed = true;

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Tenant SQL connection scope disposed.");
        }
    }
}
