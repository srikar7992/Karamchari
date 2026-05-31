using Karamchari.Performance.Domain.Reviews;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karamchari.Performance.Persistence.Configurations;

internal sealed class ReviewTemplateConfiguration : IEntityTypeConfiguration<ReviewTemplate>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<ReviewTemplate> b)
    {
        b.ToTable("ReviewTemplates");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(60);
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.RowVersion).IsRowVersion();

        // Sections + nested Questions stored as JSON â€” immutable value objects.
        // Avoids cross-table joins for template read; template structure changes = new template.
        b.Property(x => x.Sections)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<ReviewSection>>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
            .HasColumnType("nvarchar(max)");
    }
}

internal sealed class ReviewCycleConfiguration : IEntityTypeConfiguration<ReviewCycle>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<ReviewAssignment> b)
    {
        b.ToTable("ReviewAssignments");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(60);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.TenantId, x.CycleId, x.RevieweeId }).IsUnique();

        // ReviewerSlots are JSON â€” small, read with the assignment, not queried independently.
        b.Property(x => x.ReviewerSlots)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<ReviewerSlot>>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
            .HasColumnType("nvarchar(max)");
    }
}

internal sealed class ReviewSubmissionConfiguration : IEntityTypeConfiguration<ReviewSubmission>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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

        b.Property(x => x.ReopenHistory)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<ReopenRecord>>(v, (System.Text.Json.JsonSerializerOptions?)null)!)
            .HasColumnType("nvarchar(max)");
    }
}
