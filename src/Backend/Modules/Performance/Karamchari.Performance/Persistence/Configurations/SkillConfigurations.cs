// -----------------------------------------------------------------------
// <copyright file="SkillConfigurations.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Performance.Domain.Skills;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karamchari.Performance.Persistence.Configurations;

internal sealed class SkillTaxonomyConfiguration : IEntityTypeConfiguration<SkillTaxonomy>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<SkillTaxonomy> b)
    {
        b.ToTable("SkillTaxonomies");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(60);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.OwnsMany(x => x.Categories, cat =>
        {
            cat.ToTable("SkillCategories");
            cat.WithOwner().HasForeignKey("TaxonomyId");
            cat.HasKey(x => x.Id);
            cat.Property(x => x.TenantId).IsRequired().HasMaxLength(60);
            cat.Property(x => x.Name).IsRequired().HasMaxLength(200);
            cat.Property(x => x.Description).HasMaxLength(1000);

            cat.OwnsMany(x => x.Skills, skill =>
            {
                skill.ToTable("Skills");
                skill.WithOwner().HasForeignKey("CategoryId");
                skill.HasKey(x => x.Id);
                skill.Property(x => x.TenantId).IsRequired().HasMaxLength(60);
                skill.Property(x => x.Name).IsRequired().HasMaxLength(200);
                skill.Property(x => x.Description).HasMaxLength(1000);
                skill.Property(x => x.Domain).IsRequired();

                skill.HasIndex(x => new { x.TenantId, x.Name, x.Domain })
                    .HasDatabaseName("IX_Skills_Tenant_Name_Domain");

                // ProficiencyDescriptors: small, rarely queried independently â†’ JSON
                skill.Property(x => x.ProficiencyDescriptors)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<List<SkillProficiencyDescriptor>>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
                    .HasColumnType("nvarchar(max)")
                    .Metadata.SetValueComparer(Karamchari.Core.Persistence.ValueComparers.ReadOnlyCollectionComparer<SkillProficiencyDescriptor>());
            });
        });
    }
}

internal sealed class EmployeeSkillProfileConfiguration : IEntityTypeConfiguration<EmployeeSkillProfile>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<EmployeeSkillProfile> b)
    {
        b.ToTable("EmployeeSkillProfiles");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(60);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.TenantId, x.EmployeeId }).IsUnique();

        b.OwnsMany(x => x.Assessments, a =>
        {
            a.ToTable("EmployeeSkillAssessments");
            a.WithOwner().HasForeignKey("ProfileId");
            a.HasKey(x => x.Id);
            a.Property(x => x.SkillId).IsRequired();
            a.Property(x => x.Level).IsRequired();
            a.Property(x => x.Source).IsRequired();
            a.Property(x => x.Evidence).HasMaxLength(2000);

            a.HasIndex(x => new { x.ProfileId, x.SkillId, x.AssessedOnUtc })
                .HasDatabaseName("IX_SkillAssessments_Profile_Skill_Date");
        });
    }
}
