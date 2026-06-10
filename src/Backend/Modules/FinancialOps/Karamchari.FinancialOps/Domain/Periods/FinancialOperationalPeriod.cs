// -----------------------------------------------------------------------
// <copyright file="FinancialOperationalPeriod.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Karamchari.Core.Domain.Primitives;
using Karamchari.Core.Multitenancy;
using Karamchari.FinancialOps.Contracts.Primitives;

namespace Karamchari.FinancialOps.Domain.Periods;

/// <summary>
/// Defines an operational period for financial workforce operations, independent of payroll.
/// Governs cutoff windows and lock states for reimbursements and loans.
/// </summary>
public sealed class FinancialOperationalPeriod : AggregateRoot<FinancialOperationalPeriodId>, ITenantOwned
{
    private FinancialOperationalPeriod(
        FinancialOperationalPeriodId id,
        string tenantId,
        string name,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        DateTimeOffset cutoffAt)
        : base(id)
    {
        TenantId = tenantId;
        Name = name;
        StartsAt = startsAt;
        EndsAt = endsAt;
        CutoffAt = cutoffAt;
        IsLocked = false;
        IsClosed = false;
    }

    private FinancialOperationalPeriod()
    {
        // EF Core
        TenantId = string.Empty;
        Name = string.Empty;
    }

    /// <summary>
    /// Gets the tenant identifier.
    /// </summary>
    public string TenantId { get; private set; }

    /// <summary>
    /// Gets the display name of the period (e.g., "May 2026 Operations").
    /// </summary>
    public string Name { get; private set; }

    /// <summary>
    /// Gets the start timestamp of the period.
    /// </summary>
    public DateTimeOffset StartsAt { get; private set; }

    /// <summary>
    /// Gets the end timestamp of the period.
    /// </summary>
    public DateTimeOffset EndsAt { get; private set; }

    /// <summary>
    /// Gets the cutoff timestamp. Operations submitted after this point are 
    /// typically rolled over to the next period.
    /// </summary>
    public DateTimeOffset CutoffAt { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the period is locked (no new entries allowed).
    /// </summary>
    public bool IsLocked { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the period is closed (reconciled and finalized).
    /// </summary>
    public bool IsClosed { get; private set; }

    /// <summary>
    /// Creates a new financial operational period.
    /// </summary>
    public static FinancialOperationalPeriod Create(
        string tenantId,
        string name,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        DateTimeOffset cutoffAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (startsAt >= endsAt)
        {
            throw new ArgumentException("StartsAt must be before EndsAt.");
        }

        if (cutoffAt < startsAt || cutoffAt > endsAt)
        {
            throw new ArgumentException("CutoffAt must be within the period range.");
        }

        return new FinancialOperationalPeriod(
            FinancialOperationalPeriodId.New(),
            tenantId,
            name,
            startsAt,
            endsAt,
            cutoffAt);
    }

    /// <summary>
    /// Locks the period, preventing new financial entries from being recorded.
    /// </summary>
    public void Lock()
    {
        if (IsClosed)
        {
            throw new InvalidOperationException("Cannot lock a period that is already closed.");
        }
        IsLocked = true;
    }

    /// <summary>
    /// Closes the period after reconciliation is complete.
    /// </summary>
    public void Close()
    {
        if (!IsLocked)
        {
            throw new InvalidOperationException("Period must be locked before it can be closed.");
        }
        IsClosed = true;
    }
}
