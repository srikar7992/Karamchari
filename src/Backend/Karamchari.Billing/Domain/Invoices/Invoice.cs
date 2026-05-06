namespace Karamchari.Billing.Domain.Invoices;

public enum InvoiceStatus
{
    Draft = 0,
    Finalized = 1,
    Paid = 2,
    Cancelled = 3
}

public sealed class Invoice
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string TenantId { get; init; } = string.Empty;

    public Guid ClientId { get; init; }
    public Guid ContractId { get; init; }

    public string InvoiceNumber { get; set; } = string.Empty;

    public DateOnly PeriodStart { get; init; }
    public DateOnly PeriodEnd { get; init; }

    public decimal TotalAmount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal GrandTotal => TotalAmount + TaxAmount;

    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Draft;

    private readonly List<InvoiceLine> _lines = new();
    public IReadOnlyCollection<InvoiceLine> Lines => _lines.AsReadOnly();

    private readonly List<Payment> _payments = new();
    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinalizedAt { get; private set; }
    public string? FinalizedBy { get; private set; }

    /// <summary>
    /// A JSON snapshot of the invoice data (Client info, Line items, Tax rules, Exchange rates)
    /// frozen at the moment of issuance. Authoritative for PDF generation.
    /// </summary>
    public string? SnapshotJson { get; private set; }

    public void AddLine(string description, decimal quantity, decimal rate)
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Cannot modify a non-draft invoice.");

        var amount = Math.Round(quantity * rate, 2);
        _lines.Add(new InvoiceLine
        {
            Id = Guid.NewGuid(),
            Description = description,
            Quantity = quantity,
            Rate = rate,
            Amount = amount
        });

        TotalAmount = _lines.Sum(x => x.Amount);
        // Simple 18% GST for India (default, should be configurable)
        TaxAmount = Math.Round(TotalAmount * 0.18m, 2);
    }

    public void Finalize(string invoiceNumber, string finalizedBy, string snapshotJson)
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Invoice is not in Draft status.");
        
        InvoiceNumber = invoiceNumber;
        FinalizedBy = finalizedBy;
        SnapshotJson = snapshotJson;
        Status = InvoiceStatus.Finalized;
        FinalizedAt = DateTimeOffset.UtcNow;
    }

    public void RecordPayment(decimal amount, DateTimeOffset paidAt)
    {
        if (Status == InvoiceStatus.Draft)
            throw new InvalidOperationException("Cannot record payment for a Draft invoice.");

        _payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            InvoiceId = this.Id,
            Amount = amount,
            PaidAt = paidAt
        });

        var totalPaid = _payments.Sum(x => x.Amount);
        if (totalPaid >= GrandTotal)
        {
            Status = InvoiceStatus.Paid;
        }
    }
}

public sealed class InvoiceLine
{
    public Guid Id { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Quantity { get; init; }
    public decimal Rate { get; init; }
    public decimal Amount { get; init; }
}

public sealed class Payment
{
    public Guid Id { get; init; }
    public Guid InvoiceId { get; init; }
    public decimal Amount { get; init; }
    public DateTimeOffset PaidAt { get; init; }
}
