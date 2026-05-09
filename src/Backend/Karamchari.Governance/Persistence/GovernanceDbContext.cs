using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Governance.Domain.Contracts;
using Karamchari.Governance.Domain.Incidents;
using Karamchari.Governance.Domain.Reliability;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Governance.Persistence;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public class GovernanceDbContext : KaramchariDbContext
{
    /// <inheritdoc/>
    public GovernanceDbContext(DbContextOptions<GovernanceDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<ServiceLevelObjective> ServiceLevelObjectives => Set<ServiceLevelObjective>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<OperationalIncident> OperationalIncidents => Set<OperationalIncident>();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DbSet<SchemaDefinition> SchemaDefinitions => Set<SchemaDefinition>();

    /// <inheritdoc/>
    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);

        modelBuilder.Entity<ServiceLevelObjective>(b =>
        {
            b.ToTable("Governance_ServiceLevelObjectives");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.ComponentName }).IsUnique();
            b.Property(x => x.Tier).HasConversion<string>();
            b.Property(x => x.TargetSuccessRate).HasPrecision(5, 2);
            b.Property(x => x.CurrentSuccessRate).HasPrecision(5, 2);
            b.Property(x => x.ErrorBudgetRemainingPercent).HasPrecision(5, 2);
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<OperationalIncident>(b =>
        {
            b.ToTable("Governance_OperationalIncidents");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Status });
            b.Property(x => x.Severity).HasConversion<string>();
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.RowVersion).IsRowVersion();
        });

        modelBuilder.Entity<SchemaDefinition>(b =>
        {
            b.ToTable("Governance_SchemaDefinitions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.ContractName, x.Version }).IsUnique();
            b.Property(x => x.CompatibilityRule).HasConversion<string>();
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.RowVersion).IsRowVersion();
        });
    }
}
