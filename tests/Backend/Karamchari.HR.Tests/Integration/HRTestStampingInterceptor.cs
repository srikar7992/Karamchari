using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Karamchari.HR.Tests.Integration;

/// <summary>
/// SaveChanges interceptor that stamps TenantId onto every added ITenantOwned entity
/// when running under in-memory database (where no schema-level RLS interceptor runs).
/// </summary>
internal sealed class HRTestStampingInterceptor(ITenantProvider tenantProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context is null) return;

        var tenantId = tenantProvider.GetCurrentTenantId();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added && entry.Entity is ITenantOwned)
            {
                entry.Property(nameof(ITenantOwned.TenantId)).CurrentValue = tenantId;
            }
        }
    }
}
