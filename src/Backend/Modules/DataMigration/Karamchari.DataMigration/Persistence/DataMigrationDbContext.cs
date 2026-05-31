using Karamchari.Core.Multitenancy;
using Karamchari.Core.Persistence;
using Karamchari.DataMigration.Domain.Importing;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.DataMigration.Persistence;

/// <summary>
/// Persistence context for the Data Migration module.
/// </summary>
public sealed class DataMigrationDbContext : KaramchariDbContext
{
    public DataMigrationDbContext(DbContextOptions<DataMigrationDbContext> options, ITenantProvider tenantProvider)
        : base(options, tenantProvider)
    {
    }

    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();
    public DbSet<ImportRecord> ImportRecords => Set<ImportRecord>();
    public DbSet<HistoricalSalaryRecord> HistoricalSalaryRecords => Set<HistoricalSalaryRecord>();
    public DbSet<HistoricalPayrollSummary> HistoricalPayrollSummaries => Set<HistoricalPayrollSummary>();

    protected override void OnDomainModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ImportJob>(b =>
        {
            b.ToTable("Migration_ImportJobs");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.ImportType).HasMaxLength(100).IsRequired();
            b.Property(x => x.StoredFileId).HasMaxLength(256).IsRequired();
            b.Property(x => x.FileHash).HasMaxLength(256).IsRequired();
            b.Property(x => x.CreatedBy).HasMaxLength(256).IsRequired();

            b.HasMany(x => x.Records)
             .WithOne()
             .HasForeignKey(x => x.ImportJobId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => new { x.TenantId, x.FileHash });
        });

        modelBuilder.Entity<ImportRecord>(b =>
        {
            b.ToTable("Migration_ImportRecords");
            b.HasKey(x => x.Id);
            b.Property(x => x.ErrorMessage).HasMaxLength(2000);
            b.HasIndex(x => x.ImportJobId);
        });

        modelBuilder.Entity<HistoricalSalaryRecord>(b =>
        {
            b.ToTable("Migration_HistoricalSalaryRecords");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.EmployeeNumber).HasMaxLength(64).IsRequired();
            b.Property(x => x.ComponentName).HasMaxLength(128).IsRequired();
            b.Property(x => x.Frequency).HasMaxLength(64);
            b.Property(x => x.Amount).HasPrecision(18, 4);
            b.HasIndex(x => new { x.TenantId, x.ImportJobId });
            b.HasIndex(x => new { x.TenantId, x.EmployeeNumber });
        });

        modelBuilder.Entity<HistoricalPayrollSummary>(b =>
        {
            b.ToTable("Migration_HistoricalPayrollSummaries");
            b.HasKey(x => x.Id);
            b.Property(x => x.TenantId).HasMaxLength(64).IsRequired();
            b.Property(x => x.EmployeeNumber).HasMaxLength(64).IsRequired();
            b.Property(x => x.GrossPay).HasPrecision(18, 4);
            b.Property(x => x.Deductions).HasPrecision(18, 4);
            b.Property(x => x.NetPay).HasPrecision(18, 4);
            b.HasIndex(x => new { x.TenantId, x.ImportJobId });
            b.HasIndex(x => new { x.TenantId, x.EmployeeNumber, x.Year, x.Month }).IsUnique();
        });
    }
}
