using System.Data;
using System.Data.Common;
using Karamchari.Core.Multitenancy;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Persistence.Interceptors;

/// <summary>
/// Sets <c>SESSION_CONTEXT(N'TenantId', @id)</c> on every connection open.
/// SQL Server Row-Level Security policies on tenant tables read this value as
/// the failsafe — if the schema-rewriting interceptor were ever bypassed (raw
/// ADO.NET, an unguarded migration script, …), RLS still blocks cross-tenant
/// reads and writes.
///
/// Behaviour:
/// <list type="bullet">
///   <item>If a tenant is resolvable, sets the session context with read-only=1
///         so transient code can't tamper with it on the same connection.</item>
///   <item>If no tenant is resolvable (background work, admin endpoints) the
///         interceptor leaves the connection alone. RLS policies should still
///         deny the read in that case.</item>
/// </list>
/// </summary>
public sealed partial class RlsSessionContextInterceptor : DbConnectionInterceptor
{
    private const string SessionContextKey = "TenantId";

    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<RlsSessionContextInterceptor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RlsSessionContextInterceptor"/> class.
    /// </summary>
    /// <param name="tenantProvider">The tenant provider.</param>
    /// <param name="logger">The logger.</param>
    public RlsSessionContextInterceptor(
        ITenantProvider tenantProvider,
        ILogger<RlsSessionContextInterceptor> logger)
    {
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Sets the RLS session context when a connection is opened.
    /// </summary>
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        SetSessionContext(connection);
        base.ConnectionOpened(connection, eventData);
    }

    /// <summary>
    /// Sets the RLS session context when a connection is opened asynchronously.
    /// </summary>
    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        await SetSessionContextAsync(connection, cancellationToken).ConfigureAwait(false);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken).ConfigureAwait(false);
    }

    private void SetSessionContext(DbConnection connection)
    {
        if (!_tenantProvider.TryGetTenant(out var tenant) || tenant is null || tenant.TenantId == "system")
        {
            return;
        }

        using var cmd = CreateSetSessionContextCommand(connection, tenant.TenantId);
        cmd.ExecuteNonQuery();

        LogRlsSessionContextSet(_logger, tenant.TenantId);
    }

    private async Task SetSessionContextAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (!_tenantProvider.TryGetTenant(out var tenant) || tenant is null || tenant.TenantId == "system")
        {
            return;
        }

        await using var cmd = CreateSetSessionContextCommand(connection, tenant.TenantId);
        await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        LogRlsSessionContextSetAsync(_logger, tenant.TenantId);
    }

    private static DbCommand CreateSetSessionContextCommand(DbConnection connection, string tenantId)
    {
        var cmd = connection.CreateCommand();
        // Set the session context for RLS. We don't use @read_only=1 here to allow
        // connection reuse in scenarios where the context might be re-initialized
        // (e.g. nested scopes or pooling edge cases).
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

        return cmd;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Trace,
        Message = "RLS session context set for {TenantId}.")]
    static partial void LogRlsSessionContextSet(ILogger logger, string tenantId);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Trace,
        Message = "RLS session context set for {TenantId} (async).")]
    static partial void LogRlsSessionContextSetAsync(ILogger logger, string tenantId);
}
