using Karamchari.HR.Domain.Relationships;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karamchari.HR.Persistence.Configurations;

internal sealed class EmployeeRelationshipConfiguration : IEntityTypeConfiguration<EmployeeRelationship>
{
    public void Configure(EntityTypeBuilder<EmployeeRelationship> b)
    {
        b.ToTable("EmployeeRelationships");
        b.HasKey(x => x.Id);

        b.Property(x => x.TenantId).IsRequired().HasMaxLength(50);
        b.Property(x => x.FromEmployeeId).IsRequired();
        b.Property(x => x.ToEmployeeId).IsRequired();
        b.Property(x => x.RelationshipType).IsRequired().HasConversion<string>().HasMaxLength(30);
        b.Property(x => x.EffectiveFrom).IsRequired();
        b.Property(x => x.EffectiveTo);
        b.Property(x => x.Context).HasMaxLength(200);

        // Active relationships by type for a given employee (manager visibility queries)
        b.HasIndex(x => new { x.TenantId, x.ToEmployeeId, x.RelationshipType, x.EffectiveTo })
            .HasDatabaseName("IX_EmployeeRelationships_ToEmployee_Type");

        // Reverse: all employees managed by a given manager
        b.HasIndex(x => new { x.TenantId, x.FromEmployeeId, x.RelationshipType, x.EffectiveTo })
            .HasDatabaseName("IX_EmployeeRelationships_FromEmployee_Type");

        // Prevent duplicate active relationships of same type between the same pair
        b.HasIndex(x => new { x.TenantId, x.FromEmployeeId, x.ToEmployeeId, x.RelationshipType, x.EffectiveTo })
            .IsUnique()
            .HasFilter("[EffectiveTo] IS NULL")
            .HasDatabaseName("UIX_EmployeeRelationships_ActivePair");
    }
}
