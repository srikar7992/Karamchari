namespace Karamchari.PSA.Services;

using Karamchari.PSA.Domain;
using Karamchari.PSA.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Aggregates unbilled revenue into draft invoices.
/// Flow:
///   1. Query unbilled entries for the client up to <c>cutoffDate</c>
///   2. Group by (Employee × Project) to form line items
///   3. Create Invoice aggregate
///   4. Mark source rows IsInvoiced = true (atomic with invoice creation)
///   5. Generate PDF via <see cref="InvoicePdfDocument"/>
/// </summary>
public sealed partial class InvoiceGeneratorService
{
    private readonly PSADbContext _db;
    private readonly ILogger<InvoiceGeneratorService> _logger;

    public InvoiceGeneratorService(PSADbContext db, ILogger<InvoiceGeneratorService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Generates a draft invoice for a client from all unbilled revenue up to the cutoff date.
    /// Returns null if there is nothing to invoice.
    /// </summary>
    public async Task<Invoice?> GenerateAsync(
        Guid clientId,
        DateOnly cutoffDate,
        string invoiceNumberSeed,
        bool isInterState = false,
        CancellationToken ct = default)
    {
        // 1. Fetch all unbilled entries for the client, joining through project.
        var unbilledItems = await _db.UnbilledRevenue
            .Include(r => r.ProjectId) // navigation will be wired in EF config
            .Where(r =>
                !r.IsInvoiced &&
                r.WorkDate <= cutoffDate)
            .Join(
                _db.Projects.Where(p => p.ClientId == clientId),
                revenue => revenue.ProjectId,
                project => project.Id,
                (revenue, _) => revenue)
            .ToListAsync(ct);

        if (unbilledItems.Count == 0)
        {
            LogNoUnbilledRevenue(_logger, clientId, cutoffDate);
            return null;
        }

        // 2. Fetch employee name lookup (employee name is stored in Payroll domain —
        //    we use the EmployeeId directly in descriptions for now; real impl
        //    would call the HR read-model or use a local projection).
        var employeeIds = unbilledItems.Select(r => r.EmployeeId).Distinct().ToList();

        // 3. Group by (EmployeeId × ProjectId) to create invoice lines.
        var grouped = unbilledItems
            .GroupBy(r => new { r.EmployeeId, r.ProjectId, r.Currency })
            .Select(g =>
            {
                decimal totalHours = g.Sum(r => r.BillableHours);
                // Use the most recent rate in the group for the displayed rate column.
                decimal displayRate = g.OrderByDescending(r => r.WorkDate).First().BillableRate;
                decimal lineAmount = g.Sum(r => r.Amount);

                return new InvoiceLine
                {
                    Description = $"Employee {g.Key.EmployeeId.ToString()[..8]} — Project {g.Key.ProjectId.ToString()[..8]}",
                    Hours = totalHours,
                    Rate = displayRate,
                    Amount = lineAmount,
                    Currency = g.Key.Currency
                };
            })
            .ToList();

        // 4. Create Invoice aggregate.
        var client = await _db.Clients.FindAsync(new object[] { clientId }, ct);
        var invoice = Invoice.Create(
            clientId,
            invoiceNumberSeed,
            DateOnly.FromDateTime(DateTime.UtcNow),
            cutoffDate,
            grouped,
            client?.Currency ?? "INR",
            isInterState);

        _db.Invoices.Add(invoice);

        // 5. Mark source rows as invoiced (same transaction as invoice creation).
        foreach (var item in unbilledItems)
        {
            item.MarkAsInvoiced(invoice.Id);
        }

        await _db.SaveChangesAsync(ct);

        decimal totalAmount = invoice.TotalAmount;
        LogInvoiceGenerated(_logger, invoice.InvoiceNumber, clientId, grouped.Count, totalAmount);

        return invoice;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "No unbilled revenue for Client {ClientId} up to {Cutoff}.")]
    private static partial void LogNoUnbilledRevenue(ILogger logger, Guid clientId, DateOnly cutoff);

    [LoggerMessage(Level = LogLevel.Information, Message = "Generated Invoice {InvoiceNumber} for Client {ClientId} with {Count} line items. Total: {Total}.")]
    private static partial void LogInvoiceGenerated(ILogger logger, string invoiceNumber, Guid clientId, int count, decimal total);
}
