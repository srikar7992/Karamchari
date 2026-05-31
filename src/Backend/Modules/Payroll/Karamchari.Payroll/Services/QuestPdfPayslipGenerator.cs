namespace Karamchari.Payroll.Services.Payslip;

using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

/// <summary>
/// Implements the PDF generation logic using QuestPDF.
/// </summary>
public sealed class QuestPdfPayslipGenerator : IPayslipGenerator
{
    static QuestPdfPayslipGenerator()
    {
        // Set QuestPDF license (Community is for non-commercial or small revenue)
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <inheritdoc/>
    public byte[] Generate(PayslipData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1, Unit.Inch);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Verdana));

                page.Header().Row(row =>
                {
                    row.RelativeItem().Column(col =>
                    {
                        col.Item().Text(txt => txt.Span("Karamchari").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium));
                        col.Item().Text(txt => txt.Span("Modern Workforce Management").FontSize(9).Italic());
                    });

                    row.RelativeItem().AlignRight().Column(col =>
                    {
                        col.Item().Text(txt => txt.Span($"Payslip for {data.Month}").FontSize(14).SemiBold());
                        col.Item().Text(txt => txt.Span($"Employee ID: {data.EmployeeId}").FontSize(9));
                    });
                });

                page.Content().PaddingVertical(20).Column(x =>
                {
                    // Employee Info Section
                    x.Item().PaddingBottom(20).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Cell().Text(txt => txt.Span("Name:").SemiBold());
                        table.Cell().Text(data.EmployeeName);
                        table.Cell().Text(txt => txt.Span("PAN:").SemiBold());
                        table.Cell().Text(data.Pan);

                        table.Cell().Text(txt => txt.Span("Designation:").SemiBold());
                        table.Cell().Text(data.Designation);
                        table.Cell().Text(txt => txt.Span("Bank:").SemiBold());
                        table.Cell().Text(data.BankName);

                        table.Cell().Text(txt => txt.Span("Department:").SemiBold());
                        table.Cell().Text(data.Department);
                        table.Cell().Text(txt => txt.Span("A/C No:").SemiBold());
                        table.Cell().Text(data.AccountNumber);
                    });

                    x.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                    // Earnings and Deductions Tables
                    x.Item().PaddingTop(10).Row(row =>
                    {
                        // Earnings Column
                        row.RelativeItem().PaddingRight(10).Column(inner =>
                        {
                            inner.Item().Text(txt => txt.Span("Earnings").SemiBold().FontSize(12));
                            inner.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                });

                                decimal totalEarnings = 0;
                                foreach (var (name, amount) in data.Earnings)
                                {
                                    t.Cell().PaddingVertical(2).Text(name);
                                    t.Cell().PaddingVertical(2).AlignRight().Text($"â‚¹{amount:N2}");
                                    totalEarnings += amount;
                                }

                                t.Cell().Text(txt => txt.Span("Total Earnings").SemiBold());
                                t.Cell().AlignRight().Text(txt => txt.Span($"â‚¹{totalEarnings:N2}").SemiBold());
                            });
                        });

                        // Deductions Column
                        row.RelativeItem().PaddingLeft(10).Column(inner =>
                        {
                            inner.Item().Text(txt => txt.Span("Deductions").SemiBold().FontSize(12));
                            inner.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn();
                                    c.RelativeColumn();
                                });

                                decimal totalDeductions = 0;
                                foreach (var (name, amount) in data.Deductions)
                                {
                                    t.Cell().PaddingVertical(2).Text(name);
                                    t.Cell().PaddingVertical(2).AlignRight().Text($"â‚¹{amount:N2}");
                                    totalDeductions += amount;
                                }

                                t.Cell().Text(txt => txt.Span("Total Deductions").SemiBold());
                                t.Cell().AlignRight().Text(txt => txt.Span($"â‚¹{totalDeductions:N2}").SemiBold());
                            });
                        });
                    });

                    // Net Pay Section
                    x.Item().PaddingTop(30).Background(Colors.Grey.Lighten4).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Text(txt => txt.Span("Net Pay").FontSize(14).SemiBold());
                        row.RelativeItem().AlignRight().Text(txt => txt.Span($"â‚¹{data.NetPay:N2}").FontSize(14).SemiBold().FontColor(Colors.Green.Medium));
                    });

                    // YTD Summary
                    x.Item().PaddingTop(20).Column(inner =>
                    {
                        inner.Item().Text(txt => txt.Span("YTD Summary").SemiBold().FontSize(11));
                        inner.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });

                            t.Cell().Text(txt => txt.Span("YTD Gross").SemiBold());
                            t.Cell().Text(txt => txt.Span("YTD PF").SemiBold());
                            t.Cell().Text(txt => txt.Span("YTD TDS").SemiBold());

                            t.Cell().Text($"â‚¹{data.YtdTotals.GetValueOrDefault("Gross", 0):N0}");
                            t.Cell().Text($"â‚¹{data.YtdTotals.GetValueOrDefault("PF", 0):N0}");
                            t.Cell().Text($"â‚¹{data.YtdTotals.GetValueOrDefault("TDS", 0):N0}");
                        });
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("This is a computer-generated document and does not require a signature.");
                    x.Span(" Page ");
                    x.CurrentPageNumber();
                });
            });
        });

        return document.GeneratePdf();
    }
}
