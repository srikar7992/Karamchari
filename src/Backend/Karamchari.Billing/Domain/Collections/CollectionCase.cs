namespace Karamchari.Billing.Domain.Collections;

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
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = string.Empty;
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    public Guid InvoiceId { get; init; }
    
    /// <summary>Snapshot of amount remaining to be collected.</summary>
    public decimal OutstandingAmount { get; private set; }

    /// <summary>Computed days since invoice period end or finalized date.</summary>
    public int DaysOutstanding { get; private set; }

    public CollectionStatus Status { get; private set; } = CollectionStatus.Active;

    public string CurrentStage { get; private set; } = "Initial"; // e.g., 7d, 30d, 60d, 90d

    public int ReminderCount { get; private set; }

    public DateTimeOffset? LastActionAt { get; private set; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

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

    public void UpdateStatus(decimal outstanding, int days)
    {
        OutstandingAmount = outstanding;
        DaysOutstanding = days;

        if (OutstandingAmount <= 0)
        {
            Status = CollectionStatus.Closed;
        }
    }

    public void MarkDisputed()
    {
        Status = CollectionStatus.Disputed;
    }

    public void RecordReminder(string stage)
    {
        CurrentStage = stage;
        ReminderCount++;
        LastActionAt = DateTimeOffset.UtcNow;
        
        if (stage == "60d") Status = CollectionStatus.Escalated;
    }
}
