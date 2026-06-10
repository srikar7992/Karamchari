// -----------------------------------------------------------------------
// <copyright file="FeedbackConfigurations.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Performance.Domain.Feedback;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Karamchari.Performance.Persistence.Configurations;

internal sealed class FeedbackRequestConfiguration : IEntityTypeConfiguration<FeedbackRequest>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<FeedbackRequest> b)
    {
        b.ToTable("FeedbackRequests");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(60);
        b.Property(x => x.Context).HasMaxLength(2000);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.TenantId, x.SubjectEmployeeId, x.Status });
    }
}

internal sealed class FeedbackSubmissionConfiguration : IEntityTypeConfiguration<FeedbackSubmission>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Configure(EntityTypeBuilder<FeedbackSubmission> b)
    {
        b.ToTable("FeedbackSubmissions");
        b.HasKey(x => x.Id);
        b.Property(x => x.TenantId).IsRequired().HasMaxLength(60);
        b.Property(x => x.AnonymityToken).HasMaxLength(128);
        b.Property(x => x.StrengthsNarrative).IsRequired().HasMaxLength(4000);
        b.Property(x => x.GrowthAreasNarrative).IsRequired().HasMaxLength(4000);
        b.Property(x => x.OverallComment).HasMaxLength(2000);
        b.Property(x => x.Rating).HasPrecision(4, 2);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
        b.HasIndex(x => new { x.TenantId, x.SubjectEmployeeId, x.Status });
    }
}
