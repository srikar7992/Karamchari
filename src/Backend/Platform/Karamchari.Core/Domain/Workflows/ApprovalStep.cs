// -----------------------------------------------------------------------
// <copyright file="ApprovalStep.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Core.Domain.Workflows;

/// <summary>
/// Status of an approval step.
/// </summary>
public enum ApprovalStatus
{
    /// <summary>Pending approval.</summary>
    Pending,
    /// <summary>Approved.</summary>
    Approved,
    /// <summary>Rejected.</summary>
    Rejected,
    /// <summary>Skipped in workflow.</summary>
    Skipped
}

/// <summary>
/// Value object representing a single step in an approval chain.
/// Tracks who approved, when, and any comments.
/// </summary>
public sealed class ApprovalStep
{
    /// <summary>The sequence number of this step.</summary>
    public int Sequence { get; private set; }
    /// <summary>The role required to approve.</summary>
    public string Role { get; private set; } = string.Empty;
    /// <summary>The user who approved.</summary>
    public string? ApprovedBy { get; private set; }
    /// <summary>The time of approval.</summary>
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    /// <summary>The current status.</summary>
    public ApprovalStatus Status { get; private set; }
    /// <summary>Any note or comment.</summary>
    public string? Note { get; private set; }

    private ApprovalStep() { }

    /// <summary>Creates a new approval step.</summary>
    public static ApprovalStep Create(int sequence, string role)
    {
        return new ApprovalStep
        {
            Sequence = sequence,
            Role = role,
            Status = ApprovalStatus.Pending
        };
    }

    /// <summary>Approves the step.</summary>
    public void Approve(string userId, string? note = null)
    {
        if (Status != ApprovalStatus.Pending)
            throw new InvalidOperationException($"Step {Sequence} is already {Status}.");

        ApprovedBy = userId;
        ApprovedAtUtc = DateTimeOffset.UtcNow;
        Status = ApprovalStatus.Approved;
        Note = note;
    }

    /// <summary>Rejects the step.</summary>
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
