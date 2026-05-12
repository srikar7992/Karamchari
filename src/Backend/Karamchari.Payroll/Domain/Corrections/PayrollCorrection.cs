using Karamchari.Core.Domain.Primitives;
using Karamchari.Payroll.Domain.Corrections.Events;

namespace Karamchari.Payroll.Domain.Corrections;

/// <summary>
/// Aggregate for payroll corrections â€” retroactive or same-cycle.
/// Idempotency key prevents duplicate corrections for same employee+period+type.
/// </summary>
public sealed class PayrollCorrection : AggregateRoot<Guid>
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
    public CorrectionType Type { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public CorrectionScope Scope { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public CorrectionStatus Status { get; private set; }

    // Affected period
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string AffectedPeriodName { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int AffectedYear { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int AffectedMonth { get; private set; }

    // What changed
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string ChangeDescription { get; private set; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string ChangeDetails { get; private set; } = string.Empty;  // JSON payload with before/after

    // Result
    public decimal? DifferentialAmount { get; private set; }
    public string? LinkedArrearId { get; private set; }

    // Flags for post-disbursement / post-filing scenarios
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool AfterBankDisbursement { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool AfterTaxFiling { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public bool AfterEmployeeExit { get; private set; }

    // Idempotency â€” prevents duplicate corrections same employee+period+type
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string IdempotencyKey { get; private set; } = string.Empty;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string RequestedBy { get; private set; } = string.Empty;
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
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

    private PayrollCorrection() { }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static PayrollCorrection Create(
        string tenantId,
        Guid employeeId,
        string employeeName,
        CorrectionType type,
        CorrectionScope scope,
        string affectedPeriodName,
        int affectedYear,
        int affectedMonth,
        string changeDescription,
        string changeDetails,
        bool afterBankDisbursement,
        bool afterTaxFiling,
        bool afterEmployeeExit,
        string requestedBy)
    {
        var idempotencyKey = $"{tenantId}:{employeeId}:{affectedYear}:{affectedMonth}:{type}";

        var correction = new PayrollCorrection
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            EmployeeName = employeeName,
            Type = type,
            Scope = scope,
            AffectedPeriodName = affectedPeriodName,
            AffectedYear = affectedYear,
            AffectedMonth = affectedMonth,
            ChangeDescription = changeDescription,
            ChangeDetails = changeDetails,
            AfterBankDisbursement = afterBankDisbursement,
            AfterTaxFiling = afterTaxFiling,
            AfterEmployeeExit = afterEmployeeExit,
            IdempotencyKey = idempotencyKey,
            RequestedBy = requestedBy,
            Status = CorrectionStatus.Draft,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        correction.RaiseDomainEvent(new CorrectionCreatedEvent(
            correction.Id, tenantId, employeeId, type, affectedPeriodName));

        return correction;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void SubmitForApproval()
    {
        if (Status != CorrectionStatus.Draft)
            throw new InvalidOperationException($"Cannot submit correction in status {Status}.");

        Status = CorrectionStatus.Submitted;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new CorrectionApprovalRequestedEvent(Id, TenantId, EmployeeId, Type, AffectedPeriodName));
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Approve(string approvedBy)
    {
        if (Status != CorrectionStatus.Submitted)
            throw new InvalidOperationException($"Cannot approve correction in status {Status}.");

        Status = CorrectionStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new CorrectionApprovedEvent(Id, TenantId, EmployeeId, Type, AffectedPeriodName, approvedBy));
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Reject(string rejectedBy, string reason)
    {
        if (Status != CorrectionStatus.Submitted)
            throw new InvalidOperationException($"Cannot reject correction in status {Status}.");

        Status = CorrectionStatus.Rejected;
        RejectedBy = rejectedBy;
        RejectionReason = reason;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void StartRecalculation()
    {
        if (Status != CorrectionStatus.Approved)
            throw new InvalidOperationException($"Cannot start recalculation in status {Status}.");

        Status = CorrectionStatus.RecalculationInProgress;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void MarkProcessed(decimal differentialAmount, string? linkedArrearId)
    {
        if (Status != CorrectionStatus.RecalculationInProgress)
            throw new InvalidOperationException($"Cannot mark processed in status {Status}.");

        Status = CorrectionStatus.Processed;
        DifferentialAmount = differentialAmount;
        LinkedArrearId = linkedArrearId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new CorrectionProcessedEvent(Id, TenantId, EmployeeId, differentialAmount));
    }
}
