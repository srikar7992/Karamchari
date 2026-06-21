// -----------------------------------------------------------------------
// <copyright file="CompensationDbContext.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Compensation.Domain;
using Karamchari.Compensation.Persistence.Configurations;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Compensation.Persistence;

public sealed class CompensationDbContext : KaramchariDbContext
{
    public CompensationDbContext(DbContextOptions<CompensationDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider) { }

    public DbSet<CompensationBand> Bands => Set<CompensationBand>();
    public DbSet<MeritMatrix> MeritMatrices => Set<MeritMatrix>();
    public DbSet<IncrementBudgetPool> BudgetPools => Set<IncrementBudgetPool>();
    public DbSet<EmployeeCompensationRecord> CompensationRecords => Set<EmployeeCompensationRecord>();
    public DbSet<BonusPlan> BonusPlans => Set<BonusPlan>();

    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnDomainModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new BonusPlanConfiguration());
        modelBuilder.ApplyConfiguration(new CompensationBandConfiguration());
        modelBuilder.ApplyConfiguration(new MeritMatrixConfiguration());
        modelBuilder.ApplyConfiguration(new IncrementBudgetPoolConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeCompensationRecordConfiguration());
        modelBuilder.ApplyConfiguration(new CompensationCycleConfiguration());
        modelBuilder.ApplyConfiguration(new EmployeeCompReviewConfiguration());
        modelBuilder.ApplyConfiguration(new BonusPoolConfiguration());
    }
}
