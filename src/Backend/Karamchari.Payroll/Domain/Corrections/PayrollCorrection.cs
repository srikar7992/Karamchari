using Karamchari.Core.Domain.Primitives;
using Karamchari.Payroll.Domain.Corrections.Events;

namespace Karamchari.Payroll.Domain.Corrections;

/// <summary>
/// Aggregate for payroll corrections — retroactive or same-cycle.
/// Idempotency key prevents duplicate corrections for same employee+period+type.
/// </summary>
public sealed class PayrollCorrection : AggregateRoot<Guid>
{
    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public string EmployeeName { get; private set; } = string.Empty;
    public CorrectionType Type { get; private set; }
    public CorrectionScope Scope { get; private set; }
    public CorrectionStatus Status { get; private set; }

    // Affected period
    public string AffectedPeriodName { get; private set; } = string.Empty;
    public int AffectedYear { get; private set; }
    public int AffectedMonth { get; private set; }

    // What changed
    public string ChangeDescription { get; private set; } = string.Empty;
    public string ChangeDetails { get; private set; } = string.Empty;  // JSON payload with before/after

    // Result
    public decimal? DifferentialAmount { get; private set; }
    public string? LinkedArrearId { get; private set; }

    // Flags for post-disbursement / post-filing scenarios
    public bool AfterBankDisbursement { get; private set; }
    public bool AfterTaxFiling { get; private set; }
    public bool AfterEmployeeExit { get; private set; }

    // Idempotency — prevents duplicate corrections same employee+period+type
    public string IdempotencyKey { get; private set; } = string.Empty;

    public string RequestedBy { get; private set; } = string.Empty;
    public string? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAtUtc { get; private set; }
    public string? RejectedBy { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private PayrollCorrection() { }

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

    public void SubmitForApproval()
    {
        if (Status != CorrectionStatus.Draft)
            throw new InvalidOperationException($"Cannot submit correction in status {Status}.");

        Status = CorrectionStatus.PendingApproval;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new CorrectionApprovalRequestedEvent(Id, TenantId, EmployeeId, Type, AffectedPeriodName));
    }

    public void Approve(string approvedBy)
    {
        if (Status != CorrectionStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot approve correction in status {Status}.");

        Status = CorrectionStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new CorrectionApprovedEvent(Id, TenantId, EmployeeId, Type, AffectedPeriodName, approvedBy));
    }

    public void Reject(string rejectedBy, string reason)
    {
        if (Status != CorrectionStatus.PendingApproval)
            throw new InvalidOperationException($"Cannot reject correction in status {Status}.");

        Status = CorrectionStatus.Rejected;
        RejectedBy = rejectedBy;
        RejectionReason = reason;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void StartRecalculation()
    {
        if (Status != CorrectionStatus.Approved)
            throw new InvalidOperationException($"Cannot start recalculation in status {Status}.");

        Status = CorrectionStatus.RecalculationInProgress;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

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
