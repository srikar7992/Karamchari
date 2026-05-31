using System.Data.Common;
using System.Globalization;
using System.Text.RegularExpressions;
using Karamchari.Core.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Persistence.Filters;

public sealed class RawSqlTenantGuard
{
    private static readonly Regex SqlInjectionPattern = new(
        @"(?i)\b(\bunion\b.*\bselect\b|\bdrop\b|\btruncate\b|\bdelete\b|\bexec\b|\bexecute\b|\bsp_executesql\b|\bxp_cmdshell\b|--|\bunion\b\s+all\b)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex CrossTenantPattern = new(
        @"(?i)\bfrom\s+\w+\.employees\b|\bwhere\s+tenantid\s*=\s*'[^*]'\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<RawSqlTenantGuard> _logger;

    public RawSqlTenantGuard(
        ITenantProvider tenantProvider,
        ILogger<RawSqlTenantGuard> logger)
    {
        _tenantProvider = tenantProvider ?? throw new ArgumentNullException(nameof(tenantProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void GuardFromSqlRaw(string sql, DbContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        ArgumentNullException.ThrowIfNull(context);

        EnsureTenantContext();
        ValidateSql(sql);
        LogQueryExecution("FromSqlRaw", sql);
    }

    public async Task GuardFromSqlRawAsync(string sql, DbContext context, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        ArgumentNullException.ThrowIfNull(context);

        await Task.Run(() => GuardFromSqlRaw(sql, context), cancellationToken);
    }

    public void GuardExecuteSqlRaw(string sql, DbContext context)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        ArgumentNullException.ThrowIfNull(context);

        EnsureTenantContext();
        ValidateSql(sql);
        LogQueryExecution("ExecuteSqlRaw", sql);
    }

    public async Task GuardExecuteSqlRawAsync(string sql, DbContext context, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);
        ArgumentNullException.ThrowIfNull(context);

        await Task.Run(() => GuardExecuteSqlRaw(sql, context), cancellationToken);
    }

    public string SafeFromSql(string sql, params object[] parameters)
    {
        GuardParameters(sql, parameters);
        return sql;
    }

    public string SafeFromSqlInterpolated(FormattableString formattable)
    {
        ArgumentNullException.ThrowIfNull(formattable);

        var sql = formattable.Format;
        GuardParameters(sql, formattable.GetArguments()?.Cast<object>().ToArray() ?? Array.Empty<object>());
        return sql;
    }

    private void EnsureTenantContext()
    {
        if (!_tenantProvider.TryGetTenant(out var tenant) || tenant is null)
        {
            _logger.LogCritical(
                "CRITICAL: Raw SQL attempted without tenant context. Stack: {StackTrace}",
                Environment.StackTrace);
            throw new UnsafeQueryAccessException(
                "Raw SQL execution requires an active tenant context.");
        }

        _logger.LogDebug(
            "Raw SQL execution authorized for TenantId={TenantId}",
            tenant.TenantId);
    }

    private void ValidateSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new UnsafeQueryAccessException("SQL query cannot be empty.");
        }

        if (SqlInjectionPattern.IsMatch(sql))
        {
            _logger.LogCritical(
                "CRITICAL: SQL injection pattern detected in raw SQL. Query blocked. Stack: {StackTrace}",
                Environment.StackTrace);
            throw new UnsafeQueryAccessException(
                "SQL query contains potentially dangerous patterns and has been blocked.");
        }

        if (CrossTenantPattern.IsMatch(sql))
        {
            _logger.LogCritical(
                "CRITICAL: Cross-tenant SQL pattern detected. Query blocked. Tenant={TenantId}",
                _tenantProvider.TryGetTenant(out var t) ? t?.TenantId : "Unknown");
            throw new UnsafeQueryAccessException(
                "Cross-tenant SQL queries are not allowed.");
        }
    }

    private void GuardParameters(string sql, object[] parameters)
    {
        if (parameters is null || parameters.Length == 0)
        {
            return;
        }

        foreach (var param in parameters)
        {
            if (param is string str && SqlInjectionPattern.IsMatch(str))
            {
                _logger.LogCritical(
                    "CRITICAL: SQL injection pattern detected in parameter. Query blocked.");
                throw new UnsafeQueryAccessException(
                    "SQL parameter contains potentially dangerous patterns and has been blocked.");
            }
        }
    }

    private void LogQueryExecution(string method, string sql)
    {
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "Raw SQL execution via {Method} for TenantId={TenantId}: {Query}",
                method,
                _tenantProvider.TryGetTenant(out var t) ? t?.TenantId : "Unknown",
                sql.Length > 200 ? sql[..200] + "..." : sql);
        }
    }
}
