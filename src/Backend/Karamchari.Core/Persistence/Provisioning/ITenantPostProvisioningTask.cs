namespace Karamchari.Core.Persistence.Provisioning;

using Microsoft.EntityFrameworkCore;

/// <summary>
/// Represents a bounded-context-specific task that runs after a tenant's tables have been
/// physically cloned by <see cref="TenantProvisioningService"/>.
/// </summary>
/// <remarks>
/// <para>
/// The provisioning service uses <c>SELECT * INTO</c> to clone table structures from the
/// <c>dbo</c> template schema, but SQL Server does not copy indexes or constraints in that
/// operation. Each bounded context registers one or more <see cref="ITenantPostProvisioningTask"/>
/// implementations to apply its critical indexes and constraints to the new tenant schema.
/// </para>
/// <para>
/// Implementations must be registered in the bounded context's DI extension method
/// (e.g., <c>AddKaramchariPayroll</c>) via
/// <c>services.AddSingleton&lt;ITenantPostProvisioningTask, MyTask&gt;()</c>.
/// <see cref="TenantProvisioningService"/> resolves all registered implementations
/// via <c>IEnumerable&lt;ITenantPostProvisioningTask&gt;</c>.
/// </para>
/// <para>
/// Implementations must be <b>idempotent</b>: the task may be called more than once for
/// the same schema (e.g., on a retry after a partial failure). Use <c>IF NOT EXISTS</c>
/// guards in the SQL.
/// </para>
/// </remarks>
public interface ITenantPostProvisioningTask
{
    /// <summary>
    /// Executes the post-provisioning task for the specified tenant schema.
    /// </summary>
    /// <param name="schemaName">The tenant schema name (e.g., <c>tenant_acme</c>).</param>
    /// <param name="dbContext">The database context to use for DDL execution.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task ExecuteAsync(string schemaName, DbContext dbContext, CancellationToken cancellationToken = default);
}
