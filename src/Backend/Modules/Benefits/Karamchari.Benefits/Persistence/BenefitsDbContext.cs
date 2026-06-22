// -----------------------------------------------------------------------
// <copyright file="BenefitsDbContext.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Benefits.Persistence;

using Karamchari.Benefits.Domain;
using Karamchari.Benefits.Domain.Deductions;
using Karamchari.Benefits.Persistence.Configurations;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class BenefitsDbContext : KaramchariDbContext
{
    public BenefitsDbContext(DbContextOptions<BenefitsDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }

    public DbSet<BenefitPlan> Plans => Set<BenefitPlan>();
    public DbSet<EnrollmentWindow> Windows => Set<EnrollmentWindow>();
    public DbSet<BenefitEnrollment> Enrollments => Set<BenefitEnrollment>();
    public DbSet<BenefitElection> Elections => Set<BenefitElection>();
    public DbSet<BenefitDependent> Dependents => Set<BenefitDependent>();
    public DbSet<LifeEvent> LifeEvents => Set<LifeEvent>();
    public DbSet<BenefitDeductionRecord> DeductionRecords => Set<BenefitDeductionRecord>();

    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        base.OnDomainModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BenefitsDbContext).Assembly);
    }
}
