// -----------------------------------------------------------------------
// <copyright file="TenantSource.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

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

    /// <summary>Resolved from the request host (subdomain). Routing aid only — never authoritative.</summary>
    HostSubdomain = 2,

    /// <summary>System-resolved during a physical provisioning background task. Trusted.</summary>
    Provisioning = 3,

    /// <summary>Resolved from a message header in a background consumer flow.</summary>
    Messaging = 4,

    /// <summary>Resolved from serialized state in a background job or scheduled task.</summary>
    Background = 5,

    /// <summary>Explicitly provided via administrative override or system maintenance tool.</summary>
    AdminOverride = 6
}
