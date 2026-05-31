using Karamchari.Performance.Domain.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karamchari.Performance.Persistence.Configurations;

internal sealed class ExportJobConfiguration : IEntityTypeConfiguration<ExportJob>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<ExportJob> b)
    {
        b.ToTable("ExportJobs");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        b.Property(x => x.RequestedByEmployeeId).IsRequired();
        b.Property(x => x.ReportType).IsRequired().HasConversion<string>().HasMaxLength(50);
        b.Property(x => x.ParametersJson).IsRequired().HasColumnType("nvarchar(max)");
        b.Property(x => x.IdempotencyKey).IsRequired().HasMaxLength(400);
        b.Property(x => x.Status).IsRequired().HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.BlobUri).HasMaxLength(2000);
        b.Property(x => x.ErrorMessage).HasMaxLength(1000);
        b.Property(x => x.CreatedOnUtc).IsRequired();

        // Idempotency: duplicate requests within the dedup window return the existing job.
        b.HasIndex(x => new { x.TenantId, x.IdempotencyKey })
            .HasDatabaseName("IX_ExportJobs_IdempotencyKey");

        // Delivery worker query: queued jobs ready to process.
        b.HasIndex(x => new { x.TenantId, x.Status, x.CreatedOnUtc })
            .HasDatabaseName("IX_ExportJobs_Status_CreatedAt");

        // Concurrent export quota check: active jobs per tenant.
        b.HasIndex(x => new { x.TenantId, x.RequestedByEmployeeId, x.Status })
            .HasDatabaseName("IX_ExportJobs_Tenant_Employee_Status");
    }
}
