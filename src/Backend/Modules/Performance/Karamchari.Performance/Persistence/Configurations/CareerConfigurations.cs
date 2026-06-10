// -----------------------------------------------------------------------
// <copyright file="CareerConfigurations.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Performance.Domain.Career;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karamchari.Performance.Persistence.Configurations;

internal sealed class CareerFrameworkConfiguration : IEntityTypeConfiguration<CareerFramework>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<CareerFramework> b)
    {
        b.ToTable("CareerFrameworks");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(60);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.OwnsMany(x => x.Tracks, t =>
        {
            t.ToTable("CareerTracks");
            t.WithOwner().HasForeignKey("FrameworkId");
            t.HasKey(x => x.Id);
            t.Property(x => x.TenantId).IsRequired().HasMaxLength(60);
            t.Property(x => x.Name).IsRequired().HasMaxLength(200);
            t.Property(x => x.Type).IsRequired();
            t.Property(x => x.Description).HasMaxLength(1000);

            t.OwnsMany(x => x.Levels, lvl =>
            {
                lvl.ToTable("CareerLevels");
                lvl.WithOwner().HasForeignKey("TrackId");
                lvl.HasKey(x => x.Id);
                lvl.Property(x => x.Code).IsRequired().HasMaxLength(20);
                lvl.Property(x => x.DisplayName).IsRequired().HasMaxLength(100);
                lvl.Property(x => x.Order).IsRequired();
                lvl.Property(x => x.Description).HasMaxLength(1000);

                lvl.HasIndex(x => new { x.TrackId, x.Order }).IsUnique()
                    .HasDatabaseName("IX_CareerLevels_Track_Order");

                // CompetencyRequirements: small per level, not queried independently â†’ JSON
                lvl.Property(x => x.CompetencyRequirements)
                    .HasConversion(
                        v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                        v => System.Text.Json.JsonSerializer.Deserialize<List<CompetencyRequirement>>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
                    .HasColumnType("nvarchar(max)")
                    .Metadata.SetValueComparer(Karamchari.Core.Persistence.ValueComparers.ReadOnlyCollectionComparer<CompetencyRequirement>());
            });
        });
    }
}

internal sealed class EmployeeGrowthPlanConfiguration : IEntityTypeConfiguration<EmployeeGrowthPlan>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<EmployeeGrowthPlan> b)
    {
        b.ToTable("EmployeeGrowthPlans");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(60);
        b.Property(x => x.TargetLevelDisplay).HasMaxLength(100);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.Status });

        b.OwnsMany(x => x.Actions, a =>
        {
            a.ToTable("DevelopmentActions");
            a.WithOwner().HasForeignKey("GrowthPlanId");
            a.HasKey(x => x.Id);
            a.Property(x => x.Title).IsRequired().HasMaxLength(300);
            a.Property(x => x.Type).IsRequired();
            a.Property(x => x.Description).HasMaxLength(2000);
            a.Property(x => x.Status).IsRequired();
            a.Property(x => x.CompletionNotes).HasMaxLength(2000);
        });
    }
}
