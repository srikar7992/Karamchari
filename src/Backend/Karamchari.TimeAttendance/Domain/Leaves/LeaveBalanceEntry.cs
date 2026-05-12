using Karamchari.Core.Domain.Primitives;

namespace Karamchari.TimeAttendance.Domain.Leaves;

/// <summary>
/// An immutable entry in the leave balance ledger.
/// Represents a single addition or deduction to an employee's leave balance.
/// </summary>
public sealed class LeaveBalanceEntry : Entity<Guid>
{
    private LeaveBalanceEntry() { }

    private LeaveBalanceEntry(
        Guid id,
        Guid employeeId,
        Guid policyId,
        LeaveBalanceEntryType type,
        decimal quantity,
        DateOnly effectiveDate,
        string? sourceReference,
        string? remarks,
        string? correlationId) : base(id)
    {
        EmployeeId = employeeId;
        PolicyId = policyId;
        EntryType = type;
        Quantity = quantity;
        EffectiveDate = effectiveDate;
        SourceReference = sourceReference;
        Remarks = remarks;
        CorrelationId = correlationId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid EmployeeId { get; private set; }
    public Guid PolicyId { get; private set; }
    public LeaveBalanceEntryType EntryType { get; private set; }

    /// <summary>Positive for additions (accrual, restore), negative for deductions (consumption, expiry).</summary>
    public decimal Quantity { get; private set; }

    public DateOnly EffectiveDate { get; private set; }
    public string? SourceReference { get; private set; }
    public string? Remarks { get; private set; }
    public string? CorrelationId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public static LeaveBalanceEntry Create(
        Guid employeeId,
        Guid policyId,
        LeaveBalanceEntryType type,
        decimal quantity,
        DateOnly effectiveDate,
        string? sourceReference = null,
        string? remarks = null,
        string? correlationId = null)
    {
        return new LeaveBalanceEntry(
            Guid.NewGuid(), employeeId, policyId, type, quantity,
            effectiveDate, sourceReference, remarks, correlationId);
    }
}
