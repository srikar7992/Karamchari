// -----------------------------------------------------------------------
// <copyright file="BenefitsConfigurations.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Benefits.Persistence.Configurations;

using Karamchari.Benefits.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class BenefitPlanConfiguration : IEntityTypeConfiguration<BenefitPlan>
{
    public void Configure(EntityTypeBuilder<BenefitPlan> b)
    {
        b.ToTable("BenefitPlans");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(v => v.Value, v => new BenefitPlanId(v));
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        b.Property(x => x.PlanName).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.Category).HasConversion<string>().HasMaxLength(40);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.ProviderName).IsRequired().HasMaxLength(200);
        b.Property(x => x.ExternalPlanCode).HasMaxLength(100);

        // WaitingPeriodRule — stored as two columns accessed via private backing fields.
        // WaitingRule (the public computed property) is excluded from the model.
        b.Property<string>("WaitingPeriodType")
            .HasField("_waitingPeriodType")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("WaitingPeriodType")
            .HasMaxLength(30)
            .IsRequired();
        b.Property<int>("WaitingPeriodDays")
            .HasField("_waitingPeriodDays")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("WaitingPeriodDays")
            .HasDefaultValue(0);
        b.Ignore(x => x.WaitingRule);

        // BenefitPlanTier — owned collection; EF Core creates a composite PK
        // (BenefitPlanId shadow FK + Tier enum) stored as a separate table.
        b.OwnsMany(x => x.Tiers, tier =>
        {
            tier.ToTable("BenefitPlanTiers");
            tier.WithOwner().HasForeignKey("BenefitPlanId");
            tier.HasKey("BenefitPlanId", nameof(BenefitPlanTier.Tier));
            tier.Property(t => t.Tier).HasConversion<string>().HasMaxLength(30);
            tier.Property(t => t.EmployeeMonthlyPremium).HasColumnType("decimal(10,2)");
            tier.Property(t => t.EmployerMonthlyPremium).HasColumnType("decimal(10,2)");
            tier.Ignore(t => t.TotalMonthlyPremium);
        });

        b.HasIndex(x => new { x.TenantId, x.Category, x.Status })
            .HasDatabaseName("IX_BenefitPlans_Tenant_Category_Status");
        b.HasIndex(x => new { x.TenantId, x.PlanYearStart, x.PlanYearEnd })
            .HasDatabaseName("IX_BenefitPlans_Tenant_PlanYear");
    }
}

internal sealed class EnrollmentWindowConfiguration : IEntityTypeConfiguration<EnrollmentWindow>
{
    public void Configure(EntityTypeBuilder<EnrollmentWindow> b)
    {
        b.ToTable("BenefitEnrollmentWindows");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(v => v.Value, v => new EnrollmentWindowId(v));
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        b.Property(x => x.WindowType).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        b.HasIndex(x => new { x.TenantId, x.WindowType, x.Status })
            .HasDatabaseName("IX_BenefitEnrollmentWindows_Tenant_Type_Status");
        b.HasIndex(x => new { x.TenantId, x.PlanYearStart })
            .HasDatabaseName("IX_BenefitEnrollmentWindows_Tenant_PlanYear");
    }
}

internal sealed class BenefitEnrollmentConfiguration : IEntityTypeConfiguration<BenefitEnrollment>
{
    public void Configure(EntityTypeBuilder<BenefitEnrollment> b)
    {
        b.ToTable("BenefitEnrollments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(v => v.Value, v => new BenefitEnrollmentId(v));
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        b.Property(x => x.EmployeeName).IsRequired().HasMaxLength(200);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);

        // Provenance FK — every enrollment must be traceable to its authorising window.
        b.Property(x => x.WindowId).HasConversion(v => v.Value, v => new EnrollmentWindowId(v));
        b.HasOne<EnrollmentWindow>().WithMany()
            .HasForeignKey(e => e.WindowId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Elections).WithOne()
            .HasForeignKey(e => e.EnrollmentId)
            .HasPrincipalKey(e => e.Id)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.Dependents).WithOne()
            .HasForeignKey(d => d.EnrollmentId)
            .HasPrincipalKey(e => e.Id)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasMany(x => x.LifeEvents).WithOne()
            .HasForeignKey(le => le.EnrollmentId)
            .HasPrincipalKey(e => e.Id)
            .OnDelete(DeleteBehavior.Cascade);

        // One active enrollment per employee per plan year.
        b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.PlanYearStart })
            .HasDatabaseName("IX_BenefitEnrollments_Tenant_Employee_PlanYear")
            .IsUnique();

        b.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("IX_BenefitEnrollments_Tenant_Status");
    }
}

internal sealed class BenefitElectionConfiguration : IEntityTypeConfiguration<BenefitElection>
{
    public void Configure(EntityTypeBuilder<BenefitElection> b)
    {
        b.ToTable("BenefitElections");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(v => v.Value, v => new BenefitElectionId(v));
        b.Property(x => x.EnrollmentId).HasConversion(v => v.Value, v => new BenefitEnrollmentId(v));
        b.Property(x => x.PlanId).HasConversion(v => v.Value, v => new BenefitPlanId(v));
        b.Property(x => x.Category).HasConversion<string>().HasMaxLength(40);
        b.Property(x => x.CoverageTier).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.EmployeeMonthlyPremium).HasColumnType("decimal(10,2)");
        b.Property(x => x.EmployerMonthlyPremium).HasColumnType("decimal(10,2)");
        b.Ignore(x => x.TotalMonthlyPremium);

        b.HasIndex(x => new { x.EnrollmentId, x.Category, x.IsActive })
            .HasDatabaseName("IX_BenefitElections_Enrollment_Category_Active");
    }
}

internal sealed class BenefitDependentConfiguration : IEntityTypeConfiguration<BenefitDependent>
{
    public void Configure(EntityTypeBuilder<BenefitDependent> b)
    {
        b.ToTable("BenefitDependents");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(v => v.Value, v => new BenefitDependentId(v));
        b.Property(x => x.EnrollmentId).HasConversion(v => v.Value, v => new BenefitEnrollmentId(v));
        b.Property(x => x.FirstName).IsRequired().HasMaxLength(100);
        b.Property(x => x.LastName).IsRequired().HasMaxLength(100);
        b.Property(x => x.Relationship).HasConversion<string>().HasMaxLength(30);

        b.HasIndex(x => new { x.EnrollmentId, x.IsRemoved })
            .HasDatabaseName("IX_BenefitDependents_Enrollment_Active");
    }
}

internal sealed class LifeEventConfiguration : IEntityTypeConfiguration<LifeEvent>
{
    public void Configure(EntityTypeBuilder<LifeEvent> b)
    {
        b.ToTable("BenefitLifeEvents");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(v => v.Value, v => new LifeEventId(v));
        b.Property(x => x.EnrollmentId).HasConversion(v => v.Value, v => new BenefitEnrollmentId(v));
        b.Property(x => x.EventType).HasConversion<string>().HasMaxLength(40);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.DocumentStorageReference).HasMaxLength(2000);
        b.Property(x => x.RejectionReason).HasMaxLength(500);
        b.Property(x => x.ReviewedByEmployeeId).HasMaxLength(50);
        b.Ignore(x => x.IsWindowOpen);

        b.HasIndex(x => new { x.EnrollmentId, x.EventType, x.Status })
            .HasDatabaseName("IX_BenefitLifeEvents_Enrollment_Type_Status");
    }
}
