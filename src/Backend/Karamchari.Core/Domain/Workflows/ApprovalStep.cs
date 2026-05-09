namespace Karamchari.Core.Domain.Workflows;

public enum ApprovalStatus { Pending, Approved, Rejected, Skipped }

/// <summary>
/// Value object representing a single step in an approval chain.
/// Tracks who approved, when, and any comments.
/// </summary>
public sealed class ApprovalStep
{
    public int Sequence { get; private set; }
    public string Role { get; private set; } = string.Empty;
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public ApprovalStatus Status { get; private set; }
    public string? Note { get; private set; }

    private ApprovalStep() { }

    public static ApprovalStep Create(int sequence, string role)
    {
        return new ApprovalStep
        {
            Sequence = sequence,
            Role = role,
            Status = ApprovalStatus.Pending
        };
    }

    public void Approve(string userId, string? note = null)
    {
        if (Status != ApprovalStatus.Pending)
            throw new InvalidOperationException($"Step {Sequence} is already {Status}.");

        ApprovedBy = userId;
        ApprovedAtUtc = DateTimeOffset.UtcNow;
        Status = ApprovalStatus.Approved;
        Note = note;
    }

    public void Reject(string userId, string reason)
    {
        if (Status != ApprovalStatus.Pending)
            throw new InvalidOperationException($"Step {Sequence} is already {Status}.");

        ApprovedBy = userId;
        ApprovedAtUtc = DateTimeOffset.UtcNow;
        Status = ApprovalStatus.Rejected;
        Note = reason;
    }
}
