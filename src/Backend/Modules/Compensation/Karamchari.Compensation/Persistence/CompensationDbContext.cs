using Karamchari.Compensation.Domain;
using Karamchari.Compensation.Persistence.Configurations;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
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
    }
}
