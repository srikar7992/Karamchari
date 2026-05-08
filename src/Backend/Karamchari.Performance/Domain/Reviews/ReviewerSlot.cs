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
    public static ReviewerSlot Create(Guid reviewerId, ReviewerRole role)
        => new(reviewerId, role, false, null);

    public ReviewerSlot MarkCompleted()
        => this with { IsCompleted = true, CompletedOnUtc = DateTimeOffset.UtcNow };
}
