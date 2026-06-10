// -----------------------------------------------------------------------
// <copyright file="AssetManagementDbContext.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.AssetManagement.Domain;
using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.AssetManagement.Persistence;

public sealed class AssetManagementDbContext : KaramchariDbContext
{
    public AssetManagementDbContext(DbContextOptions<AssetManagementDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider) { }

    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetCategory> AssetCategories => Set<AssetCategory>();
    public DbSet<AssetDepreciationSchedule> DepreciationSchedules => Set<AssetDepreciationSchedule>();
    public DbSet<AssetMaintenanceRecord> MaintenanceRecords => Set<AssetMaintenanceRecord>();

    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        base.OnDomainModelCreating(modelBuilder);

        modelBuilder.Entity<AssetCategory>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(id => id.Value, v => new AssetCategoryId(v));
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Asset>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(id => id.Value, v => new AssetId(v));
            e.Property(x => x.CategoryId).HasConversion(id => id.Value, v => new AssetCategoryId(v));
            e.Property(x => x.Name).HasMaxLength(300).IsRequired();
            e.Property(x => x.SerialNumber).HasMaxLength(200);
            e.Property(x => x.AssetTag).HasMaxLength(100);
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.Condition).HasConversion<string>();
            e.Ignore(x => x.CurrentAssignment);
            e.OwnsMany(x => x.Assignments, a =>
            {
                a.WithOwner().HasForeignKey("AssetRootId");
                a.HasKey(x => x.Id);
                a.Property(x => x.AssetId).HasConversion(id => id.Value, v => new AssetId(v));
                a.Property(x => x.ConditionOnReturn).HasConversion<string>();
                a.Property(x => x.Notes).HasMaxLength(1000);
            });
        });

        modelBuilder.Entity<AssetDepreciationSchedule>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(id => id.Value, v => new DepreciationScheduleId(v));
            e.Property(x => x.AssetId).HasConversion(id => id.Value, v => new AssetId(v));
            e.Property(x => x.Method).HasConversion<string>();
            e.Property(x => x.PurchaseValue).HasPrecision(18, 2);
            e.Property(x => x.ResidualValue).HasPrecision(18, 2);
            e.Property(x => x.CurrentBookValue).HasPrecision(18, 2);
            e.Ignore(x => x.AccumulatedDepreciation);
            e.OwnsMany(x => x.Periods, p =>
            {
                p.WithOwner().HasForeignKey("ScheduleId");
                p.HasKey("ScheduleId", "Year");
                p.Property(x => x.AssetId).HasConversion(id => id.Value, v => new AssetId(v));
                p.Property(x => x.Amount).HasPrecision(18, 2);
                p.Property(x => x.BookValueAfter).HasPrecision(18, 2);
            });
        });

        modelBuilder.Entity<AssetMaintenanceRecord>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasConversion(id => id.Value, v => new MaintenanceRecordId(v));
            e.Property(x => x.AssetId).HasConversion(id => id.Value, v => new AssetId(v));
            e.Property(x => x.Type).HasConversion<string>();
            e.Property(x => x.Status).HasConversion<string>();
            e.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            e.Property(x => x.Vendor).HasMaxLength(300);
            e.Property(x => x.EstimatedCost).HasPrecision(18, 2);
            e.Property(x => x.ActualCost).HasPrecision(18, 2);
            e.Property(x => x.TechnicianNotes).HasMaxLength(2000);
            e.Ignore(x => x.IsUnderWarranty);
        });
    }
}
