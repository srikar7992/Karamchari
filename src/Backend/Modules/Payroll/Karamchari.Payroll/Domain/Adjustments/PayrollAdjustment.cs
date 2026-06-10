// -----------------------------------------------------------------------
// <copyright file="PayrollAdjustment.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.Payroll.Domain.Adjustments.Events;

namespace Karamchari.Payroll.Domain.Adjustments;

/// <summary>
/// Manual payroll adjustment — credits or debits against an employee's payroll.
/// Applied in the next available payroll run.
/// </summary>
public sealed class PayrollAdjustment : AggregateRoot<Guid>, ITenantOwned
{
    public string TenantId { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public Guid? PayrollRunId { get; private set; }
    public decimal Amount { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public AdjustmentType Type { get; private set; }
    public AdjustmentStatus Status { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? AppliedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    private PayrollAdjustment() { }

    public static PayrollAdjustment Create(
        string tenantId,
        Guid employeeId,
        decimal amount,
        string reason,
        AdjustmentType type,
        string createdBy)
    {
        if (amount <= 0)
            throw new ArgumentException("Adjustment amount must be positive.");

        var adjustment = new PayrollAdjustment
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EmployeeId = employeeId,
            Amount = amount,
            Reason = reason,
            Type = type,
            Status = AdjustmentStatus.Pending,
            CreatedBy = createdBy,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        adjustment.RaiseDomainEvent(new PayrollAdjustmentCreatedEvent(adjustment.Id, tenantId, employeeId, amount, type));
        return adjustment;
    }

    public void Apply(Guid payrollRunId)
    {
        if (Status != AdjustmentStatus.Pending)
            throw new InvalidOperationException($"Cannot apply adjustment in status {Status}.");

        PayrollRunId = payrollRunId;
        Status = AdjustmentStatus.Applied;
        AppliedAtUtc = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new PayrollAdjustmentAppliedEvent(Id, TenantId, EmployeeId, payrollRunId));
    }

    public void Cancel()
    {
        if (Status != AdjustmentStatus.Pending)
            throw new InvalidOperationException("Only pending adjustments can be cancelled.");

        Status = AdjustmentStatus.Cancelled;
    }
}
