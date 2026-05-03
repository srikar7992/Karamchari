using System.Text.RegularExpressions;

namespace Karamchari.Core.Multitenancy;

/// <summary>
/// Immutable snapshot of the resolved tenant for the current request scope.
/// Construction validates the tenant id format — once you hold a
/// <see cref="TenantContext"/>, downstream code can trust it.
/// </summary>
public sealed partial record TenantContext
{
    /// <summary>Allowed tenant id format. Lowercase, hyphenated, 1-50 chars; safe to use as a SQL schema suffix.</summary>
    public const string TenantIdPattern = "^[a-z0-9][a-z0-9-]{0,49}$";

    /// <summary>Schema prefix used in storage. Combined with <see cref="TenantId"/> -> <c>tenant_acme</c>.</summary>
    public const string SchemaPrefix = "tenant_";

    [GeneratedRegex(TenantIdPattern, RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex TenantIdRegex();

    public TenantContext(string tenantId, TenantSource source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        if (!TenantIdRegex().IsMatch(tenantId))
        {
            throw new ArgumentException(
                $"Tenant id '{tenantId}' is invalid. Must match {TenantIdPattern}.",
                nameof(tenantId));
        }

        TenantId = tenantId;
        Source = source;

        // SchemaName is derived deterministically — never accept it from user input.
        // The hyphen replacement keeps SQL identifier rules happy without quoting.
        SchemaName = SchemaPrefix + tenantId.Replace('-', '_');
    }

    /// <summary>The validated tenant identifier (e.g. <c>acme</c>).</summary>
    public string TenantId { get; }

    /// <summary>The fully-qualified DB schema name for this tenant (e.g. <c>tenant_acme</c>).</summary>
    public string SchemaName { get; }

    /// <summary>Where this tenant id came from. Used for auditing and source-agreement checks.</summary>
    public TenantSource Source { get; }
}
