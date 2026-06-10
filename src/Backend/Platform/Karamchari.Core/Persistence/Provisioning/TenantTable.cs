// -----------------------------------------------------------------------
// <copyright file="TenantTable.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Karamchari.Core.Persistence.Provisioning;

/// <summary>
/// Describes a tenant-owned table that participates in Row-Level Security.
/// Artifacts are discovered via <see cref="ITenantModelDiscoveryService"/>;
/// the discovery result is consumed by <c>RlsScriptGenerator</c> at tenant provisioning time.
///
/// We don't carry the schema name here Ã¢â‚¬â€ every tenant table lives under the
/// active tenant's schema, derived per-tenant during provisioning.
/// </summary>
public sealed partial record TenantTable
{
    /// <summary>Conservative SQL Server identifier pattern: regular identifiers only, no quoted/bracket escapes needed.</summary>
    public const string TableNamePattern = "^[A-Za-z_][A-Za-z0-9_]{0,127}$";

    [GeneratedRegex(TableNamePattern, RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex TableNameRegex();

    /// <summary>Public boundary-check used by the RLS script generator before any identifier is embedded in DDL.</summary>
    public static bool TableNameIsValid(string identifier) =>
        !string.IsNullOrWhiteSpace(identifier) && TableNameRegex().IsMatch(identifier);

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantTable"/> class.
    /// </summary>
    /// <param name="tableName">The name of the table.</param>
    /// <param name="tenantIdColumn">The name of the tenant ID column.</param>
    public TenantTable(string tableName, string tenantIdColumn = "TenantId")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tableName);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantIdColumn);

        if (!TableNameRegex().IsMatch(tableName))
        {
            throw new ArgumentException(
                $"Table name '{tableName}' is not a valid SQL Server regular identifier.",
                nameof(tableName));
        }

        if (!TableNameRegex().IsMatch(tenantIdColumn))
        {
            throw new ArgumentException(
                $"Column name '{tenantIdColumn}' is not a valid SQL Server regular identifier.",
                nameof(tenantIdColumn));
        }

        TableName = tableName;
        TenantIdColumn = tenantIdColumn;
    }

    /// <summary>
    /// Gets the SQL Server table name participating in tenant row-level security.
    /// </summary>
    public string TableName { get; }

    /// <summary>
    /// Gets the tenant discriminator column used by row-level security predicates.
    /// </summary>
    public string TenantIdColumn { get; }
}
