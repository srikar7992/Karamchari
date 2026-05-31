using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Karamchari.Billing.Contracts;
using Karamchari.Billing.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Billing.Services;

public sealed class BillingReadService : IBillingReadService
{
    private readonly BillingDbContext _dbContext;

    public BillingReadService(BillingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<decimal> GetUnbilledRevenueAsync(string tenantId, CancellationToken ct = default)
    {
        return await _dbContext.BillableEntries
            .Where(e => e.TenantId == tenantId && !e.IsBilled && !e.IsVoided)
            .SumAsync(e => e.Amount, ct);
    }

    public async Task<IReadOnlyList<OutstandingInvoiceDto>> GetOutstandingInvoicesAsync(string tenantId, CancellationToken ct = default)
    {
        var invoices = await _dbContext.Invoices
            .Include(i => i.Payments)
            .Where(i => i.TenantId == tenantId &&
                        i.Status != Domain.Invoices.InvoiceStatus.Draft &&
                        i.Status != Domain.Invoices.InvoiceStatus.Paid &&
                        i.Status != Domain.Invoices.InvoiceStatus.Cancelled)
            .ToListAsync(ct);

        return invoices.Select(inv => new OutstandingInvoiceDto(
            inv.Id,
            inv.ClientId,
            inv.GrandTotal,
            inv.PeriodEnd,
            inv.Payments.Select(p => p.Amount).ToList()
        )).ToList();
    }

    public async Task<Guid?> GetEmployeeRoleIdAsync(string tenantId, Guid employeeId, Guid projectId, DateOnly asOfDate, CancellationToken ct = default)
    {
        var roleMapping = await _dbContext.EmployeeRoles
            .Where(r => r.TenantId == tenantId && r.EmployeeId == employeeId &&
                        (r.ProjectId == null || r.ProjectId == projectId) &&
                        r.EffectiveFrom <= asOfDate && (r.EffectiveTo == null || r.EffectiveTo >= asOfDate))
            .OrderByDescending(r => r.ProjectId)
            .FirstOrDefaultAsync(ct);

        return roleMapping?.RoleId;
    }

    public async Task<decimal?> ResolveRateAsync(string tenantId, Guid projectId, Guid roleId, DateOnly asOfDate, CancellationToken ct = default)
    {
        var contract = await _dbContext.Contracts
            .Include(c => c.RateCards)
            .Where(c => c.TenantId == tenantId && c.ProjectId == projectId && c.IsActive)
            .FirstOrDefaultAsync(ct);

        if (contract == null) return null;

        try
        {
            return contract.ResolveRate(roleId, asOfDate);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public async Task<InvoiceFinalizedDetailsDto?> GetInvoiceFinalizedDetailsAsync(Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await _dbContext.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId, ct);

        if (invoice == null) return null;

        return new InvoiceFinalizedDetailsDto(invoice.ClientId, invoice.FinalizedAt);
    }
}
