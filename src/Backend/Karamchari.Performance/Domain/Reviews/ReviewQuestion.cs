namespace Karamchari.Performance.Domain.Reviews;

/// <summary>
/// Value object — part of ReviewSection. Stored as JSON column on ReviewSection.
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
            throw new ArgumentOutOfRangeException(nameof(weight), "Question weight must be 0–1.");
        if (minRating >= maxRating)
            throw new ArgumentException("MinRating must be less than MaxRating.");

        return new ReviewQuestion(Guid.NewGuid(), text.Trim(), type, weight,
            minRating, maxRating, competencyCode?.Trim(), isRequired);
    }
}
