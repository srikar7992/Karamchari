namespace Karamchari.PSA.Domain;

using Karamchari.Core.Multitenancy;

/// <summary>
/// A single line item on a client invoice.
/// </summary>
public sealed class InvoiceLine
{
    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>Gets the description shown on the invoice (e.g., "Jane Smith — Senior Engineer").</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>Gets the total billable hours for this line.</summary>
    public decimal Hours { get; init; }

    /// <summary>Gets the hourly rate for this line.</summary>
    public decimal Rate { get; init; }

    /// <summary>Gets the line total (<c>Hours * Rate</c>).</summary>
    public decimal Amount { get; init; }

    /// <summary>Gets the currency of the amount.</summary>
    public string Currency { get; init; } = "INR";
}

/// <summary>
/// Represents a draft invoice generated from unbilled revenue entries for a specific client.
/// The invoice is immutable once <see cref="IsFinalized"/> is true.
/// </summary>
public sealed class Invoice : ITenantOwned
{
    /// <inheritdoc/>
    public string TenantId { get; private set; } = string.Empty;

    /// <summary>Gets the unique identifier.</summary>
    public Guid Id { get; private set; }

    /// <summary>Gets the client this invoice is addressed to.</summary>
    public Guid ClientId { get; private set; }

    /// <summary>Gets the human-readable invoice number (e.g., "INV-2026-042").</summary>
    public string InvoiceNumber { get; private set; } = string.Empty;

    /// <summary>Gets the date the invoice was generated.</summary>
    public DateOnly InvoiceDate { get; private set; }

    /// <summary>Gets the cutoff date used to select unbilled revenue entries.</summary>
    public DateOnly CutoffDate { get; private set; }

    /// <summary>Gets the invoice line items.</summary>
    public List<InvoiceLine> Lines { get; private set; } = new();

    /// <summary>Gets the subtotal before GST.</summary>
    public decimal SubTotal { get; private set; }

    /// <summary>Gets the CGST rate applied (default 9% for intra-state).</summary>
    public decimal CgstRate { get; private set; } = 0.09m;

    /// <summary>Gets the SGST rate applied (default 9% for intra-state).</summary>
    public decimal SgstRate { get; private set; } = 0.09m;

    /// <summary>Gets the IGST rate applied (default 18% for inter-state).</summary>
    public decimal IgstRate { get; private set; } = 0m;

    /// <summary>Gets the computed CGST amount.</summary>
    public decimal CgstAmount => Math.Round(SubTotal * CgstRate, 2);

    /// <summary>Gets the computed SGST amount.</summary>
    public decimal SgstAmount => Math.Round(SubTotal * SgstRate, 2);

    /// <summary>Gets the computed IGST amount.</summary>
    public decimal IgstAmount => Math.Round(SubTotal * IgstRate, 2);

    /// <summary>Gets the total amount including all applicable taxes.</summary>
    public decimal TotalAmount => SubTotal + CgstAmount + SgstAmount + IgstAmount;

    /// <summary>Gets the billing currency.</summary>
    public string Currency { get; private set; } = "INR";

    /// <summary>Gets whether the invoice is finalized (locked from further changes).</summary>
    public bool IsFinalized { get; private set; }

    /// <summary>Gets the storage path for the generated PDF.</summary>
    public string? PdfPath { get; private set; }

    private Invoice() { }

    /// <summary>
    /// Creates a new draft invoice.
    /// </summary>
    public static Invoice Create(
        Guid clientId,
        string invoiceNumber,
        DateOnly invoiceDate,
        DateOnly cutoffDate,
        List<InvoiceLine> lines,
        string currency = "INR",
        bool isInterState = false)
    {
        if (clientId == Guid.Empty) throw new ArgumentException("ClientId must not be empty.", nameof(clientId));
        ArgumentException.ThrowIfNullOrWhiteSpace(invoiceNumber);
        ArgumentNullException.ThrowIfNull(lines);
        ArgumentNullException.ThrowIfNull(currency);
        if (lines.Count == 0) throw new ArgumentException("Invoice must have at least one line item.", nameof(lines));

        decimal subtotal = lines.Sum(l => l.Amount);

        return new Invoice
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            InvoiceNumber = invoiceNumber,
            InvoiceDate = invoiceDate,
            CutoffDate = cutoffDate,
            Lines = lines,
            SubTotal = subtotal,
            Currency = currency.ToUpperInvariant(),
            // For inter-state billing: IGST at 18%. For intra-state: CGST 9% + SGST 9%.
            CgstRate = isInterState ? 0m : 0.09m,
            SgstRate = isInterState ? 0m : 0.09m,
            IgstRate = isInterState ? 0.18m : 0m,
            IsFinalized = false
        };
    }

    /// <summary>Attaches the storage path of the generated PDF.</summary>
    public void AttachPdf(string pdfPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pdfPath);
        PdfPath = pdfPath;
        IsFinalized = true;
    }
}
