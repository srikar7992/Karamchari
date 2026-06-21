using Karamchari.Analytics.Domain;
using Karamchari.Core.Multitenancy;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Analytics.Persistence;

public sealed class AnalyticsDbContext(
    DbContextOptions<AnalyticsDbContext> options,
    ITenantProvider tenantProvider) : DbContext(options)
{
    // Empty string when no HTTP context (background jobs) → filter allows all tenants.
    // Non-empty in HTTP requests → filter enforces tenant isolation as defense-in-depth.
    public string CurrentTenantId =>
        tenantProvider.TryGetCurrentTenantId(out var tid) ? tid ?? string.Empty : string.Empty;

    public DbSet<DimEmployee> DimEmployees => Set<DimEmployee>();
    public DbSet<DimDate> DimDates => Set<DimDate>();
    public DbSet<FactWorkforceDaily> FactWorkforceDaily => Set<FactWorkforceDaily>();
    public DbSet<FactAttrition> FactAttrition => Set<FactAttrition>();
    public DbSet<FactHiring> FactHiring => Set<FactHiring>();
    public DbSet<AggMonthlyHeadcount> AggMonthlyHeadcounts => Set<AggMonthlyHeadcount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("analytics");

        modelBuilder.Entity<DimEmployee>(e =>
        {
            e.ToTable("Dim_Employee");
            e.HasKey(x => x.EmployeeId);
            e.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            e.Property(x => x.FullName).HasMaxLength(512).IsRequired();
            e.Property(x => x.Grade).HasMaxLength(64);
            e.Property(x => x.HireDate).HasColumnType("date");
            e.Property(x => x.TerminationDate).HasColumnType("date");
            e.HasIndex(x => new { x.TenantId, x.IsActive });
        });

        modelBuilder.Entity<DimDate>(e =>
        {
            e.ToTable("Dim_Date");
            e.HasKey(x => x.DateKey);
            e.Property(x => x.Date).HasColumnType("date");
        });

        modelBuilder.Entity<FactWorkforceDaily>(e =>
        {
            e.ToTable("Fact_WorkforceDaily");
            e.HasKey(x => x.Id);
            e.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            e.Property(x => x.BasicSalary).HasPrecision(18, 2);
            e.Property(x => x.TotalCTC).HasPrecision(18, 2);
            e.Property(x => x.LeaveBalance).HasPrecision(10, 2);
            e.HasIndex(x => new { x.TenantId, x.DateKey });
            e.HasIndex(x => new { x.TenantId, x.EmployeeId, x.DateKey }).IsUnique();
        });

        modelBuilder.Entity<FactAttrition>(e =>
        {
            e.ToTable("Fact_Attrition");
            e.HasKey(x => x.Id);
            e.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            e.Property(x => x.TerminationType).HasMaxLength(64).IsRequired();
            e.HasIndex(x => new { x.TenantId, x.DateKey });
        });

        modelBuilder.Entity<FactHiring>(e =>
        {
            e.ToTable("Fact_Hiring");
            e.HasKey(x => x.Id);
            e.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            e.Property(x => x.SourceChannel).HasMaxLength(128);
            e.HasIndex(x => new { x.TenantId, x.DateKey });
        });

        modelBuilder.Entity<AggMonthlyHeadcount>(e =>
        {
            e.ToTable("Agg_MonthlyHeadcount");
            e.HasKey(x => new { x.TenantId, x.Year, x.Month, x.DepartmentId });
            e.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            e.Ignore(x => x.NetChange); // computed
        });

        // Global query filters — defense-in-depth on top of service-layer explicit filters.
        // Empty CurrentTenantId (background jobs) → passes; non-empty (HTTP) → enforces tenant.
        modelBuilder.Entity<DimEmployee>().HasQueryFilter(
            e => string.IsNullOrEmpty(CurrentTenantId) || e.TenantId == CurrentTenantId);
        modelBuilder.Entity<FactWorkforceDaily>().HasQueryFilter(
            e => string.IsNullOrEmpty(CurrentTenantId) || e.TenantId == CurrentTenantId);
        modelBuilder.Entity<FactAttrition>().HasQueryFilter(
            e => string.IsNullOrEmpty(CurrentTenantId) || e.TenantId == CurrentTenantId);
        modelBuilder.Entity<FactHiring>().HasQueryFilter(
            e => string.IsNullOrEmpty(CurrentTenantId) || e.TenantId == CurrentTenantId);
        modelBuilder.Entity<AggMonthlyHeadcount>().HasQueryFilter(
            e => string.IsNullOrEmpty(CurrentTenantId) || e.TenantId == CurrentTenantId);
    }
}
