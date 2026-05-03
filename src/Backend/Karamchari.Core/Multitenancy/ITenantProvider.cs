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
/// precondition — silent fallbacks are how cross-tenant leaks are born.
/// </summary>
public interface ITenantProvider
{
    /// <summary>
    /// Returns the current request's tenant context.
    /// </summary>
    /// <exception cref="TenantResolutionException">
    /// Thrown when no tenant can be resolved, or when multiple sources disagree.
    /// </exception>
    TenantContext GetTenant();

    /// <summary>
    /// Indicates whether <see cref="GetTenant"/> would succeed without throwing.
    /// Use sparingly — only for diagnostics, telemetry tagging, and the few
    /// edges (health checks, anonymous metadata endpoints) that legitimately
    /// run without a tenant. Business logic must call <see cref="GetTenant"/>.
    /// </summary>
    bool TryGetTenant(out TenantContext? tenant);
}
