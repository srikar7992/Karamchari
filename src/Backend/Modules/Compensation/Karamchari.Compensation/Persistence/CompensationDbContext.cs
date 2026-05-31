using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Compensation.Domain;
using Karamchari.Compensation.Persistence.Configurations;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Compensation.Persistence;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class CompensationDbContext : KaramchariDbContext
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public CompensationDbContext(DbContextOptions<CompensationDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<CompensationBand> Bands => Set<CompensationBand>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<MeritMatrix> MeritMatrices => Set<MeritMatrix>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<IncrementBudgetPool> BudgetPools => Set<IncrementBudgetPool>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<EmployeeCompensationRecord> CompensationRecords => Set<EmployeeCompensationRecord>();

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new CompensationBandConfiguration());
        modelBuilder.ApplyConfiguration(new MeritMatrixConfiguration());
        modelBuilder.ApplyConfiguration(new IncrementBudgetPoolConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeCompensationRecordConfiguration());

        const string MessagingSchema = "dbo";

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<InboxState>(b => b.ToTable("InboxState", MessagingSchema, t => t.ExcludeFromMigrations()));
        modelBuilder.Entity<OutboxMessage>(b => b.ToTable("OutboxMessage", MessagingSchema, t => t.ExcludeFromMigrations()));
        modelBuilder.Entity<OutboxState>(b => b.ToTable("OutboxState", MessagingSchema, t => t.ExcludeFromMigrations()));
    }
}
