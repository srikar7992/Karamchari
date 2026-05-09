using Karamchari.Compensation.Domain;
using Karamchari.Compensation.Persistence.Configurations;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Compensation.Persistence;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class CompensationDbContext : KaramchariDbContext
{
    private const string MessagingSchema = "dbo";

    public CompensationDbContext(DbContextOptions<CompensationDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<CompensationBand> CompensationBands => Set<CompensationBand>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<MeritMatrix> MeritMatrices => Set<MeritMatrix>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<IncrementBudgetPool> IncrementBudgetPools => Set<IncrementBudgetPool>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<EmployeeCompensationRecord> EmployeeCompensationRecords => Set<EmployeeCompensationRecord>();

    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new CompensationBandConfiguration());
        modelBuilder.ApplyConfiguration(new MeritMatrixConfiguration());
        modelBuilder.ApplyConfiguration(new IncrementBudgetPoolConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeCompensationRecordConfiguration());

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        modelBuilder.Entity<InboxState>(b => b.ToTable("InboxState", MessagingSchema));
        modelBuilder.Entity<OutboxMessage>(b => b.ToTable("OutboxMessage", MessagingSchema));
        modelBuilder.Entity<OutboxState>(b => b.ToTable("OutboxState", MessagingSchema));
    }
}
