using Karamchari.Compensation.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karamchari.Compensation.Persistence.Configurations;

internal sealed class BonusPlanConfiguration : IEntityTypeConfiguration<BonusPlan>
{
    public void Configure(EntityTypeBuilder<BonusPlan> b)
    {
        b.ToTable("BonusPlans");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(50);
        b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
        b.Property(x => x.TotalBudget).HasPrecision(18, 2);
        b.Ignore(x => x.AllocatedBudget);
        b.Ignore(x => x.RemainingBudget);

        b.OwnsMany(x => x.Allocations, a =>
        {
            a.ToTable("BonusAllocations");
            a.HasKey(x => x.Id);
            a.Property(x => x.TargetAmount).HasPrecision(18, 2);
            a.Property(x => x.ActualPaidAmount).HasPrecision(18, 2);
            a.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
            a.Property(x => x.Notes).HasMaxLength(500);
        });
    }
}

internal sealed class CompensationBandConfiguration : IEntityTypeConfiguration<CompensationBand>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<CompensationBand> b)
    {
        b.ToTable("CompensationBands");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.JobFamily).IsRequired().HasMaxLength(100);
        b.Property(x => x.CareerLevelCode).IsRequired().HasMaxLength(50);
        b.Property(x => x.BandType).IsRequired().HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
        b.Property(x => x.MinimumAnnual).HasPrecision(18, 2);
        b.Property(x => x.MidpointAnnual).HasPrecision(18, 2);
        b.Property(x => x.MaximumAnnual).HasPrecision(18, 2);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.TenantId, x.JobFamily, x.CareerLevelCode, x.BandType, x.EffectiveFrom })
            .HasDatabaseName("IX_CompensationBands_JobFamily_Level_Type_Date");
    }
}

internal sealed class MeritMatrixConfiguration : IEntityTypeConfiguration<MeritMatrix>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<MeritMatrix> b)
    {
        b.ToTable("MeritMatrices");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.TenantId, x.ReviewYear })
            .IsUnique()
            .HasDatabaseName("UIX_MeritMatrices_Tenant_Year");

        b.OwnsMany(x => x.Cells, c =>
        {
            c.ToTable("MeritMatrixCells");
            c.HasKey(x => x.Id);
            c.Property(x => x.MatrixId).IsRequired();
            c.Property(x => x.Rating).IsRequired().HasConversion<string>().HasMaxLength(30);
            c.Property(x => x.CompaRatioMin).HasPrecision(7, 2);
            c.Property(x => x.CompaRatioMax).HasPrecision(7, 2);
            c.Property(x => x.MinIncrementPercent).HasPrecision(7, 2);
            c.Property(x => x.MidIncrementPercent).HasPrecision(7, 2);
            c.Property(x => x.MaxIncrementPercent).HasPrecision(7, 2);
            c.HasIndex(x => new { x.MatrixId, x.Rating, x.CompaRatioMin })
                .HasDatabaseName("IX_MeritMatrixCells_Matrix_Rating_Range");
        });
    }
}

internal sealed class IncrementBudgetPoolConfiguration : IEntityTypeConfiguration<IncrementBudgetPool>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<IncrementBudgetPool> b)
    {
        b.ToTable("IncrementBudgetPools");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        b.Property(x => x.DepartmentName).IsRequired().HasMaxLength(200);
        b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
        b.Property(x => x.TotalBudget).HasPrecision(18, 2);
        b.Property(x => x.AllocatedBudget).HasPrecision(18, 2);
        b.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.TenantId, x.DepartmentId, x.ReviewYear })
            .IsUnique()
            .HasDatabaseName("UIX_IncrementBudgetPools_Dept_Year");
    }
}

internal sealed class EmployeeCompensationRecordConfiguration : IEntityTypeConfiguration<EmployeeCompensationRecord>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<EmployeeCompensationRecord> b)
    {
        b.ToTable("EmployeeCompensationRecords");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        b.Property(x => x.EmployeeId).IsRequired();
        b.Property(x => x.CurrentAnnualCTC).HasPrecision(18, 2);
        b.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(3);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.TenantId, x.EmployeeId })
            .IsUnique()
            .HasDatabaseName("UIX_EmployeeCompensationRecords_Employee");

        b.OwnsMany(x => x.Revisions, r =>
        {
            r.ToTable("CompensationRevisions");
            r.HasKey(x => x.Id);
            r.Property(x => x.CompensationRecordId).IsRequired();
            r.Property(x => x.ExternalRevisionId).IsRequired();
            r.Property(x => x.PreviousAnnualCTC).HasPrecision(18, 2);
            r.Property(x => x.NewAnnualCTC).HasPrecision(18, 2);
            r.Property(x => x.IncrementPercent).HasPrecision(7, 4);
            r.Property(x => x.Reason).IsRequired().HasMaxLength(50);
            r.Property(x => x.ReferenceId).HasMaxLength(100);
            // Idempotency: same external revision cannot create two rows
            r.HasIndex(x => new { x.CompensationRecordId, x.ExternalRevisionId })
                .IsUnique()
                .HasDatabaseName("UIX_CompensationRevisions_ExternalId");
        });
    }
}
