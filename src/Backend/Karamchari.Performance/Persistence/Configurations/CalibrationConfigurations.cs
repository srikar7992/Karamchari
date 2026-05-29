using Karamchari.Performance.Domain.Calibration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karamchari.Performance.Persistence.Configurations;

internal sealed class CalibrationSessionConfiguration : IEntityTypeConfiguration<CalibrationSession>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<CalibrationSession> b)
    {
        b.ToTable("CalibrationSessions");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(60);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.TenantId, x.ReviewCycleId, x.Name }).IsUnique();

        b.Property(x => x.BellCurvePolicy)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<BellCurvePolicy>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
            .HasColumnType("nvarchar(max)");

        b.OwnsMany(x => x.PanelMembers, pm =>
        {
            pm.ToTable("CalibrationPanelMembers");
            pm.WithOwner().HasForeignKey("SessionId");
            pm.HasKey(x => x.Id);
            pm.Property(x => x.EmployeeId).IsRequired();
            pm.Property(x => x.Role).IsRequired();
        });

        b.OwnsMany(x => x.Entries, e =>
        {
            e.ToTable("CalibrationEntries");
            e.WithOwner().HasForeignKey("SessionId");
            e.HasKey(x => x.Id);
            e.Property(x => x.TenantId).IsRequired().HasMaxLength(60);
            e.Property(x => x.EmployeeId).IsRequired();
            e.Property(x => x.PreCalibrationScore).HasPrecision(5, 2);
            e.Property(x => x.PreCalibrationBucket).IsRequired().HasMaxLength(50);
            e.Property(x => x.CalibratedScore).HasPrecision(5, 2);
            e.Property(x => x.CalibratedBucket).IsRequired().HasMaxLength(50);

            e.HasIndex(x => new { x.SessionId, x.EmployeeId }).IsUnique()
                .HasDatabaseName("IX_CalibrationEntries_Session_Employee");

            e.OwnsMany(x => x.Adjustments, adj =>
            {
                adj.ToTable("CalibrationAdjustmentRecords");
                adj.WithOwner().HasForeignKey("CalibrationEntryId");
                adj.HasKey(x => x.Id);
                adj.Property(x => x.PreviousScore).HasPrecision(5, 2);
                adj.Property(x => x.NewScore).HasPrecision(5, 2);
                adj.Property(x => x.PreviousBucket).IsRequired().HasMaxLength(50);
                adj.Property(x => x.NewBucket).IsRequired().HasMaxLength(50);
                adj.Property(x => x.Justification).IsRequired().HasMaxLength(1000);
            });
        });
    }
}
