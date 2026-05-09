using Karamchari.Core.Domain.Primitives;
using Karamchari.Payroll.Domain.Reimbursements.Events;

namespace Karamchari.Payroll.Domain.Reimbursements;

/// <summary>
/// Aggregate for an employee reimbursement claim.
/// Supports partial approvals, clawback, and fraud flag escalation.
/// </summary>
public sealed class ReimbursementClaim : AggregateRoot<Guid>
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EmployeeId { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string EmployeeName { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public ReimbursementCategory Category { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public ReimbursementStatus Status { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public ReimbursementTaxability Taxability { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Description { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal ClaimedAmount { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal ApprovedAmount { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal PolicyLimit { get; private set; }

    // Attachment evidence
    public string? AttachmentBlobPath { get; private set; }
    public string? AttachmentFileName { get; private set; }
    public string? AttachmentHash { get; private set; }  // SHA256 for duplicate detection

    // Date of expense
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly ExpenseDate { get; private set; }

    // Payroll linkage
    public string? PayoutPeriodName { get; private set; }

    // Fraud indicators
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public FraudIndicatorLevel FraudIndicator { get; private set; }
    public string? FraudNote { get; private set; }

    // Clawback
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool IsClawedBack { get; private set; }
    public string? ClawbackReason { get; private set; }
    public DateTimeOffset? ClawbackAtUtc { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string SubmittedBy { get; private set; } = string.Empty;
    public string? ApprovedBy { get; private set; }
    public string? RejectedBy { get; private set; }
    public string? RejectionReason { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    private ReimbursementClaim() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static ReimbursementClaim SubmitWithPolicy(
        string tenantId,
        Guid employeeId,
        string employeeName,
        ReimbursementCategory category,
        string description,
        decimal claimedAmount,
        DateOnly expenseDate,
        string submittedBy,
        IEnumerable<string?> existingHashesForEmployee,
        string? attachmentBlobPath = null,
        string? attachmentFileName = null,
        string? attachmentHash = null)
    {
        // 1. Evaluate policy (Domain Rule)
        var policyCheck = Karamchari.Payroll.Services.Reimbursements.ReimbursementPolicyEngine.Evaluate(
            category, claimedAmount, attachmentHash, existingHashesForEmployee);

        if (!policyCheck.IsAllowed)
            throw new InvalidOperationException($"Policy violation: {policyCheck.ViolationReason}");

        // 2. Create claim
        var claim = new ReimbursementClaim
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            Category = category,
            Description = description,
            ClaimedAmount = claimedAmount,
            PolicyLimit = policyCheck.PolicyLimit,
            ExpenseDate = expenseDate,
            Taxability = policyCheck.Taxability,
            AttachmentBlobPath = attachmentBlobPath,
            AttachmentFileName = attachmentFileName,
            AttachmentHash = attachmentHash,
            Status = ReimbursementStatus.Submitted,
            FraudIndicator = policyCheck.FraudIndicator,
            SubmittedBy = submittedBy,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        if (policyCheck.FraudIndicator != FraudIndicatorLevel.None)
            claim.FraudNote = "Automated fraud detection";

        claim.RaiseDomainEvent(new ReimbursementSubmittedEvent(
            claim.Id, tenantId, employeeId, category, claimedAmount));

        return claim;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void FlagFraud(FraudIndicatorLevel level, string note)
    {
        FraudIndicator = level;
        FraudNote = note;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Approve(string approvedBy, decimal? approvedAmount = null)
    {
        if (Status is not (ReimbursementStatus.Submitted or ReimbursementStatus.PendingApproval))
            throw new InvalidOperationException($"Cannot approve claim in status {Status}.");

        var amount = approvedAmount ?? ClaimedAmount;

        if (amount > PolicyLimit)
            throw new InvalidOperationException($"Approved amount {amount} exceeds policy limit {PolicyLimit}.");

        ApprovedAmount = amount;
        ApprovedBy = approvedBy;
        Status = amount < ClaimedAmount
            ? ReimbursementStatus.PartiallyApproved
            : ReimbursementStatus.Approved;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new ReimbursementApprovedEvent(Id, TenantId, EmployeeId, ApprovedAmount, approvedBy));
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Reject(string rejectedBy, string reason)
    {
        if (Status is not (ReimbursementStatus.Submitted or ReimbursementStatus.PendingApproval))
            throw new InvalidOperationException($"Cannot reject claim in status {Status}.");

        Status = ReimbursementStatus.Rejected;
        RejectedBy = rejectedBy;
        RejectionReason = reason;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void MarkPaidOut(string payoutPeriodName)
    {
        if (Status is not (ReimbursementStatus.Approved or ReimbursementStatus.PartiallyApproved))
            throw new InvalidOperationException($"Cannot mark paid out in status {Status}.");

        Status = ReimbursementStatus.PaidOut;
        PayoutPeriodName = payoutPeriodName;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Clawback(string reason)
    {
        if (Status != ReimbursementStatus.PaidOut)
            throw new InvalidOperationException("Only paid-out claims can be clawed back.");

        IsClawedBack = true;
        ClawbackReason = reason;
        ClawbackAtUtc = DateTimeOffset.UtcNow;
        Status = ReimbursementStatus.Clawback;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new ReimbursementClawedBackEvent(Id, TenantId, EmployeeId, ApprovedAmount, reason));
    }
}
