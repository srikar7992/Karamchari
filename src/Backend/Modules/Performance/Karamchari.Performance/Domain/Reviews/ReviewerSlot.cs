namespace Karamchari.Performance.Domain.Reviews;

/// <summary>
/// Value object representing a nominated reviewer for an assignment.
/// Stored as JSON collection on ReviewAssignment.
/// </summary>
public sealed record ReviewerSlot(
    Guid ReviewerId,
    ReviewerRole Role,
    bool IsCompleted,
    DateTimeOffset? CompletedOnUtc)
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static ReviewerSlot Create(Guid reviewerId, ReviewerRole role)
        => new(reviewerId, role, false, null);

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public ReviewerSlot MarkCompleted()
        => this with { IsCompleted = true, CompletedOnUtc = DateTimeOffset.UtcNow };
}
