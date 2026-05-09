using System.Text.RegularExpressions;

namespace Karamchari.Core.Multitenancy;

/// <summary>
/// Immutable snapshot of the resolved tenant for the current request scope.
/// Construction validates the tenant id format Ã¢â‚¬â€ once you hold a
/// <see cref="TenantContext"/>, downstream code can trust it.
/// </summary>
public sealed partial record TenantContext
{
    /// <summary>
    /// Allowed tenant id format. Lowercase alphanumeric plus hyphen and underscore, 1-50 chars.
    /// Safe to use as a SQL schema suffix because <see cref="SchemaName"/> only ever
    /// concatenates this against the fixed <see cref="SchemaPrefix"/>; no other character
    /// classes are accepted, so the schema name itself stays inside <c>[A-Za-z0-9_]</c>.
    /// </summary>
    /// <summary>Schema prefix used in storage. Combined with <see cref="TenantId"/> -> <c>tenant_acme</c>.</summary>
    public const string SchemaPrefix = "tenant_";

    [GeneratedRegex(TenantConstants.TenantIdPattern, RegexOptions.CultureInvariant | RegexOptions.Singleline)]
    private static partial Regex TenantIdRegex();

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantContext"/> class.
    /// </summary>
    /// <param name="tenantId">The validated tenant identifier.</param>
    /// <param name="source">The source of the tenant resolution.</param>
    public TenantContext(string tenantId, TenantSource source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        if (!TenantIdRegex().IsMatch(tenantId))
        {
            throw new ArgumentException(
                $"Tenant id '{tenantId}' is invalid. Must match {TenantConstants.TenantIdPattern}.",
                nameof(tenantId));
        }

        TenantId = tenantId;
        Source = source;

        // SchemaName is derived deterministically Ã¢â‚¬â€ never accept it from user input.
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
