namespace Karamchari.PSA.Services;

using System.Globalization;
using Karamchari.PSA.Domain;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

/// <summary>
/// Generates a production-quality B2B invoice PDF using QuestPDF.
/// Features: GST breakdown (CGST/SGST/IGST), currency-aware formatting, payment terms.
/// </summary>
public sealed class InvoicePdfDocument : IDocument
{
    private readonly Invoice _invoice;
    private readonly Client _client;

    public InvoicePdfDocument(Invoice invoice, Client client)
    {
        _invoice = invoice;
        _client = client;
    }

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontFamily("Arial").FontSize(10));

            page.Header().Element(ComposeHeader);
            page.Content().PaddingVertical(20).Element(ComposeContent);
            page.Footer().Element(ComposeFooter);
        });
    }

    private void ComposeHeader(IContainer container)
    {
        container.Row(row =>
        {
            // Left: Your company branding
            row.RelativeItem().Column(col =>
            {
                col.Item().Text("Karamchari Technologies Pvt. Ltd.")
                    .FontSize(16).Bold().FontColor(Colors.Indigo.Darken2);
                col.Item().Text("Hyderabad, Telangana â€” GSTIN: 36AAAAA0000A1Z5")
                    .FontSize(8).FontColor(Colors.Grey.Darken1);
                col.Item().PaddingTop(10).Text("TAX INVOICE")
                    .FontSize(22).Bold().FontColor(Colors.Indigo.Darken3);
            });

            // Right: Invoice metadata
            row.ConstantItem(200).Column(col =>
            {
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn();
                        c.RelativeColumn();
                    });
                    AddMetaRow(t, "Invoice No:", _invoice.InvoiceNumber);
                    AddMetaRow(t, "Date:", _invoice.InvoiceDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture));
                    AddMetaRow(t, "Due Date:", _invoice.InvoiceDate.AddDays(30).ToString("dd MMM yyyy", CultureInfo.InvariantCulture));
                });
            });
        });
    }

    private void ComposeContent(IContainer container)
    {
        container.Column(col =>
        {
            // Bill-To section
            col.Item().PaddingVertical(12).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("BILL TO").FontSize(8).Bold().FontColor(Colors.Grey.Darken2);
                    c.Item().Text(_client.Name).Bold();
                    c.Item().Text(_client.BillingAddress);
                    c.Item().Text($"GSTIN: {_client.Gstin}");
                });
            });

            // Divider
            col.Item().LineHorizontal(1).LineColor(Colors.Indigo.Lighten3);
            col.Item().PaddingVertical(8);

            // Line items table
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(4);   // Description
                    columns.RelativeColumn(1.5f); // Hours
                    columns.RelativeColumn(2);    // Rate
                    columns.RelativeColumn(2);    // Amount
                });

                // Header row
                table.Header(header =>
                {
                    HeaderCell(header.Cell(), "Description");
                    HeaderCell(header.Cell(), "Hours");
                    HeaderCell(header.Cell(), "Rate / Hr");
                    HeaderCell(header.Cell(), "Amount");
                });

                // Data rows
                foreach (var line in _invoice.Lines)
                {
                    table.Cell().PaddingVertical(4).Text(line.Description);
                    table.Cell().PaddingVertical(4).AlignRight().Text(line.Hours.ToString("N2", CultureInfo.InvariantCulture));
                    table.Cell().PaddingVertical(4).AlignRight().Text(FormatCurrency(line.Rate, line.Currency));
                    table.Cell().PaddingVertical(4).AlignRight().Text(FormatCurrency(line.Amount, line.Currency));
                }
            });

            col.Item().PaddingTop(12).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            // Totals block (right-aligned)
            col.Item().AlignRight().PaddingTop(8).Column(totals =>
            {
                var cur = _invoice.Currency;
                TotalRow(totals, "Sub Total:", FormatCurrency(_invoice.SubTotal, cur));

                if (_invoice.IgstRate > 0)
                {
                    TotalRow(totals, $"IGST ({_invoice.IgstRate:P0}):", FormatCurrency(_invoice.IgstAmount, cur));
                }
                else
                {
                    TotalRow(totals, $"CGST ({_invoice.CgstRate:P0}):", FormatCurrency(_invoice.CgstAmount, cur));
                    TotalRow(totals, $"SGST ({_invoice.SgstRate:P0}):", FormatCurrency(_invoice.SgstAmount, cur));
                }

                totals.Item().LineHorizontal(1).LineColor(Colors.Indigo.Darken1);
                totals.Item().PaddingTop(4).Row(r =>
                {
                    r.RelativeItem().Text("TOTAL DUE:").Bold().FontSize(13).FontColor(Colors.Indigo.Darken3);
                    r.ConstantItem(160).AlignRight()
                        .Text(FormatCurrency(_invoice.TotalAmount, cur))
                        .Bold().FontSize(13).FontColor(Colors.Indigo.Darken3);
                });
            });

            // Payment terms
            col.Item().PaddingTop(20).Column(terms =>
            {
                terms.Item().Text("PAYMENT TERMS").FontSize(8).Bold().FontColor(Colors.Grey.Darken2);
                terms.Item().Text("Payment due within 30 days of invoice date.");
                terms.Item().Text("Bank: HDFC Bank | Account: 12345678901234 | IFSC: HDFC0001234");
                terms.Item().Text("Please quote the invoice number in your payment reference.");
            });
        });
    }

    private void ComposeFooter(IContainer container)
    {
        container.Row(row =>
        {
            row.RelativeItem().Text("This is a computer-generated invoice. No signature required.")
                .FontSize(8).FontColor(Colors.Grey.Medium);
            row.ConstantItem(60).AlignRight().Text(text =>
            {
                text.Span("Page ").FontSize(8);
                text.CurrentPageNumber().FontSize(8);
                text.Span(" of ").FontSize(8);
                text.TotalPages().FontSize(8);
            });
        });
    }

    // â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static void HeaderCell(IContainer cell, string text)
    {
        cell.Background(Colors.Indigo.Darken3)
            .Padding(6)
            .Text(text).FontColor(Colors.White).Bold().FontSize(9);
    }

    private static void AddMetaRow(TableDescriptor t, string label, string value)
    {
        t.Cell().Text(label).Bold().FontSize(9);
        t.Cell().AlignRight().Text(value).FontSize(9);
    }

    private static void TotalRow(ColumnDescriptor col, string label, string value)
    {
        col.Item().Row(r =>
        {
            r.RelativeItem().Text(label).FontSize(10);
            r.ConstantItem(160).AlignRight().Text(value).FontSize(10);
        });
    }

    private static string FormatCurrency(decimal amount, string currency) =>
        currency == "USD"
            ? $"${amount:N2}"
            : $"â‚¹{amount:N2}";

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public DocumentSettings GetSettings() => new() { CompressDocument = true };

    /// <summary>Generates the PDF bytes for the invoice.</summary>
    public byte[] ToPdfBytes() => this.GeneratePdf();
}
