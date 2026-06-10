// -----------------------------------------------------------------------
// <copyright file="OnboardingDbContext.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.Onboarding.Domain;
using Karamchari.Onboarding.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Onboarding.Persistence;

public sealed class OnboardingDbContext : KaramchariDbContext
{
    public OnboardingDbContext(DbContextOptions<OnboardingDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }

    public DbSet<OnboardingCase> Cases => Set<OnboardingCase>();
    public DbSet<OnboardingTask> Tasks => Set<OnboardingTask>();
    public DbSet<OnboardingDocument> Documents => Set<OnboardingDocument>();
    public DbSet<OnboardingTemplate> Templates => Set<OnboardingTemplate>();
    public DbSet<TemplateTask> TemplateTasks => Set<TemplateTask>();

    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        base.OnDomainModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OnboardingDbContext).Assembly);
    }
}
