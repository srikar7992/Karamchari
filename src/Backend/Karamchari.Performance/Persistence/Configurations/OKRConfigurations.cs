using Karamchari.Performance.Domain.OKRs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karamchari.Performance.Persistence.Configurations;

internal sealed class OKRCycleConfiguration : IEntityTypeConfiguration<OKRCycle>
{
    public void Configure(EntityTypeBuilder<OKRCycle> b)
    {
        b.ToTable("OKRCycles");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(60);
        b.Property(x => x.Label).IsRequired().HasMaxLength(200);
        b.Property(x => x.Scope).IsRequired();
        b.Property(x => x.Status).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();
    }
}

internal sealed class ObjectiveConfiguration : IEntityTypeConfiguration<Objective>
{
    public void Configure(EntityTypeBuilder<Objective> b)
    {
        b.ToTable("Objectives");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(60);
        b.Property(x => x.Title).IsRequired().HasMaxLength(400);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.OwnerType).IsRequired();
        b.Property(x => x.Status).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.TenantId, x.CycleId, x.OwnerId });

        b.OwnsMany(x => x.KeyResults, kr =>
        {
            kr.ToTable("KeyResults");
            kr.WithOwner().HasForeignKey("ObjectiveId");
            kr.HasKey(x => x.Id);
            kr.Property(x => x.Title).IsRequired().HasMaxLength(400);
            kr.Property(x => x.Type).IsRequired();
            kr.Property(x => x.Status).IsRequired();
            kr.Property(x => x.Weight).HasPrecision(5, 4);
            kr.Property(x => x.Target).HasPrecision(18, 4);
            kr.Property(x => x.CurrentValue).HasPrecision(18, 4);
            kr.Property(x => x.Score).HasPrecision(5, 4);

            kr.OwnsMany(x => x.CheckIns, ci =>
            {
                ci.ToTable("KRCheckIns");
                ci.WithOwner().HasForeignKey("KeyResultId");
                ci.HasKey(x => x.Id);
                ci.Property(x => x.Value).HasPrecision(18, 4);
                ci.Property(x => x.ConfidenceLevel).IsRequired();
                ci.Property(x => x.Notes).HasMaxLength(1000);
            });
        });
    }
}
