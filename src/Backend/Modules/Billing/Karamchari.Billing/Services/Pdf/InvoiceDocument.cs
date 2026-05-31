using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Karamchari.Billing.Services.Pdf;

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record InvoiceSnapshotData(
    string InvoiceNumber,
    string ClientName,
    string ClientAddress,
    string ClientGstin,
    DateOnly Date,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    IReadOnlyList<InvoiceLineData> Lines,
    decimal Subtotal,
    decimal TaxPercentage,
    decimal GstAmount,
    decimal TotalAmount,
    string TotalInWords,
    string Currency);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed record InvoiceLineData(
    string Description,
    decimal Quantity,
    decimal Rate,
    decimal Amount);

/// <summary>
/// Provides required documentation for this member.
/// </summary>
public sealed class InvoiceDocument : IDocument
{
    private readonly InvoiceSnapshotData _data;

    public InvoiceDocument(InvoiceSnapshotData data) => _data = data;

    /// <summary>
    /// Provides required documentation for this member.
    /// </summary>
    public void Compose(IDocumentContainer container)
    {
        container.Page(page =>
        {
            page.Margin(40);
            page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana)); // Safer default

            page.Header().Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("KARAMCHARI").FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                    col.Item().Text("Service Business OS").FontSize(9).Italic();
                });

                row.RelativeItem().AlignRight().Column(col =>
                {
                    col.Item().Text("INVOICE").FontSize(24).ExtraBold();
                    col.Item().Text($"#{_data.InvoiceNumber}").SemiBold();
                    col.Item().Text($"Date: {_data.Date:dd MMM yyyy}");
                    col.Item().Text($"Period: {_data.PeriodStart:dd MMM} - {_data.PeriodEnd:dd MMM yyyy}").FontSize(8).FontColor(Colors.Grey.Medium);
                });
            });

            page.Content().PaddingVertical(20).Column(col =>
            {
                // Client Info
                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().Text("BILL TO").FontSize(8).SemiBold().FontColor(Colors.Grey.Medium);
                        c.Item().Text(_data.ClientName).Bold();
                        c.Item().Text(_data.ClientAddress).FontSize(9);
                        if (!string.IsNullOrEmpty(_data.ClientGstin))
                            c.Item().Text($"GSTIN: {_data.ClientGstin}").FontSize(9).FontColor(Colors.Blue.Darken2);
                    });

                    row.RelativeItem().AlignRight().Column(c =>
                    {
                        c.Item().Text("PAYMENT TERMS").FontSize(8).SemiBold().FontColor(Colors.Grey.Medium);
                        c.Item().Text("Due on Receipt").FontSize(9);
                    });
                });

                col.Item().PaddingTop(20).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);
                        columns.RelativeColumn();
                        columns.ConstantColumn(60);
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(100);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderStyle).Text("#");
                        header.Cell().Element(HeaderStyle).Text("Description");
                        header.Cell().Element(HeaderStyle).AlignRight().Text("Qty");
                        header.Cell().Element(HeaderStyle).AlignRight().Text("Rate");
                        header.Cell().Element(HeaderStyle).AlignRight().Text("Total");

                        static IContainer HeaderStyle(IContainer container) =>
                            container.DefaultTextStyle(x => x.SemiBold()).PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Black);
                    });

                    for (int i = 0; i < _data.Lines.Count; i++)
                    {
                        var line = _data.Lines[i];
                        table.Cell().Element(CellStyle).Text((i + 1).ToString());
                        table.Cell().Element(CellStyle).Text(line.Description);
                        table.Cell().Element(CellStyle).AlignRight().Text(line.Quantity.ToString("N2"));
                        table.Cell().Element(CellStyle).AlignRight().Text($"{_data.Currency} {line.Rate:N2}");
                        table.Cell().Element(CellStyle).AlignRight().Text($"{_data.Currency} {line.Amount:N2}");

                        static IContainer CellStyle(IContainer container) =>
                            container.PaddingVertical(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                    }
                });

                // Totals
                col.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem().Column(c =>
                    {
                        c.Item().PaddingTop(10).Text("Amount in Words:").FontSize(8).SemiBold().FontColor(Colors.Grey.Medium);
                        c.Item().Text($"{_data.TotalInWords} only").FontSize(9).Italic();

                        c.Item().PaddingTop(20).Text("NOTES").FontSize(8).SemiBold().FontColor(Colors.Grey.Medium);
                        c.Item().Text("Please quote invoice number on all payments.").FontSize(8);
                    });

                    row.RelativeItem().AlignRight().Column(c =>
                    {
                        c.Item().Row(r =>
                        {
                            r.ConstantItem(100).Text("Subtotal:");
                            r.ConstantItem(100).AlignRight().Text($"{_data.Currency} {_data.Subtotal:N2}");
                        });

                        if (_data.TaxPercentage > 0)
                        {
                            c.Item().Row(r =>
                            {
                                r.ConstantItem(100).Text($"Tax ({_data.TaxPercentage}%):");
                                r.ConstantItem(100).AlignRight().Text($"{_data.Currency} {_data.GstAmount:N2}");
                            });
                        }

                        c.Item().PaddingTop(5).BorderTop(1).Row(r =>
                        {
                            r.ConstantItem(100).Text("Total:").FontSize(12).Bold();
                            r.ConstantItem(100).AlignRight().Text($"{_data.Currency} {_data.TotalAmount:N2}").FontSize(12).Bold();
                        });
                    });
                });
            });

            page.Footer().Column(f =>
            {
                f.Item().PaddingTop(20).AlignCenter().Text("This is a computer generated document and does not require a physical signature.").FontSize(8).FontColor(Colors.Grey.Medium);
                f.Item().AlignCenter().DefaultTextStyle(x => x.FontSize(8)).Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        });
    }
}
