using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Karamchari.Recruitment.Tests;

public sealed class RecruitmentTestStampingInterceptor(ITenantProvider tenantProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Stamp(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Stamp(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void Stamp(DbContext? context)
    {
        if (context == null) return;

        var tenantId = tenantProvider.GetCurrentTenantId();

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is ITenantOwned tenantOwned)
                {
                    entry.Property("TenantId").CurrentValue = tenantId;
                }

                if (entry.Entity is IAuditable auditable)
                {
                    entry.Property("CreatedBy").CurrentValue = "test-user";
                    entry.Property("CreatedOnUtc").CurrentValue = DateTimeOffset.UtcNow;
                }
            }
        }
    }
}
