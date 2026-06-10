// -----------------------------------------------------------------------
// <copyright file="OnboardingConfigurations.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Onboarding.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karamchari.Onboarding.Persistence.Configurations;

internal sealed class OnboardingCaseConfiguration : IEntityTypeConfiguration<OnboardingCase>
{
    public void Configure(EntityTypeBuilder<OnboardingCase> b)
    {
        b.ToTable("OnboardingCases");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(v => v.Value, v => new OnboardingCaseId(v));
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        b.Property(x => x.EmployeeName).IsRequired().HasMaxLength(200);
        b.Property(x => x.CaseType).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Department).HasMaxLength(100);
        b.Property(x => x.Role).HasMaxLength(200);
        b.Property(x => x.TemplateId).HasConversion(
            v => v == null ? (Guid?)null : v.Value,
            v => v == null ? null : new OnboardingTemplateId(v.Value));

        b.HasMany(x => x.Tasks).WithOne().HasForeignKey(t => t.CaseId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Documents).WithOne().HasForeignKey(d => d.CaseId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.CaseType })
            .HasDatabaseName("IX_OnboardingCases_Tenant_Employee_Type");
        b.HasIndex(x => new { x.TenantId, x.Status })
            .HasDatabaseName("IX_OnboardingCases_Tenant_Status");
    }
}

internal sealed class OnboardingTaskConfiguration : IEntityTypeConfiguration<OnboardingTask>
{
    public void Configure(EntityTypeBuilder<OnboardingTask> b)
    {
        b.ToTable("OnboardingTasks");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(v => v.Value, v => new OnboardingTaskId(v));
        b.Property(x => x.CaseId).HasConversion(v => v.Value, v => new OnboardingCaseId(v));
        b.Property(x => x.Title).IsRequired().HasMaxLength(300);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.Category).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.AssignedToEmployeeId).HasMaxLength(50);
        b.Property(x => x.CompletionNotes).HasMaxLength(500);
        b.Property(x => x.SkipReason).HasMaxLength(500);
        b.Ignore(x => x.IsOverdue);

        b.HasIndex(x => new { x.CaseId, x.Status })
            .HasDatabaseName("IX_OnboardingTasks_Case_Status");
    }
}

internal sealed class OnboardingDocumentConfiguration : IEntityTypeConfiguration<OnboardingDocument>
{
    public void Configure(EntityTypeBuilder<OnboardingDocument> b)
    {
        b.ToTable("OnboardingDocuments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(v => v.Value, v => new OnboardingDocumentId(v));
        b.Property(x => x.CaseId).HasConversion(v => v.Value, v => new OnboardingCaseId(v));
        b.Property(x => x.DocumentType).HasConversion<string>().HasMaxLength(50);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.FileName).HasMaxLength(500);
        b.Property(x => x.StorageReference).HasMaxLength(2000);
        b.Property(x => x.VerifiedByEmployeeId).HasMaxLength(50);
        b.Property(x => x.RejectionReason).HasMaxLength(500);

        b.HasIndex(x => new { x.CaseId, x.DocumentType })
            .HasDatabaseName("IX_OnboardingDocuments_Case_Type");
    }
}

internal sealed class OnboardingTemplateConfiguration : IEntityTypeConfiguration<OnboardingTemplate>
{
    public void Configure(EntityTypeBuilder<OnboardingTemplate> b)
    {
        b.ToTable("OnboardingTemplates");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasConversion(v => v.Value, v => new OnboardingTemplateId(v));
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.CaseType).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Department).HasMaxLength(100);
        b.Property(x => x.Role).HasMaxLength(200);

        b.HasMany(x => x.Tasks).WithOne().HasForeignKey(t => t.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => new { x.TenantId, x.CaseType, x.IsActive })
            .HasDatabaseName("IX_OnboardingTemplates_Tenant_Type_Active");
    }
}

internal sealed class TemplatTaskConfiguration : IEntityTypeConfiguration<TemplateTask>
{
    public void Configure(EntityTypeBuilder<TemplateTask> b)
    {
        b.ToTable("OnboardingTemplateTasks");
        b.HasKey(x => x.Id);
        b.Property(x => x.TemplateId).HasConversion(v => v.Value, v => new OnboardingTemplateId(v));
        b.Property(x => x.Title).IsRequired().HasMaxLength(300);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.Category).HasConversion<string>().HasMaxLength(20);
    }
}
