using Karamchari.Core.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Karamchari.Core.Persistence.Interceptors;

/// <summary>
/// Stamps and validates <see cref="ITenantOwned.TenantId"/> on every save.
///
/// Two enforcement modes, both fail-closed:
/// <list type="bullet">
///   <item><b>Insert:</b> if the entity has a blank TenantId, populate it with
///         the current tenant. If it has one already, it must match the
///         current tenant â€” otherwise the save is rejected as a cross-tenant
///         write attempt.</item>
///   <item><b>Update / Delete:</b> the TenantId on the tracked entity must
///         match the current tenant. Modifying the TenantId is never allowed
///         (the property is detected via EF metadata, so setting it through a
///         backing field is also caught).</item>
/// </list>
///
/// This interceptor is the application-layer twin of the RLS BLOCK predicate:
/// RLS enforces the same invariant at the database layer, so we have two
/// independent gates against cross-tenant writes.
/// </summary>
public sealed class TenantStampingInterceptor : SaveChangesInterceptor
{
    private const string TenantIdPropertyName = nameof(ITenantOwned.TenantId);

    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<TenantStampingInterceptor> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TenantStampingInterceptor"/> class.
    /// </summary>
    /// <param name="tenantProvider">The tenant provider.</param>
    /// <param name="logger">The logger.</param>
    public TenantStampingInterceptor(
        ITenantProvider tenantProvider,
        ILogger<TenantStampingInterceptor> logger)
    {
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    /// <summary>
    /// Stamps and validates tenant IDs before saving changes synchronously.
    /// </summary>
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        if (eventData.Context is { } ctx)
        {
            StampAndValidate(ctx);
        }

        return base.SavingChanges(eventData, result);
    }

    /// <summary>
    /// Stamps and validates tenant IDs before saving changes asynchronously.
    /// </summary>
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        if (eventData.Context is { } ctx)
        {
            StampAndValidate(ctx);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void StampAndValidate(DbContext context)
    {
        // We need a tenant. If none is resolvable, refuse the save â€” any tenant-owned
        // write without a tenant context is a bug, period.
        var tenant = _tenantProvider.GetTenant();
        var expectedTenantId = tenant.TenantId;

        foreach (var entry in context.ChangeTracker.Entries<ITenantOwned>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    StampForInsert(entry, expectedTenantId);
                    break;

                case EntityState.Modified:
                    GuardModification(entry, expectedTenantId);
                    break;

                case EntityState.Deleted:
                    GuardDelete(entry, expectedTenantId);
                    break;
            }
        }
    }

    private void StampForInsert(EntityEntry<ITenantOwned> entry, string expectedTenantId)
    {
        var prop = entry.Property(TenantIdPropertyName);
        var current = prop.CurrentValue as string;

        if (string.IsNullOrWhiteSpace(current))
        {
            prop.CurrentValue = expectedTenantId;
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace(
                    "Stamped TenantId={TenantId} on inserted {EntityType}.",
                    expectedTenantId,
                    entry.Entity.GetType().Name);
            }
            return;
        }

        if (!string.Equals(current, expectedTenantId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Refusing to insert {entry.Entity.GetType().Name}: TenantId '{current}' does not match the active tenant '{expectedTenantId}'.");
        }
    }

    private static void GuardModification(EntityEntry<ITenantOwned> entry, string expectedTenantId)
    {
        var prop = entry.Property(TenantIdPropertyName);

        // Detect attempts to change TenantId on an existing row.
        if (prop.IsModified)
        {
            throw new InvalidOperationException(
                $"Refusing to update {entry.Entity.GetType().Name}: TenantId is immutable.");
        }

        // Even if the property wasn't marked modified, make sure the row we're
        // about to touch belongs to the current tenant. RLS would catch a
        // mismatch too, but failing here yields a clearer error.
        var current = prop.CurrentValue as string;
        if (!string.Equals(current, expectedTenantId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Refusing to update {entry.Entity.GetType().Name}: row TenantId '{current}' does not match the active tenant '{expectedTenantId}'.");
        }
    }

    private static void GuardDelete(EntityEntry<ITenantOwned> entry, string expectedTenantId)
    {
        var current = entry.Property(TenantIdPropertyName).CurrentValue as string;
        if (!string.Equals(current, expectedTenantId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Refusing to delete {entry.Entity.GetType().Name}: row TenantId '{current}' does not match the active tenant '{expectedTenantId}'.");
        }
    }
}
