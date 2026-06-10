// -----------------------------------------------------------------------
// <copyright file="CollectionCase.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Billing.Domain.Collections;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum CollectionStatus
{
    Active = 0,
    Escalated = 1,
    Disputed = 2,
    Closed = 3
}

/// <summary>
/// Tracks the collection lifecycle of an unpaid or partially paid invoice.
/// </summary>
public sealed class CollectionCase
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string TenantId { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid InvoiceId { get; init; }

    /// <summary>Snapshot of amount remaining to be collected.</summary>
    public decimal OutstandingAmount { get; private set; }

    /// <summary>Computed days since invoice period end or finalized date.</summary>
    public int DaysOutstanding { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public CollectionStatus Status { get; private set; } = CollectionStatus.Active;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string CurrentStage { get; private set; } = "Initial"; // e.g., 7d, 30d, 60d, 90d

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public int ReminderCount { get; private set; }

    public DateTimeOffset? LastActionAt { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public static CollectionCase Create(string tenantId, Guid invoiceId, decimal amount, int days)
    {
        return new CollectionCase
        {
            TenantId = tenantId,
            InvoiceId = invoiceId,
            OutstandingAmount = amount,
            DaysOutstanding = days
        };
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void UpdateStatus(decimal outstanding, int days)
    {
        OutstandingAmount = outstanding;
        DaysOutstanding = days;

        if (OutstandingAmount <= 0)
        {
            Status = CollectionStatus.Closed;
        }
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void MarkDisputed()
    {
        Status = CollectionStatus.Disputed;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void RecordReminder(string stage)
    {
        CurrentStage = stage;
        ReminderCount++;
        LastActionAt = DateTimeOffset.UtcNow;

        if (stage == "60d") Status = CollectionStatus.Escalated;
    }
}
