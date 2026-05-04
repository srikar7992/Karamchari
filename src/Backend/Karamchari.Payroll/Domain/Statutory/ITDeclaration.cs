namespace Karamchari.Payroll.Domain.Statutory;

using Karamchari.Core.Multitenancy;

/// <summary>
/// Represents a specific investment declaration or proof submission.
/// </summary>
public class ITDeclaration : ITenantOwned
{
    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public int FinancialYear { get; private set; }

    /// <summary>The version of this specific declaration (increments on re-upload).</summary>
    public int Version { get; private set; }
    public Guid? PreviousVersionId { get; private set; }

    public VerificationStatus Status { get; private set; }

    /// <summary>The category of the declaration (e.g., "80C", "80D", "HRA").</summary>
    public string Category { get; private set; } = string.Empty;

    /// <summary>The amount claimed by the employee.</summary>
    public decimal ClaimedAmount { get; private set; }

    /// <summary>The amount approved by HR after verification.</summary>
    public decimal? ApprovedAmount { get; private set; }

    /// <summary>Storage URI of the proof document.</summary>
    public string ProofUri { get; private set; } = string.Empty;

    public string? RejectionReason { get; private set; }

    public string SubmittedBy { get; private set; } = string.Empty;
    public string? VerifiedBy { get; private set; }

    public DateTime SubmittedAt { get; private set; }
    public DateTime? VerifiedAt { get; private set; }

    /// <summary>Optimistic concurrency token.</summary>
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private ITDeclaration() { }

    public static ITDeclaration Create(
        Guid employeeId, 
        int financialYear, 
        string category, 
        decimal amount, 
        string proofUri, 
        string submittedBy)
    {
        return new ITDeclaration
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            FinancialYear = financialYear,
            Category = category,
            ClaimedAmount = amount,
            ProofUri = proofUri,
            Status = VerificationStatus.PendingReview,
            SubmittedBy = submittedBy,
            SubmittedAt = DateTime.UtcNow,
            Version = 1
        };
    }

    public void Approve(decimal amount, string approver)
    {
        if (Status != VerificationStatus.PendingReview && Status != VerificationStatus.Submitted)
            throw new InvalidOperationException($"Cannot approve declaration in {Status} state.");

        ApprovedAmount = amount;
        Status = VerificationStatus.Approved;
        VerifiedBy = approver;
        VerifiedAt = DateTime.UtcNow;
    }

    public void Reject(string reason, string rejectedBy)
    {
        if (Status != VerificationStatus.PendingReview && Status != VerificationStatus.Submitted)
            throw new InvalidOperationException($"Cannot reject declaration in {Status} state.");

        Status = VerificationStatus.Rejected;
        RejectionReason = reason;
        VerifiedBy = rejectedBy;
        VerifiedAt = DateTime.UtcNow;
    }

    public void Supersede()
    {
        Status = VerificationStatus.Superseded;
    }
}
