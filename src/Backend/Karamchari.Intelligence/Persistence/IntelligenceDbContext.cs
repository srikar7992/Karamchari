using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Intelligence.Domain.Metrics;
using Karamchari.Intelligence.Domain.Signals;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Intelligence.Persistence;

public class IntelligenceDbContext : KaramchariDbContext
{
    public IntelligenceDbContext(DbContextOptions<IntelligenceDbContext> options, ITenantProvider tenantProvider) 
        : base(options, tenantProvider)
    {
    }

    public DbSet<IntelligenceSignal> IntelligenceSignals => Set<IntelligenceSignal>();
    public DbSet<MetricDefinition> MetricDefinitions => Set<MetricDefinition>();

    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);

        const string MessagingSchema = "dbo";
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        modelBuilder.Entity<InboxState>(b => b.ToTable("InboxState", MessagingSchema));
        modelBuilder.Entity<OutboxMessage>(b => b.ToTable("OutboxMessage", MessagingSchema));
        modelBuilder.Entity<OutboxState>(b => b.ToTable("OutboxState", MessagingSchema));

        modelBuilder.Entity<IntelligenceSignal>(b =>
        {
            b.ToTable("Intelligence_Signals");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.SignalType, x.SubjectId });
            b.Property(x => x.RowVersion).IsRowVersion();
            
            b.OwnsOne(x => x.Confidence, cb =>
            {
                cb.Property(p => p.Level).HasConversion<string>();
                cb.Property(p => p.Score).HasPrecision(5, 2);
            });
        });

        modelBuilder.Entity<MetricDefinition>(b =>
        {
            b.ToTable("Intelligence_MetricDefinitions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Name, x.CurrentVersion }).IsUnique();
            b.Property(x => x.RowVersion).IsRowVersion();

            b.OwnsOne(x => x.Calculation, cb =>
            {
                cb.Property(p => p.Formula).IsRequired();
            });
        });
    }
}
