using System.Diagnostics.CodeAnalysis;

namespace Karamchari.Core.Multitenancy;

/// <summary>
/// Request-scoped accessor for the active tenant.
///
/// Implementations resolve the tenant from the validated JWT, optionally
/// cross-checking subdomain and trusted gateway headers, and throw
/// <see cref="TenantResolutionException"/> if any source is missing,
/// malformed, or in disagreement with the JWT.
///
/// There is intentionally **no** "TryGet" or "default tenant" surface: every
/// piece of code below this layer treats tenant resolution as a hard
/// precondition â€” silent fallbacks are how cross-tenant leaks are born.
/// </summary>
public interface ITenantProvider
{
    /// <summary>
    /// Returns the active tenant identifier. Business logic should use this
    /// rather than interacting with the full execution envelope.
    /// </summary>
    string GetCurrentTenantId();

    /// <summary>
    /// Indicates whether <see cref="GetCurrentTenantId"/> would succeed without throwing.
    /// </summary>
    bool TryGetCurrentTenantId([NotNullWhen(true)] out string? tenantId);

    /// <summary>
    /// Returns the current request's tenant execution envelope.
    /// </summary>
    /// <exception cref="TenantResolutionException">
    /// Thrown when no tenant can be resolved, or when multiple sources disagree.
    /// </exception>
    TenantExecutionEnvelope GetTenant();

    /// <summary>
    /// Indicates whether <see cref="GetTenant"/> would succeed without throwing.
    /// Use sparingly â€” only for diagnostics, telemetry tagging, and the few
    /// edges (health checks, anonymous metadata endpoints) that legitimately
    /// run without a tenant. Business logic must call <see cref="GetTenant"/>.
    /// </summary>
    bool TryGetTenant(out TenantExecutionEnvelope? tenant);
}
