// -----------------------------------------------------------------------
// <copyright file="PlatformIntelligenceDbContext.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.PlatformIntelligence.Domain.Decisions;
using Karamchari.PlatformIntelligence.Domain.Insights;
using Karamchari.PlatformIntelligence.Domain.Optimization;
using Karamchari.PlatformIntelligence.Domain.Recommendations;
using Karamchari.PlatformIntelligence.Domain.Risk;
using Karamchari.PlatformIntelligence.Domain.Scenarios;
using Karamchari.PlatformIntelligence.Domain.Simulation;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.PlatformIntelligence.Persistence;

public class PlatformIntelligenceDbContext : KaramchariDbContext
{
    public PlatformIntelligenceDbContext(
        DbContextOptions<PlatformIntelligenceDbContext> options,
        ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }

    public DbSet<PlatformDecision> PlatformDecisions => Set<PlatformDecision>();
    public DbSet<WorkforceScenario> WorkforceScenarios => Set<WorkforceScenario>();
    public DbSet<SimulationRun> SimulationRuns => Set<SimulationRun>();
    public DbSet<WorkforceOptimization> WorkforceOptimizations => Set<WorkforceOptimization>();
    public DbSet<ExecutiveDigest> ExecutiveDigests => Set<ExecutiveDigest>();
    public DbSet<WorkforceRisk> WorkforceRisks => Set<WorkforceRisk>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();

    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);

        modelBuilder.Entity<PlatformDecision>(b =>
        {
            b.ToTable("Platform_Decisions");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.ScopeKey, x.GeneratedAt });
            b.Property(x => x.ScopeKey).HasMaxLength(200);
            b.Property(x => x.ConfidenceScore).HasPrecision(5, 1);
            b.Property(x => x.Priority).HasConversion<string>();
            b.Property(x => x.Title).HasMaxLength(500);
        });

        modelBuilder.Entity<WorkforceScenario>(b =>
        {
            b.ToTable("Platform_Scenarios");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.IsOptimal });
            b.HasIndex(x => new { x.TenantId, x.Status });
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Name).HasMaxLength(200);
            b.Property(x => x.CoverageScore).HasPrecision(5, 1);
            b.Property(x => x.OvertimeRate).HasPrecision(5, 2);
            b.Property(x => x.EstimatedCost).HasPrecision(18, 2);
        });

        modelBuilder.Entity<SimulationRun>(b =>
        {
            b.ToTable("Platform_SimulationRuns");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.CreatedAt });
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Name).HasMaxLength(200);
        });

        modelBuilder.Entity<WorkforceOptimization>(b =>
        {
            b.ToTable("Platform_Optimizations");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.ScopeKey, x.GeneratedAt });
            b.Property(x => x.ScopeKey).HasMaxLength(200);
            b.Property(x => x.ObjectiveType).HasMaxLength(100);
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.EstimatedAnnualSavings).HasPrecision(18, 2);
        });

        modelBuilder.Entity<ExecutiveDigest>(b =>
        {
            b.ToTable("Platform_ExecutiveDigests");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Period, x.PeriodDate });
            b.Property(x => x.Period).HasConversion<string>();
        });

        modelBuilder.Entity<WorkforceRisk>(b =>
        {
            b.ToTable("Platform_WorkforceRisks");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Category }).IsUnique();
            b.Property(x => x.Category).HasConversion<string>();
            b.Property(x => x.Level).HasConversion<string>();
            b.Property(x => x.RiskScore).HasPrecision(5, 1);
        });

        modelBuilder.Entity<Recommendation>(b =>
        {
            b.ToTable("Platform_Recommendations");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.TenantId, x.Status, x.GeneratedAt });
            b.HasIndex(x => x.LinkedRiskId);
            b.Property(x => x.Status).HasConversion<string>();
            b.Property(x => x.Category).HasMaxLength(100);
            b.Property(x => x.Title).HasMaxLength(500);
            b.Property(x => x.ConfidenceScore).HasPrecision(5, 1);
            b.Property(x => x.EstimatedSavings).HasPrecision(18, 2);
            b.Property(x => x.RiskReductionPercent).HasPrecision(5, 1);
        });
    }
}
