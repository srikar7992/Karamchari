using Karamchari.Performance.Domain.Promotions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karamchari.Performance.Persistence.Configurations;

internal sealed class PromotionRecommendationConfiguration : IEntityTypeConfiguration<PromotionRecommendation>
{
    public void Configure(EntityTypeBuilder<PromotionRecommendation> b)
    {
        b.ToTable("PromotionRecommendations");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(60);
        b.Property(x => x.CurrentGrade).IsRequired().HasMaxLength(50);
        b.Property(x => x.ProposedGrade).IsRequired().HasMaxLength(50);
        b.Property(x => x.CurrentTitle).IsRequired().HasMaxLength(200);
        b.Property(x => x.ProposedTitle).IsRequired().HasMaxLength(200);
        b.Property(x => x.Justification).IsRequired().HasMaxLength(4000);
        b.Property(x => x.ReadinessScore).HasPrecision(5, 2);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.TenantId, x.EmployeeId, x.Status });

        b.OwnsMany(x => x.ApprovalActions, aa =>
        {
            aa.ToTable("PromotionApprovalActions");
            aa.WithOwner().HasForeignKey("RecommendationId");
            aa.HasKey(x => x.Id);
            aa.Property(x => x.Stage).IsRequired();
            aa.Property(x => x.ActorId).IsRequired();
            aa.Property(x => x.Approved).IsRequired();
            aa.Property(x => x.Comments).HasMaxLength(1000);
        });
    }
}
