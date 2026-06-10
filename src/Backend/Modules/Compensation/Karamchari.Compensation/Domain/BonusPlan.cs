// -----------------------------------------------------------------------
// <copyright file="BonusPlan.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

namespace Karamchari.Compensation.Domain;

public enum BonusPlanType { Discretionary, PerformanceBased, Commission, Retention, SignOn }
public enum BonusAllocationStatus { Draft, Approved, Paid, Cancelled }

/// <summary>
/// Variable pay plan for a cycle year. Manages bonus allocations per employee.
/// Budget enforcement: total allocated cannot exceed TotalBudget.
/// Performance-linked: allocation amount may reference a performance rating.
/// </summary>
public sealed class BonusPlan : AggregateRoot<Guid>, ITenantOwned
{
    private readonly List<BonusAllocation> _allocations = [];
    private BonusPlan() { /* EF */ }

    private BonusPlan(Guid id, string tenantId, string name, BonusPlanType type,
        int cycleYear, decimal totalBudget, string currencyCode) : base(id)
    {
        TenantId = tenantId;
        Name = name;
        Type = type;
        CycleYear = cycleYear;
        TotalBudget = totalBudget;
        CurrencyCode = currencyCode;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public BonusPlanType Type { get; private set; }
    public int CycleYear { get; private set; }
    public decimal TotalBudget { get; private set; }
    public decimal AllocatedBudget => _allocations
        .Where(a => a.Status != BonusAllocationStatus.Cancelled)
        .Sum(a => a.TargetAmount);
    public decimal RemainingBudget => TotalBudget - AllocatedBudget;
    public string CurrencyCode { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyList<BonusAllocation> Allocations => _allocations.AsReadOnly();

    public static BonusPlan Create(string tenantId, string name, BonusPlanType type,
        int cycleYear, decimal totalBudget, string currencyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalBudget);
        if (cycleYear < 2020 || cycleYear > 2100) throw new ArgumentOutOfRangeException(nameof(cycleYear));

        return new BonusPlan(Guid.NewGuid(), tenantId, name.Trim(), type, cycleYear,
            totalBudget, currencyCode.ToUpperInvariant());
    }

    /// <summary>
    /// Adds a bonus allocation for an employee. Validates against remaining budget.
    /// </summary>
    public BonusAllocation Allocate(Guid employeeId, decimal targetAmount, string? notes = null)
    {
        if (!IsActive) throw new InvalidOperationException("Cannot allocate to an inactive plan.");
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetAmount);
        if (targetAmount > RemainingBudget)
            throw new InvalidOperationException(
                $"Allocation of {targetAmount} {CurrencyCode} exceeds remaining budget of {RemainingBudget} {CurrencyCode}.");

        if (_allocations.Any(a => a.EmployeeId == employeeId && a.Status == BonusAllocationStatus.Draft))
            throw new InvalidOperationException("Employee already has a pending allocation in this plan.");

        var alloc = new BonusAllocation(Guid.NewGuid(), Id, employeeId, targetAmount, notes);
        _allocations.Add(alloc);
        return alloc;
    }

    public void ApprovePlan()
    {
        if (_allocations.Count == 0) throw new InvalidOperationException("Plan has no allocations to approve.");
        foreach (var alloc in _allocations.Where(a => a.Status == BonusAllocationStatus.Draft))
            alloc.Approve();
    }

    public void MarkPaid(Guid employeeId, decimal actualAmount)
    {
        var alloc = _allocations.FirstOrDefault(a => a.EmployeeId == employeeId && a.Status == BonusAllocationStatus.Approved)
            ?? throw new InvalidOperationException("No approved allocation for this employee.");
        alloc.MarkPaid(actualAmount);
    }

    public void CancelAllocation(Guid employeeId)
    {
        var alloc = _allocations.FirstOrDefault(a => a.EmployeeId == employeeId && a.Status != BonusAllocationStatus.Paid)
            ?? throw new InvalidOperationException("No cancellable allocation for this employee.");
        alloc.Cancel();
    }

    public void Deactivate() => IsActive = false;
}

/// <summary>Individual bonus allocation within a plan for one employee.</summary>
public sealed class BonusAllocation : Entity<Guid>
{
    private BonusAllocation() { /* EF */ }

    internal BonusAllocation(Guid id, Guid planId, Guid employeeId,
        decimal targetAmount, string? notes) : base(id)
    {
        PlanId = planId;
        EmployeeId = employeeId;
        TargetAmount = targetAmount;
        Notes = notes;
        Status = BonusAllocationStatus.Draft;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid PlanId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public decimal TargetAmount { get; private set; }
    public decimal? ActualPaidAmount { get; private set; }
    public string? Notes { get; private set; }
    public BonusAllocationStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? PaidAt { get; private set; }

    internal void Approve() => Status = BonusAllocationStatus.Approved;

    internal void MarkPaid(decimal actualAmount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(actualAmount);
        ActualPaidAmount = actualAmount;
        Status = BonusAllocationStatus.Paid;
        PaidAt = DateTimeOffset.UtcNow;
    }

    internal void Cancel() => Status = BonusAllocationStatus.Cancelled;
}
