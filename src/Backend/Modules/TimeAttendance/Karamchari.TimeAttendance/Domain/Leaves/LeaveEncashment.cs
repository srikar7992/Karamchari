namespace Karamchari.TimeAttendance.Domain.Leaves;

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Records an encashment request against an employee's leave balance.
/// Triggers a ledger debit of type Encashment and payroll credit.
/// </summary>
public sealed class LeaveEncashment : AggregateRoot<Guid>, ITenantOwned
{
    private LeaveEncashment()
    {
        TenantId = string.Empty;
    }

    private LeaveEncashment(Guid id, Guid employeeId, Guid policyId, decimal days,
        string requestedBy) : base(id)
    {
        EmployeeId = employeeId;
        PolicyId = policyId;
        Days = days;
        RequestedBy = requestedBy;
        Status = EncashmentStatus.Pending;
        RequestedAtUtc = DateTimeOffset.UtcNow;
    }

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public Guid PolicyId { get; private set; }
    public decimal Days { get; private set; }
    public EncashmentStatus Status { get; private set; }
    public string RequestedBy { get; private set; } = string.Empty;
    public string? ApprovedBy { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset RequestedAtUtc { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    /// <summary>Reference to the payroll run that paid this out.</summary>
    public string? PayrollRunReference { get; private set; }

    public static LeaveEncashment Create(Guid employeeId, Guid policyId, decimal days, string requestedBy)
    {
        if (days <= 0) throw new ArgumentOutOfRangeException(nameof(days), "Encashment days must be positive.");
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedBy);
        return new LeaveEncashment(Guid.NewGuid(), employeeId, policyId, days, requestedBy);
    }

    public void Approve(string approvedBy)
    {
        if (Status != EncashmentStatus.Pending)
            throw new InvalidOperationException("Only pending encashments can be approved.");
        ApprovedBy = approvedBy;
        Status = EncashmentStatus.Approved;
    }

    public void Reject(string reason)
    {
        if (Status != EncashmentStatus.Pending)
            throw new InvalidOperationException("Only pending encashments can be rejected.");
        RejectionReason = reason;
        Status = EncashmentStatus.Rejected;
    }

    public void MarkProcessed(string payrollRunReference)
    {
        if (Status != EncashmentStatus.Approved)
            throw new InvalidOperationException("Only approved encashments can be marked processed.");
        PayrollRunReference = payrollRunReference;
        ProcessedAtUtc = DateTimeOffset.UtcNow;
        Status = EncashmentStatus.Processed;
    }
}

public enum EncashmentStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Processed = 4
}
