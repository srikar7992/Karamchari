using System.ComponentModel.DataAnnotations;

namespace Karamchari.Core.Multitenancy;

/// <summary>
/// Configuration for the request-scoped tenant resolver. Bound from the
/// <c>"Tenancy"</c> configuration section; populated from environment
/// variables / Key Vault in production via <c>IConfiguration</c>.
///
/// Note: there is no connection-string-per-tenant setting on purpose. The
/// shared-DB / separated-schema model uses one connection string per database
/// (or elastic pool); the schema is the tenant boundary.
/// </summary>
public sealed class TenantOptions
{
    /// <summary>
    /// TODO: Add XML documentation.
    /// </summary>
    public const string SectionName = "Tenancy";

    /// <summary>JWT claim type that carries the tenant id. Defaults to <c>"tenant_id"</c>.</summary>
    [Required]
    public string JwtClaimType { get; init; } = "tenant_id";

    /// <summary>Header name used by APIM to inject tenant id on service-to-service calls. Trusted only when <see cref="TrustedGatewayHeader"/> is also satisfied.</summary>
    public string TenantHeaderName { get; init; } = "X-Tenant-Id";

    /// <summary>
    /// Header that the API gateway sets to prove the request traversed it
    /// (e.g. <c>X-Karamchari-Gateway: &lt;shared-secret-hash&gt;</c>). Only when
    /// this header is present and matches the configured fingerprint will the
    /// tenant resolver consider <see cref="TenantHeaderName"/> as a trusted source.
    /// </summary>
    public string TrustedGatewayHeader { get; init; } = "X-Karamchari-Gateway";

    /// <summary>
    /// Fingerprint that the gateway header must contain. Stored in Key Vault;
    /// rotated on a schedule. Empty in non-production environments.
    /// </summary>
    public string TrustedGatewayFingerprint { get; init; } = string.Empty;

    /// <summary>
    /// When true, the resolver expects requests to arrive on a tenant subdomain
    /// (e.g. <c>acme.karamchari.com</c>) and validates the subdomain agrees with
    /// the JWT claim. Set false for environments fronted by a single host.
    /// </summary>
    public bool RequireSubdomainAgreement { get; init; } = true;

    /// <summary>
    /// Comma-separated list of host suffixes considered "the app" â€” e.g.
    /// <c>karamchari.com,karamchari.dev</c>. Used to peel the tenant subdomain
    /// off the request host. Required when <see cref="RequireSubdomainAgreement"/> is true.
    /// </summary>
    public string AppHostSuffixes { get; init; } = "karamchari.com";
}
