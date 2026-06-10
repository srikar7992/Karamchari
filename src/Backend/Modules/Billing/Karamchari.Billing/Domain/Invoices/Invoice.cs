// -----------------------------------------------------------------------
// <copyright file="Invoice.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Billing.Domain.Invoices;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public enum InvoiceStatus
{
    Draft = 0,
    Finalized = 1,
    Paid = 2,
    Cancelled = 3
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class Invoice
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
    public Guid ClientId { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid ContractId { get; init; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly PeriodStart { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateOnly PeriodEnd { get; init; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal TotalAmount { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal TaxAmount { get; private set; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal GrandTotal => TotalAmount + TaxAmount;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Draft;

    private readonly List<InvoiceLine> _lines = new();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyCollection<InvoiceLine> Lines => _lines.AsReadOnly();

    private readonly List<Payment> _payments = new();
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinalizedAt { get; private set; }
    public string? FinalizedBy { get; private set; }

    /// <summary>
    /// A JSON snapshot of the invoice data (Client info, Line items, Tax rules, Exchange rates)
    /// frozen at the moment of issuance. Authoritative for PDF generation.
    /// </summary>
    public string? SnapshotJson { get; private set; }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
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

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class InvoiceLine
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid Id { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public string Description { get; init; } = string.Empty;
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal Quantity { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal Rate { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal Amount { get; init; }
}

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class Payment
{
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid Id { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public Guid InvoiceId { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public decimal Amount { get; init; }
    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DateTimeOffset PaidAt { get; init; }
}
