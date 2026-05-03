using System.Text.RegularExpressions;

namespace Karamchari.Core.Persistence.Provisioning;

/// <summary>
/// Describes a tenant-owned table that participates in Row-Level Security.
/// Bounded contexts register their tables via <see cref="ITenantTableRegistry"/>;
/// the registry is consumed by <c>RlsScriptGenerator</c> at tenant provisioning time.
///
/// We don't carry the schema name here — every tenant table lives under the
/// active tenant's schema, derived per-tenant during provisioning.
/// </summary>
public sealed partial record TenantTable
{
    /// <summary>Conservative SQL Server identifier pattern: regular identifiers only, no quoted/bracket escapes needed.</summary>
    public const string TableNamePattern = "^[A-Za-z_][A-Za-z0-9_]{0,127}$";

    [GeneratedRegex(TableNamePattern, RegexOptions.CultureInvariant | RegexOptions.Compiled)]
    private static partial Regex TableNameRegex();

    /// <summary>Public boundary-check used by the RLS script generator before any identifier is embedded in DDL.</summary>
    public static bool TableName_IsValid(string identifier) =>
        !string.IsNullOrWhiteSpace(identifier) && TableNameRegex().IsMatch(identifier);

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

    public string TableName { get; }

    public string TenantIdColumn { get; }
}
