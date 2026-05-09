using Karamchari.Core.Domain.Idempotency;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence.Interceptors;
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

    /// <summary>Gets the centralized audit logs.</summary>
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

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

        modelBuilder.Entity<AuditLog>(builder =>
        {
            builder.ToTable("AuditLogs", "dbo");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.TableName).IsRequired().HasMaxLength(128);
            builder.Property(x => x.Action).IsRequired().HasMaxLength(64);
            builder.HasIndex(x => x.TenantId);
            builder.HasIndex(x => x.TimestampUtc);
        });
    }
}
