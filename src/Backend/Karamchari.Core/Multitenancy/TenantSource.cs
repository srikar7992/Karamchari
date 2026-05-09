namespace Karamchari.Core.Multitenancy;

/// <summary>
/// Origin of a resolved tenant identifier. Used for diagnostics and cross-source
/// validation: when more than one source carries a tenant id (e.g., JWT + subdomain),
/// they must agree, otherwise the request is rejected.
/// </summary>
public enum TenantSource
{
    /// <summary>Resolved from a validated JWT claim. The only authoritative source for user requests.</summary>
    JwtClaim = 0,

    /// <summary>Resolved from an <c>X-Tenant-Id</c>-style header injected by the API gateway. Trusted only on the gateway-only network path.</summary>
    TrustedHeader = 1,

    /// <summary>Resolved from the request host (subdomain). Routing aid only Ã¢â‚¬â€ never authoritative.</summary>
    HostSubdomain = 2,

    /// <summary>System-resolved during a physical provisioning background task. Trusted.</summary>
    Provisioning = 3,
}
