using Karamchari.Core.Domain.Idempotency;
using Karamchari.Core.Multitenancy;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Core.Persistence;

/// <summary>
/// DbContext for shared infrastructure and core capabilities.
/// </summary>
public sealed class CoreDbContext : KaramchariDbContext
{
    /// <summary>Initializes a new instance of the <see cref="CoreDbContext"/> class.</summary>
    public CoreDbContext(DbContextOptions<CoreDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }

    /// <summary>Gets the idempotent requests set.</summary>
    public DbSet<IdempotentRequest> IdempotentRequests => Set<IdempotentRequest>();

    /// <summary>Configures the core domain model.</summary>
    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<IdempotentRequest>(builder =>
        {
            builder.ToTable("IdempotentRequests", "dbo"); // Shared, not per-tenant schema
            builder.HasKey(x => new { x.IdempotencyKey, x.TenantId });
            builder.Property(x => x.ResponseBody).IsRequired();
            builder.HasIndex(x => x.ExpiresAtUtc);
        });
    }
}
