namespace Karamchari.Core.Multitenancy;

/// <summary>
/// Marker for an entity whose rows belong to a tenant and must therefore
/// carry a <c>TenantId</c> column. The presence of this column is what lets
/// SQL Server Row-Level Security policies enforce isolation at the database
/// layer â€” the predicate compares the row's <see cref="TenantId"/> against
/// <c>SESSION_CONTEXT(N'TenantId')</c>.
///
/// Schema isolation already places these rows under the tenant's own schema;
/// <see cref="ITenantOwned"/> + RLS is defence in depth so a single bug
/// (a missed schema rewrite, a hand-written raw query) cannot leak across
/// tenants.
///
/// Application code should never set <see cref="TenantId"/> directly. The
/// <c>TenantStampingInterceptor</c> populates it on insert from the active
/// <see cref="ITenantProvider"/>; the property setter is expected to be
/// non-public.
/// </summary>
public interface ITenantOwned
{
    /// <summary>The owning tenant id. Stamped automatically on insert.</summary>
    string TenantId { get; }
}
