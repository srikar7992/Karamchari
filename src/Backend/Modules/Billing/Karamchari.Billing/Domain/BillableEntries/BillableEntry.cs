// -----------------------------------------------------------------------
// <copyright file="BillableEntry.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Billing.Domain.BillableEntries;

/// <summary>
/// A non-editable record of billable work.
/// Derived from approved timesheets.
/// If a timesheet is edited, the corresponding BillableEntry is Voided and a new one is created.
/// </summary>
public sealed class BillableEntry
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;

    /// <summary>The source entry in the TimeAttendance module.</summary>
    public Guid TimesheetEntryId { get; init; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ContractId { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ProjectId { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid EmployeeId { get; init; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly WorkDate { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal Hours { get; init; }

    /// <summary>The rate applied at the time of creation based on the contract rate card.</summary>
    public decimal Rate { get; init; }

    /// <summary>Hours * Rate. The authoritative revenue amount for this work.</summary>
    public decimal Amount { get; init; }

    /// <summary>
    /// If true, this entry is no longer valid (e.g., due to a retroactive timesheet edit).
    /// Used instead of deletion to maintain a perfect audit trail.
    /// </summary>
    public bool IsVoided { get; private set; }

    /// <summary>Flagged true once included in a finalized Invoice.</summary>
    public bool IsBilled { get; private set; }
    public Guid? InvoiceId { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Void()
    {
        if (IsBilled)
            throw new InvalidOperationException("Cannot void an entry that has already been billed. Issue a Credit Note instead.");

        IsVoided = true;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void MarkBilled(Guid invoiceId)
    {
        if (IsVoided)
            throw new InvalidOperationException("Cannot bill a voided entry.");

        IsBilled = true;
        InvoiceId = invoiceId;
    }
}
