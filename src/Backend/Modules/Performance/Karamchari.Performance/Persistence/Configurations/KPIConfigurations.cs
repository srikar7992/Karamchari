// -----------------------------------------------------------------------
// <copyright file="KPIConfigurations.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Performance.Domain.KPIs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karamchari.Performance.Persistence.Configurations;

internal sealed class KPIDefinitionConfiguration : IEntityTypeConfiguration<KPIDefinition>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<KPIDefinition> b)
    {
        b.ToTable("KPIDefinitions");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(60);
        b.Property(x => x.Code).IsRequired().HasMaxLength(100);
        b.Property(x => x.DisplayName).IsRequired().HasMaxLength(200);
        b.Property(x => x.Unit).HasMaxLength(50);
        b.Property(x => x.FormulaExpression).HasMaxLength(4000);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();

        b.OwnsOne(x => x.Threshold, t =>
        {
            t.Property(p => p.RedBelow).HasPrecision(18, 4).IsRequired();
            t.Property(p => p.YellowBelow).HasPrecision(18, 4).IsRequired();
            t.Property(p => p.GreenAbove).HasPrecision(18, 4).IsRequired();
        });
    }
}

internal sealed class KPIResultConfiguration : IEntityTypeConfiguration<KPIResult>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<KPIResult> b)
    {
        b.ToTable("KPIResults");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(60);
        b.Property(x => x.PeriodLabel).IsRequired().HasMaxLength(20);
        b.Property(x => x.RawValue).HasPrecision(18, 4);
        b.Property(x => x.NormalizedScore).HasPrecision(5, 2);
        b.Property(x => x.OverrideJustification).HasMaxLength(1000);

        b.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.KPIDefinitionId, x.EmployeeId, x.PeriodLabel });
    }
}

internal sealed class KPISnapshotConfiguration : IEntityTypeConfiguration<KPISnapshot>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<KPISnapshot> b)
    {
        b.ToTable("KPISnapshots");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(60);
        b.Property(x => x.PeriodLabel).IsRequired().HasMaxLength(20);
        b.Property(x => x.KPICode).IsRequired().HasMaxLength(100);
        b.Property(x => x.KPIDisplayName).IsRequired().HasMaxLength(200);
        b.Property(x => x.Value).HasPrecision(18, 4);
        b.Property(x => x.NormalizedScore).HasPrecision(5, 2);

        b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.KPIDefinitionId, x.PeriodLabel });
    }
}
