using Karamchari.Core.Domain.Primitives;

namespace Karamchari.Performance.Domain.Reviews;

/// <summary>
/// Individual response to one question within a ReviewSubmission.
/// RatingValue is set for Rating/Competency questions; TextValue for Text questions.
/// </summary>
public sealed class ReviewResponse : Entity<Guid>
{
    private ReviewResponse() { /* EF materialization */ }

    internal ReviewResponse(
        Guid id,
        Guid submissionId,
        Guid questionId,
        QuestionType questionType,
        decimal? ratingValue,
        string? textValue) : base(id)
    {
        SubmissionId = submissionId;
        QuestionId = questionId;
        QuestionType = questionType;
        RatingValue = ratingValue;
        TextValue = textValue;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid SubmissionId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid QuestionId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public QuestionType QuestionType { get; private set; }
    public decimal? RatingValue { get; private set; }
    public string? TextValue { get; private set; }

    internal static ReviewResponse ForRating(Guid submissionId, Guid questionId, decimal rating, QuestionType type)
        => new(Guid.NewGuid(), submissionId, questionId, type, rating, null);

    internal static ReviewResponse ForText(Guid submissionId, Guid questionId, string text)
        => new(Guid.NewGuid(), submissionId, questionId, QuestionType.Text, null, text?.Trim());
}
