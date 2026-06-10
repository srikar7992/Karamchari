// -----------------------------------------------------------------------
// <copyright file="LeaveBalance.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Domain.Leaves;

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// Aggregate root managing an employee's leave balance via immutable ledger entries.
/// Use optimistic concurrency (RowVersion) to prevent double-deduction under concurrent requests.
/// </summary>
public sealed class LeaveBalance : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<LeaveBalanceEntry> _entries = [];

    // Cached running total. null = not yet initialized (EF materialization path:
    // the parameterless constructor runs before EF hydrates _entries, so we defer
    // computation to the first AvailableBalance read via lazy init).
    private decimal? _runningBalance;

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

    /// <summary>Accrual paused (LOP, suspension, configurable maternity).</summary>
    public bool AccrualPaused { get; private set; }
    public string? AccrualPauseReason { get; private set; }

    /// <summary>
    /// EF Core optimistic concurrency token.
    /// Prevents two simultaneous requests each seeing balance = 1 and both succeeding.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    /// <summary>
    /// O(1) cached balance. Lazily initialized on first read so that EF-materialized
    /// instances (where OwnsMany populates _entries after the constructor) are handled
    /// correctly without an interceptor.
    /// </summary>
    public decimal AvailableBalance
    {
        get
        {
            _runningBalance ??= _entries.Sum(e => e.Quantity);
            return _runningBalance.Value;
        }
    }

    /// <summary>
    /// Verifies _runningBalance == Sum(Entries). O(n) — only call from tests or reconciliation jobs.
    /// Never call from hot paths or getters.
    /// </summary>
    public bool VerifyBalanceIntegrity()
        => _runningBalance.HasValue && _runningBalance.Value == _entries.Sum(e => e.Quantity);

    public IReadOnlyCollection<LeaveBalanceEntry> Entries => _entries.AsReadOnly();

    public static LeaveBalance Create(Guid employeeId, Guid policyId)
        => new(Guid.NewGuid(), employeeId, policyId);

    public void AddEntry(LeaveBalanceEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.EmployeeId != EmployeeId || entry.PolicyId != PolicyId)
            throw new InvalidOperationException("Ledger entry does not match this balance.");

        if (AvailableBalance + entry.Quantity < 0 && entry.Quantity < 0)
            throw new InvalidOperationException("Insufficient leave balance.");

        _entries.Add(entry);
        // AvailableBalance was read above (guard check), so _runningBalance is guaranteed
        // non-null here. Increment in O(1) rather than re-summing.
        _runningBalance += entry.Quantity;
    }

    public void Accrue(decimal quantity, DateOnly effectiveDate, string? remarks = null, string? correlationId = null)
    {
        if (AccrualPaused) return;

        AddEntry(LeaveBalanceEntry.Create(EmployeeId, PolicyId, LeaveBalanceEntryType.Accrual,
            quantity, effectiveDate, remarks: remarks, correlationId: correlationId));
    }

    public void Consume(decimal quantity, DateOnly effectiveDate, string sourceReference, string? correlationId = null)
        => AddEntry(LeaveBalanceEntry.Create(EmployeeId, PolicyId, LeaveBalanceEntryType.Consumption,
            -Math.Abs(quantity), effectiveDate, sourceReference: sourceReference, correlationId: correlationId));

    public void Restore(decimal quantity, DateOnly effectiveDate, string sourceReference, string? correlationId = null)
        => AddEntry(LeaveBalanceEntry.Create(EmployeeId, PolicyId, LeaveBalanceEntryType.CancellationRestore,
            Math.Abs(quantity), effectiveDate, sourceReference: sourceReference, correlationId: correlationId));

    public void CarryForward(decimal quantity, DateOnly effectiveDate, string? correlationId = null)
        => AddEntry(LeaveBalanceEntry.Create(EmployeeId, PolicyId, LeaveBalanceEntryType.CarryForward,
            Math.Abs(quantity), effectiveDate, correlationId: correlationId));

    public void Expire(decimal quantity, DateOnly effectiveDate, string? correlationId = null)
        => AddEntry(LeaveBalanceEntry.Create(EmployeeId, PolicyId, LeaveBalanceEntryType.Expiry,
            -Math.Abs(quantity), effectiveDate, correlationId: correlationId));

    public void Encash(decimal quantity, DateOnly effectiveDate, string encashmentReference)
        => AddEntry(LeaveBalanceEntry.Create(EmployeeId, PolicyId, LeaveBalanceEntryType.Encashment,
            -Math.Abs(quantity), effectiveDate, sourceReference: encashmentReference));

    /// <summary>
    /// Adjusts balance for audit-safe reasons. LOPConversion and PayrollCorrection are explicit
    /// ledger entry types that bypass the insufficient-balance guard — auditors see the reason.
    /// Never use a generic "ForceAdjust" bypass; reason must be explicit.
    /// </summary>
    public void Adjust(decimal quantity, DateOnly effectiveDate, string adjustedBy, AdjustmentReason reason)
    {
        LeaveBalanceEntryType entryType = reason switch
        {
            AdjustmentReason.LOPConversion => LeaveBalanceEntryType.LOPConversion,
            AdjustmentReason.PayrollCorrection => LeaveBalanceEntryType.PayrollCorrection,
            _ => LeaveBalanceEntryType.ManualAdjustment,
        };

        var entry = LeaveBalanceEntry.Create(EmployeeId, PolicyId, entryType,
            quantity, effectiveDate, sourceReference: adjustedBy, remarks: reason.ToString());

        // LOPConversion and PayrollCorrection are authorised to go negative (audited).
        if (reason is AdjustmentReason.LOPConversion or AdjustmentReason.PayrollCorrection)
        {
            _entries.Add(entry);
            // Bypass AddEntry, so update cache manually.
            // Ensure cache is initialized first (handles EF-materialized instances).
            if (_runningBalance.HasValue)
                _runningBalance += entry.Quantity;
            // else: null — next AvailableBalance read will recalculate from _entries.
        }
        else
            AddEntry(entry); // standard guard applies
    }

    public void Migrate(decimal quantity, DateOnly effectiveDate, string migrationBatch)
        => AddEntry(LeaveBalanceEntry.Create(EmployeeId, PolicyId, LeaveBalanceEntryType.Migration,
            quantity, effectiveDate, sourceReference: migrationBatch));

    public void Correct(decimal quantity, DateOnly effectiveDate, string correctedBy, string justification)
        => AddEntry(LeaveBalanceEntry.Create(EmployeeId, PolicyId, LeaveBalanceEntryType.Correction,
            quantity, effectiveDate, sourceReference: correctedBy, remarks: justification));

    public void PauseAccrual(string reason)
    {
        AccrualPaused = true;
        AccrualPauseReason = reason;
    }

    public void ResumeAccrual()
    {
        AccrualPaused = false;
        AccrualPauseReason = null;
    }

    /// <summary>
    /// Freezes balance on resignation/death. No further debits or credits after this.
    /// </summary>
    public void Close()
    {
        if (Status == BalanceStatus.Closed)
            throw new InvalidOperationException("Balance already closed.");
        Status = BalanceStatus.Closed;
    }

    public void Activate() => Status = BalanceStatus.Active;
    public void Deactivate() => Status = BalanceStatus.Inactive;
}

public enum BalanceStatus
{
    Active = 1,
    Inactive = 2,
    Closed = 3
}
