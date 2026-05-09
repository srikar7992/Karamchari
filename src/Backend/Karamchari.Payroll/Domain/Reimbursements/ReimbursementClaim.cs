using Karamchari.Core.Domain.Primitives;
using Karamchari.Payroll.Domain.Reimbursements.Events;

namespace Karamchari.Payroll.Domain.Reimbursements;

/// <summary>
/// Aggregate for an employee reimbursement claim.
/// Supports partial approvals, clawback, and fraud flag escalation.
/// </summary>
public sealed class ReimbursementClaim : AggregateRoot<Guid>
{
    public Guid TenantId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string EmployeeName { get; private set; } = string.Empty;
    public ReimbursementCategory Category { get; private set; }
    public ReimbursementStatus Status { get; private set; }
    public ReimbursementTaxability Taxability { get; private set; }

    public string Description { get; private set; } = string.Empty;
    public decimal ClaimedAmount { get; private set; }
    public decimal ApprovedAmount { get; private set; }
    public decimal PolicyLimit { get; private set; }

    // Attachment evidence
    public string? AttachmentBlobPath { get; private set; }
    public string? AttachmentFileName { get; private set; }
    public string? AttachmentHash { get; private set; }  // SHA256 for duplicate detection

    // Date of expense
    public DateOnly ExpenseDate { get; private set; }

    // Payroll linkage
    public string? PayoutPeriodName { get; private set; }

    // Fraud indicators
    public FraudIndicatorLevel FraudIndicator { get; private set; }
    public string? FraudNote { get; private set; }

    // Clawback
    public bool IsClawedBack { get; private set; }
    public string? ClawbackReason { get; private set; }
    public DateTimeOffset? ClawbackAtUtc { get; private set; }

    public string SubmittedBy { get; private set; } = string.Empty;
    public string? ApprovedBy { get; private set; }
    public string? RejectedBy { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private ReimbursementClaim() { }

    public static ReimbursementClaim Submit(
        Guid tenantId,
        Guid employeeId,
        string employeeName,
        ReimbursementCategory category,
        string description,
        decimal claimedAmount,
        decimal policyLimit,
        DateOnly expenseDate,
        ReimbursementTaxability taxability,
        string submittedBy,
        string? attachmentBlobPath = null,
        string? attachmentFileName = null,
        string? attachmentHash = null)
    {
        var claim = new ReimbursementClaim
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            Category = category,
            Description = description,
            ClaimedAmount = claimedAmount,
            PolicyLimit = policyLimit,
            ExpenseDate = expenseDate,
            Taxability = taxability,
            AttachmentBlobPath = attachmentBlobPath,
            AttachmentFileName = attachmentFileName,
            AttachmentHash = attachmentHash,
            Status = ReimbursementStatus.Submitted,
            FraudIndicator = FraudIndicatorLevel.None,
            SubmittedBy = submittedBy,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        claim.RaiseDomainEvent(new ReimbursementSubmittedEvent(
            claim.Id, tenantId, employeeId, category, claimedAmount));

        return claim;
    }

    public void FlagFraud(FraudIndicatorLevel level, string note)
    {
        FraudIndicator = level;
        FraudNote = note;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

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

    public void Reject(string rejectedBy, string reason)
    {
        if (Status is not (ReimbursementStatus.Submitted or ReimbursementStatus.PendingApproval))
            throw new InvalidOperationException($"Cannot reject claim in status {Status}.");

        Status = ReimbursementStatus.Rejected;
        RejectedBy = rejectedBy;
        RejectionReason = reason;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void MarkPaidOut(string payoutPeriodName)
    {
        if (Status is not (ReimbursementStatus.Approved or ReimbursementStatus.PartiallyApproved))
            throw new InvalidOperationException($"Cannot mark paid out in status {Status}.");

        Status = ReimbursementStatus.PaidOut;
        PayoutPeriodName = payoutPeriodName;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

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
