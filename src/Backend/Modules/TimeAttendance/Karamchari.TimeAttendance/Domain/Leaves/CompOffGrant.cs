// -----------------------------------------------------------------------
// <copyright file="CompOffGrant.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.TimeAttendance.Domain.Leaves;

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;

/// <summary>
/// A single comp-off grant earned by working on a holiday/weekend.
/// Expiry is per grant (not per balance) — critical for fairness and auditability.
/// </summary>
public sealed class CompOffGrant : AggregateRoot<Guid>, ITenantOwned
{
    private CompOffGrant(Guid id, Guid employeeId, DateOnly workedOnDate,
        decimal daysEarned, DateOnly expiryDate, Guid approvedBy) : base(id)
    {
        EmployeeId = employeeId;
        WorkedOnDate = workedOnDate;
        DaysEarned = daysEarned;
        ExpiryDate = expiryDate;
        ApprovedBy = approvedBy;
        Status = CompOffGrantStatus.Active;
        EarnedAtUtc = DateTimeOffset.UtcNow;
    }

    private CompOffGrant()
    {
        TenantId = string.Empty;
    }

    public string TenantId { get; internal set; } = string.Empty;
    public Guid EmployeeId { get; private set; }

    /// <summary>Date the employee actually worked (the holiday/weekend).</summary>
    public DateOnly WorkedOnDate { get; private set; }

    public decimal DaysEarned { get; private set; }
    public decimal DaysConsumed { get; private set; }
    public decimal DaysAvailable => DaysEarned - DaysConsumed;

    public DateOnly ExpiryDate { get; private set; }
    public Guid ApprovedBy { get; private set; }
    public CompOffGrantStatus Status { get; private set; }
    public DateTimeOffset EarnedAtUtc { get; private set; }

    /// <summary>Reference to the leave request that consumed from this grant.</summary>
    public Guid? ConsumedByRequestId { get; private set; }

    public static CompOffGrant Create(Guid employeeId, DateOnly workedOnDate,
        decimal daysEarned, DateOnly expiryDate, Guid approvedBy)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(daysEarned);
        if (expiryDate <= workedOnDate)
            throw new ArgumentException("Expiry must be after worked date.");

        return new CompOffGrant(Guid.NewGuid(), employeeId, workedOnDate,
            daysEarned, expiryDate, approvedBy);
    }

    public void Consume(decimal days, Guid leaveRequestId)
    {
        if (Status != CompOffGrantStatus.Active)
            throw new InvalidOperationException($"Grant is {Status}, cannot consume.");
        if (days > DaysAvailable)
            throw new InvalidOperationException($"Cannot consume {days} days. Available: {DaysAvailable}.");

        DaysConsumed += days;
        ConsumedByRequestId = leaveRequestId;

        if (DaysAvailable == 0) Status = CompOffGrantStatus.FullyConsumed;
    }

    public void Expire()
    {
        if (Status != CompOffGrantStatus.Active)
            throw new InvalidOperationException("Only active grants can be expired.");
        Status = CompOffGrantStatus.Expired;
    }

    public void Reinstate()
    {
        if (Status != CompOffGrantStatus.Expired)
            throw new InvalidOperationException("Only expired grants can be reinstated.");
        Status = DaysAvailable > 0 ? CompOffGrantStatus.Active : CompOffGrantStatus.FullyConsumed;
    }

    public void Forfeit()
    {
        if (Status != CompOffGrantStatus.Active)
            throw new InvalidOperationException("Only active grants can be forfeited.");
        Status = CompOffGrantStatus.Forfeited;
    }

    public void MarkPayable()
    {
        if (Status != CompOffGrantStatus.Active)
            throw new InvalidOperationException("Only active grants can be marked payable.");
        Status = CompOffGrantStatus.PendingEncashment;
    }

    /// <summary>
    /// Factory used by CompOffAccrualService. TenantId set via EF shadow property / base class.
    /// </summary>
    public static CompOffGrant Create(
        string tenantId,
        Guid employeeId,
        DateOnly workedOnDate,
        decimal daysEarned,
        DateOnly expiryDate,
        Guid? approvedBy = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(daysEarned);
        if (expiryDate <= workedOnDate)
            throw new ArgumentException("Expiry must be after worked date.");

        var grant = new CompOffGrant(Guid.NewGuid(), employeeId, workedOnDate,
            daysEarned, expiryDate, approvedBy ?? Guid.Empty)
        {
            TenantId = tenantId
        };
        return grant;
    }
}

public enum CompOffGrantStatus
{
    Active = 1,
    FullyConsumed = 2,
    Expired = 3,
    Forfeited = 4,
    PendingEncashment = 5
}
