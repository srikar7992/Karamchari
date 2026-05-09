using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Intelligence.Domain.Metrics;
using Karamchari.Intelligence.Domain.Signals;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Intelligence.Persistence;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public class IntelligenceDbContext : KaramchariDbContext
{
    /// <inheritdoc/>
    public IntelligenceDbContext(DbContextOptions<IntelligenceDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<IntelligenceSignal> IntelligenceSignals => Set<IntelligenceSignal>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<MetricDefinition> MetricDefinitions => Set<MetricDefinition>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<OrganizationalHealthSignal> OrganizationalHealthSignals => Set<OrganizationalHealthSignal>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<WorkforceRiskSignal> WorkforceRiskSignals => Set<WorkforceRiskSignal>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<ExecutiveInsight> ExecutiveInsights => Set<ExecutiveInsight>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<StrategicWorkforceScenario> StrategicWorkforceScenarios => Set<StrategicWorkforceScenario>();

    /// <inheritdoc/>
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

        modelBuilder.Entity<OrganizationalHealthSignal>(b =>
        {
            b.ToTable("Strategy_OrgHealthSignals");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.OrgUnitId });
            b.Property(x => x.OverallHealthScore).HasPrecision(5, 2);
            b.Property(x => x.RowVersion).IsRowVersion();

            b.OwnsOne(x => x.BurnoutRisk);
            b.OwnsOne(x => x.StaffingStress);
            b.OwnsOne(x => x.Confidence, cb =>
            {
                cb.Property(p => p.Level).HasConversion<string>();
                cb.Property(p => p.Score).HasPrecision(5, 2);
            });
            b.OwnsOne(x => x.Explanation, cb => cb.ToJson());
        });

        modelBuilder.Entity<WorkforceRiskSignal>(b =>
        {
            b.ToTable("Strategy_WorkforceRiskSignals");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Category, x.SubjectId });
            b.Property(x => x.Category).HasConversion<string>();
            b.Property(x => x.RowVersion).IsRowVersion();

            b.OwnsOne(x => x.Severity);
            b.OwnsOne(x => x.Confidence, cb =>
            {
                cb.Property(p => p.Level).HasConversion<string>();
                cb.Property(p => p.Score).HasPrecision(5, 2);
            });
            b.OwnsOne(x => x.Explanation, cb => cb.ToJson());
        });

        modelBuilder.Entity<ExecutiveInsight>(b =>
        {
            b.ToTable("Strategy_ExecutiveInsights");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Category });
            b.Property(x => x.ContributingSignalIds).HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(Guid.Parse).ToList()
            );

            b.OwnsOne(x => x.AggregateConfidence, cb =>
            {
                cb.Property(p => p.Level).HasConversion<string>();
                cb.Property(p => p.Score).HasPrecision(5, 2);
            });
        });

        modelBuilder.Entity<StrategicWorkforceScenario>(b =>
        {
            b.ToTable("Strategy_StrategicScenarios");
            b.HasKey(x => x.Id);
            b.Property(x => x.RowVersion).IsRowVersion();
        });
    }
}
