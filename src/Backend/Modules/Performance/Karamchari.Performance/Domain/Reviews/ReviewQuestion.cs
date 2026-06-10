// -----------------------------------------------------------------------
// <copyright file="ReviewQuestion.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Performance.Domain.Reviews;

/// <summary>
/// Value object â€” part of ReviewSection. Stored as JSON column on ReviewSection.
/// QuestionId is stable across template versions for analytics correlation.
/// </summary>
public sealed record ReviewQuestion(
    Guid QuestionId,
    string Text,
    QuestionType Type,
    decimal Weight,
    int MinRating,
    int MaxRating,
    string? CompetencyCode,
    bool IsRequired)
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static ReviewQuestion Create(
        string text,
        QuestionType type,
        decimal weight,
        int minRating = 1,
        int maxRating = 5,
        string? competencyCode = null,
        bool isRequired = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        if (weight < 0 || weight > 1)
            throw new ArgumentOutOfRangeException(nameof(weight), "Question weight must be 0â€“1.");
        if (minRating >= maxRating)
            throw new ArgumentException("MinRating must be less than MaxRating.");

        return new ReviewQuestion(Guid.NewGuid(), text.Trim(), type, weight,
            minRating, maxRating, competencyCode?.Trim(), isRequired);
    }
}
