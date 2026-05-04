namespace Karamchari.Payroll.Domain.Statutory;

/// <summary>
/// Defines the lifecycle states of an investment proof verification.
/// </summary>
public enum VerificationStatus
{
    /// <summary>User has saved the record but not yet submitted for HR review.</summary>
    Draft,

    /// <summary>The proof has been submitted by the employee.</summary>
    Submitted,

    /// <summary>The proof is currently being reviewed by HR (assigned or in queue).</summary>
    PendingReview,

    /// <summary>HR has verified the proof and approved the amount.</summary>
    Approved,

    /// <summary>HR has rejected the proof (e.g. invalid document, wrong date).</summary>
    Rejected,

    /// <summary>The declaration has been replaced by a newer version.</summary>
    Superseded
}
