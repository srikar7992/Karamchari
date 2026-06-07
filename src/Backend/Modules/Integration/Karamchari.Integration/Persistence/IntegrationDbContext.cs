using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Integration.Domain;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Integration.Persistence;

public sealed class IntegrationDbContext : KaramchariDbContext
{
    public IntegrationDbContext(DbContextOptions<IntegrationDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider) { }

    public DbSet<WebhookSubscription> WebhookSubscriptions => Set<WebhookSubscription>();

    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);

        modelBuilder.Entity<WebhookSubscription>(e =>
        {
            e.ToTable("WebhookSubscriptions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id)
                .HasConversion(id => id.Value, v => new WebhookSubscriptionId(v));
            e.Property(x => x.TenantId).HasMaxLength(100).IsRequired();
            e.Property(x => x.TargetUrl).HasMaxLength(2000).IsRequired();
            e.Property(x => x.Secret).HasMaxLength(500).IsRequired();
            e.Property(x => x.Description).HasMaxLength(500);
            e.Property(x => x.EventTypeFilter).HasMaxLength(2000);

            // EventTypes stored as pipe-separated string; loaded back at runtime from EventTypeFilter
            e.Ignore(x => x.EventTypes);

            e.OwnsMany(x => x.Deliveries, d =>
            {
                d.ToTable("WebhookDeliveries");
                d.HasKey(x => x.Id);
                d.Property(x => x.SubscriptionId)
                    .HasConversion(id => id.Value, v => new WebhookSubscriptionId(v));
                d.Property(x => x.EventType).HasMaxLength(200).IsRequired();
                d.Property(x => x.PayloadJson).HasColumnType("nvarchar(max)");
                d.Property(x => x.ErrorMessage).HasMaxLength(2000);
                d.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            });
        });
    }
}
