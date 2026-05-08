using Karamchari.Performance.Domain.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karamchari.Performance.Persistence.Configurations;

internal sealed class ReviewTemplateConfiguration : IEntityTypeConfiguration<ReviewTemplate>
{
    public void Configure(EntityTypeBuilder<ReviewTemplate> b)
    {
        b.ToTable("ReviewTemplates");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(60);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.RowVersion).IsRowVersion();

        // Sections + nested Questions stored as JSON — immutable value objects.
        // Avoids cross-table joins for template read; template structure changes = new template.
        b.OwnsMany(x => x.Sections, s =>
        {
            s.ToJson();
            s.Property(p => p.SectionId).IsRequired();
            s.Property(p => p.Title).IsRequired().HasMaxLength(200);
            s.Property(p => p.Weight).HasPrecision(5, 4);
            s.OwnsMany(p => p.Questions, q =>
            {
                q.Property(x => x.QuestionId).IsRequired();
                q.Property(x => x.Text).IsRequired().HasMaxLength(1000);
                q.Property(x => x.Weight).HasPrecision(5, 4);
                q.Property(x => x.CompetencyCode).HasMaxLength(100);
            });
        });
    }
}

internal sealed class ReviewCycleConfiguration : IEntityTypeConfiguration<ReviewCycle>
{
    public void Configure(EntityTypeBuilder<ReviewCycle> b)
    {
        b.ToTable("ReviewCycles");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(60);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.TenantId, x.Name, x.ReviewPeriodStart }).IsUnique();
    }
}

internal sealed class ReviewAssignmentConfiguration : IEntityTypeConfiguration<ReviewAssignment>
{
    public void Configure(EntityTypeBuilder<ReviewAssignment> b)
    {
        b.ToTable("ReviewAssignments");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(60);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.TenantId, x.CycleId, x.RevieweeId }).IsUnique();

        // ReviewerSlots are JSON — small, read with the assignment, not queried independently.
        b.OwnsMany(x => x.ReviewerSlots, rs =>
        {
            rs.ToJson();
            rs.Property(p => p.ReviewerId).IsRequired();
            rs.Property(p => p.Role).IsRequired();
            rs.Property(p => p.IsCompleted).IsRequired();
        });
    }
}

internal sealed class ReviewSubmissionConfiguration : IEntityTypeConfiguration<ReviewSubmission>
{
    public void Configure(EntityTypeBuilder<ReviewSubmission> b)
    {
        b.ToTable("ReviewSubmissions");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(60);
        b.Property(x => x.ComputedScore).HasPrecision(5, 2);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.AssignmentId, x.ReviewerId });

        b.OwnsMany(x => x.Responses, r =>
        {
            r.ToTable("ReviewResponses");
            r.WithOwner().HasForeignKey("SubmissionId");
            r.HasKey(x => x.Id);
            r.Property(x => x.QuestionId).IsRequired();
            r.Property(x => x.RatingValue).HasPrecision(5, 2);
            r.Property(x => x.TextValue).HasMaxLength(4000);
        });

        b.OwnsMany(x => x.ReopenHistory, rh =>
        {
            rh.ToJson();
            rh.Property(p => p.ReopenedBy).IsRequired();
            rh.Property(p => p.Justification).IsRequired().HasMaxLength(1000);
            rh.Property(p => p.ReopenedOnUtc).IsRequired();
        });
    }
}
