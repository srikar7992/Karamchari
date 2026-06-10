// -----------------------------------------------------------------------
// <copyright file="ReviewSection.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Performance.Domain.Reviews;

/// <summary>
/// Value object â€” owned by ReviewTemplate. Stored as JSON column.
/// Section weights across all sections in a template must sum to 1.0 (enforced at template level).
/// </summary>
public sealed record ReviewSection(
    Guid SectionId,
    string Title,
    decimal Weight,
    int DisplayOrder,
    IReadOnlyList<ReviewQuestion> Questions)
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static ReviewSection Create(
        string title,
        decimal weight,
        int displayOrder,
        IReadOnlyList<ReviewQuestion> questions)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        if (weight <= 0 || weight > 1)
            throw new ArgumentOutOfRangeException(nameof(weight), "Section weight must be > 0 and â‰¤ 1.");
        if (questions == null || questions.Count == 0)
            throw new ArgumentException("Section must have at least one question.");

        return new ReviewSection(Guid.NewGuid(), title.Trim(), weight, displayOrder,
            questions.ToList().AsReadOnly());
    }
}
