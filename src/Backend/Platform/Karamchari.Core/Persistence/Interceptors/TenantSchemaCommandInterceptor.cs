// -----------------------------------------------------------------------
// <copyright file="TenantSchemaCommandInterceptor.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Data.Common;
using System.Text.RegularExpressions;
using Karamchari.Core.Multitenancy;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Persistence.Interceptors;

/// <summary>
/// Rewrites the placeholder schema baked into every command (<see cref="KaramchariDbContext.PlaceholderSchema"/>)
/// to the active tenant's schema, immediately before SQL Server executes it.
///
/// Why this and not <c>IModelCacheKeyFactory</c>? Including the tenant id in the
/// model cache key produces one compiled EF model per tenant Ã¢â‚¬â€ at scale that
/// blows up memory and warmup time. We keep a single compiled model and shift
/// tenant resolution to command execution.
///
/// Invariants this interceptor enforces (failing fast on any breach):
/// <list type="bullet">
///   <item>An active tenant <b>must</b> be resolvable. No default fallback.</item>
///   <item>The tenant schema name must match the validated format defined on <see cref="TenantExecutionEnvelope"/>.</item>
///   <item>The placeholder must be unambiguous in the SQL Ã¢â‚¬â€ see <see cref="PlaceholderRegex"/>.</item>
/// </list>
/// </summary>
public sealed partial class TenantSchemaCommandInterceptor : DbCommandInterceptor
{
    /// <summary>
    /// Matches the placeholder schema in either bracketed or bare form, in any
    /// position where EF Core emits it: as <c>[__tenant__].[Foo]</c>,
    /// <c>__tenant__.Foo</c>, or qualified function calls. Word-boundary
    /// anchors keep us from matching a substring of an unrelated identifier.
    /// </summary>
    [GeneratedRegex(
        @"(?<lb>\[)__tenant__(?<rb>\])|(?<![\w])__tenant__(?![\w])",
        RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex PlaceholderRegex();

    /// <summary>Validates that the substituted schema is itself safe to embed in SQL Ã¢â‚¬â€ defence-in-depth, since the value already came from a validated <see cref="TenantExecutionEnvelope"/>.</summary>
    [GeneratedRegex(@"^tenant_[a-z0-9_]{1,64}$", RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex SchemaNameRegex();

    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<TenantSchemaCommandInterceptor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantSchemaCommandInterceptor"/> class.
    /// </summary>
    /// <param name="tenantProvider">The tenant provider.</param>
    /// <param name="logger">The logger.</param>
    public TenantSchemaCommandInterceptor(
        ITenantProvider tenantProvider,
        ILogger<TenantSchemaCommandInterceptor> logger)
    {
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Rewrites the command text before a reader is executed.
    /// </summary>
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Rewrite(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    /// <summary>
    /// Rewrites the command text before a reader is executed asynchronously.
    /// </summary>
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Rewrite(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    /// <summary>
    /// Rewrites the command text before a non-query is executed.
    /// </summary>
    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        Rewrite(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    /// <summary>
    /// Rewrites the command text before a non-query is executed asynchronously.
    /// </summary>
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Rewrite(command);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    /// <summary>
    /// Rewrites the command text before a scalar is executed.
    /// </summary>
    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        Rewrite(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    /// <summary>
    /// Rewrites the command text before a scalar is executed asynchronously.
    /// </summary>
    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        Rewrite(command);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    private void Rewrite(DbCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var original = command.CommandText;
        if (string.IsNullOrEmpty(original))
        {
            return;
        }

        if (!original.Contains(KaramchariDbContext.PlaceholderSchema, StringComparison.Ordinal))
        {
            return;
        }

        var tenant = _tenantProvider.GetTenant();
        _logger.LogInformation("Interceptor: Rewriting SQL for schema {Schema}. Original SQL: {Sql}", tenant.SchemaName, original[..Math.Min(100, original.Length)]);

        if (tenant.SchemaName != "dbo" && !SchemaNameRegex().IsMatch(tenant.SchemaName))
        {
            // TenantExecutionEnvelope already validates this via TenantId format, 
            // but we re-check at the boundary because the value is about to be embedded in raw SQL.
            throw new InvalidOperationException(
                $"Refusing to execute SQL: tenant schema '{tenant.SchemaName}' does not match the safe-identifier pattern.");
        }

        var placeholderRegex = PlaceholderRegex();
        if (!placeholderRegex.IsMatch(original))
        {
            return;
        }

        var rewritten = placeholderRegex.Replace(
            original,
            match => match.Groups["lb"].Success
                ? $"[{tenant.SchemaName}]"
                : tenant.SchemaName);

        _logger.LogInformation("Interceptor: Rewrote SQL to: {Sql}", rewritten[..Math.Min(100, rewritten.Length)]);

        if (ReferenceEquals(rewritten, original))
        {
            // Match-but-no-substitution should never happen given the regex,
            // but if it does we treat it as a bug Ã¢â‚¬â€ better to throw than to
            // silently run the wrong SQL.
            throw new InvalidOperationException(
                "Tenant schema rewrite matched the placeholder but produced no substitution.");
        }

        command.CommandText = rewritten;

        LogTenantSchemaRewritten(
            _logger,
            tenant.TenantId,
            KaramchariDbContext.PlaceholderSchema,
            tenant.SchemaName);
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Trace,
        Message = "Tenant schema rewritten for {TenantId}: {Placeholder} -> {Schema}")]
    static partial void LogTenantSchemaRewritten(ILogger logger, string tenantId, string placeholder, string schema);
}
