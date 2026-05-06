using Karamchari.Billing.Domain.BillableEntries;
using Karamchari.Billing.Domain.Invoices;
using Karamchari.Billing.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Billing.Services;

public sealed class InvoiceGeneratorService
{
    private readonly BillingDbContext _db;

    public InvoiceGeneratorService(BillingDbContext db) => _db = db;

    /// <summary>
    /// Groups unbilled work for a specific contract and generates a Draft Invoice.
    /// </summary>
    public async Task<Invoice?> GenerateDraftAsync(
        Guid contractId, 
        DateOnly periodStart, 
        DateOnly periodEnd, 
        CancellationToken ct = default)
    {
        var contract = await _db.Contracts
            .FirstOrDefaultAsync(c => c.Id == contractId, ct);
        
        if (contract == null) return null;

        var unbilledEntries = await _db.BillableEntries
            .Where(e => e.ContractId == contractId && 
                        e.WorkDate >= periodStart && 
                        e.WorkDate <= periodEnd && 
                        !e.IsBilled && 
                        !e.IsVoided)
            .ToListAsync(ct);

        if (!unbilledEntries.Any()) return null;

        var invoice = new Invoice
        {
            TenantId = contract.TenantId,
            ClientId = contract.ClientId,
            ContractId = contractId,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd
        };

        // Grouping logic for professional line items
        // We group by Project and Role to provide an itemized view that clients can audit.
        var groups = unbilledEntries
            .GroupBy(e => new { e.ProjectId, e.Rate }) 
            .Select(g => new
            {
                // In a production scenario, we'd join with Project/Role names for the description
                Description = $"Professional Services - Project {g.Key.ProjectId} (Rate: {contract.Currency} {g.Key.Rate}/hr)",
                TotalHours = g.Sum(x => x.Hours),
                Rate = g.Key.Rate
            });

        foreach (var group in groups)
        {
            invoice.AddLine(group.Description, group.TotalHours, group.Rate);
        }

        using var tx = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            _db.Invoices.Add(invoice);
            await _db.SaveChangesAsync(ct);

            // Update entries to mark as billed
            foreach (var entry in unbilledEntries)
            {
                entry.MarkBilled(invoice.Id);
            }

            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return invoice;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
}
