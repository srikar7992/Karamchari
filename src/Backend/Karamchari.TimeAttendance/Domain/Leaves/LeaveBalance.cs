namespace Karamchari.TimeAttendance.Domain.Leaves;

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Aggregate root that manages an employee's leave balance for a specific policy.
/// Business truth is derived from the immutable ledger entries.
/// </summary>
public sealed class LeaveBalance : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<LeaveBalanceEntry> _entries = [];

    private LeaveBalance(Guid id, Guid employeeId, Guid policyId) : base(id)
    {
        EmployeeId = employeeId;
        PolicyId = policyId;
        Status = BalanceStatus.Active;
    }

    private LeaveBalance()
    {
        TenantId = string.Empty;
    }

    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public Guid PolicyId { get; private set; }
    public BalanceStatus Status { get; private set; }

    /// <summary>
    /// Derived balance calculated from ledger entries.
    /// </summary>
    public decimal AvailableBalance => _entries.Sum(e => e.Quantity);

    public IReadOnlyCollection<LeaveBalanceEntry> Entries => _entries.AsReadOnly();

    public static LeaveBalance Create(Guid employeeId, Guid policyId)
    {
        return new LeaveBalance(Guid.NewGuid(), employeeId, policyId);
    }

    /// <summary>
    /// Adds a ledger entry to adjust the balance.
    /// </summary>
    public void AddEntry(LeaveBalanceEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.EmployeeId != EmployeeId || entry.PolicyId != PolicyId)
            throw new InvalidOperationException("Ledger entry does not match this balance.");

        if (AvailableBalance + entry.Quantity < 0 && entry.Quantity < 0)
            throw new InvalidOperationException("Insufficient leave balance for this deduction.");

        _entries.Add(entry);
    }

    /// <summary>
    /// Accrues leave for the employee.
    /// </summary>
    public void Accrue(decimal quantity, DateOnly effectiveDate, string? remarks = null, string? correlationId = null)
    {
        var entry = LeaveBalanceEntry.Create(
            EmployeeId, PolicyId, LeaveBalanceEntryType.Accrual, quantity,
            effectiveDate, remarks: remarks, correlationId: correlationId);

        AddEntry(entry);
    }

    /// <summary>
    /// Consumes leave.
    /// </summary>
    public void Consume(decimal quantity, DateOnly effectiveDate, string sourceReference, string? correlationId = null)
    {
        var entry = LeaveBalanceEntry.Create(
            EmployeeId, PolicyId, LeaveBalanceEntryType.Consumption, -Math.Abs(quantity),
            effectiveDate, sourceReference: sourceReference, correlationId: correlationId);

        AddEntry(entry);
    }

    /// <summary>
    /// Restores balance from a cancelled leave.
    /// </summary>
    public void Restore(decimal quantity, DateOnly effectiveDate, string sourceReference, string? correlationId = null)
    {
        var entry = LeaveBalanceEntry.Create(
            EmployeeId, PolicyId, LeaveBalanceEntryType.CancellationRestore, Math.Abs(quantity),
            effectiveDate, sourceReference: sourceReference, correlationId: correlationId);

        AddEntry(entry);
    }
}

public enum BalanceStatus
{
    Active = 1,
    Inactive = 2
}
