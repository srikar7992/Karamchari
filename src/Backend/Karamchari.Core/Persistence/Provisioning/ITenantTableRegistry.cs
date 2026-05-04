namespace Karamchari.Core.Persistence.Provisioning;

/// <summary>
/// Singleton registry of every tenant-owned table that requires RLS.
///
/// Bounded-context startup populates the registry through its
/// <c>AddKaramchari{Context}</c> extension. The provisioning service reads it
/// to generate per-tenant security policies via <c>RlsScriptGenerator</c>.
///
/// The registry is the single source of truth: missing a table here means
/// missing RLS for it. Treat additions to this registry like security commits.
/// </summary>
public interface ITenantTableRegistry
{
    /// <summary>All tables that should have an RLS policy applied.</summary>
    IReadOnlyCollection<TenantTable> Tables { get; }

    /// <summary>Adds a table to the registry. Throws if the table is already registered.</summary>
    void Register(TenantTable table);
}
