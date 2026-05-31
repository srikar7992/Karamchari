using Karamchari.Performance.Domain.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karamchari.Performance.Persistence.Configurations;

internal sealed class PerformanceSnapshotConfiguration : IEntityTypeConfiguration<PerformanceSnapshot>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<PerformanceSnapshot> b)
    {
        b.ToTable("PerformanceSnapshots");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(60);
        b.Property(x => x.CycleName).IsRequired().HasMaxLength(200);
        b.Property(x => x.PerformanceBucket).IsRequired().HasMaxLength(50);
        b.Property(x => x.CompositeScore).HasPrecision(5, 2);
        b.Property(x => x.GoalCompletionRate).HasPrecision(5, 2);
        b.Property(x => x.OKRScore).HasPrecision(5, 2);
        b.Property(x => x.KPIScore).HasPrecision(5, 2);
        b.Property(x => x.PeerFeedbackScore).HasPrecision(5, 2);

        b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.ReviewCycleId });
        b.HasIndex(x => new { x.TenantId, x.IsHighPerformer, x.IsAtRetentionRisk });
    }
}
