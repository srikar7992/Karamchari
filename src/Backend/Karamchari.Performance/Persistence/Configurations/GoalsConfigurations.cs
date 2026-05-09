using Karamchari.Performance.Domain.Goals;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karamchari.Performance.Persistence.Configurations;

internal sealed class GoalCycleConfiguration : IEntityTypeConfiguration<GoalCycle>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<GoalCycle> b)
    {
        b.ToTable("GoalCycles");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(60);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Type).IsRequired();
        b.Property(x => x.Status).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.TenantId, x.Name, x.Type, x.StartDate })
            .IsUnique();
    }
}

internal sealed class GoalConfiguration : IEntityTypeConfiguration<Goal>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<Goal> b)
    {
        b.ToTable("Goals");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(60);
        b.Property(x => x.Title).IsRequired().HasMaxLength(400);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.OwnerType).IsRequired();
        b.Property(x => x.Type).IsRequired();
        b.Property(x => x.Status).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.TenantId, x.CycleId, x.OwnerId });

        b.OwnsMany(x => x.Updates, pu =>
        {
            pu.ToTable("GoalProgressUpdates");
            pu.WithOwner().HasForeignKey("GoalId");
            pu.HasKey(x => x.Id);
            pu.Property(x => x.Value).HasPrecision(18, 4);
            pu.Property(x => x.ProgressPercent).HasPrecision(5, 2).IsRequired();
            pu.Property(x => x.Notes).HasMaxLength(1000);
        });

        b.OwnsMany(x => x.Revisions, rr =>
        {
            rr.ToTable("GoalRevisionRecords");
            rr.WithOwner().HasForeignKey("GoalId");
            rr.HasKey(x => x.Id);
            rr.Property(x => x.PreviousTitle).IsRequired().HasMaxLength(400);
            rr.Property(x => x.NewTitle).IsRequired().HasMaxLength(400);
            rr.Property(x => x.Reason).IsRequired().HasMaxLength(1000);
        });
    }
}
