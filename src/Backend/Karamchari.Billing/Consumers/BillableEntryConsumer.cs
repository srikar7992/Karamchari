using Karamchari.Billing.Domain.BillableEntries;
using Karamchari.Billing.Domain.Contracts;
using Karamchari.Billing.Persistence;
using Karamchari.TimeAttendance.Contracts;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Billing.Consumers;

public sealed class BillableEntryConsumer : IConsumer<TimesheetApprovedIntegrationEvent>
{
    private const string ConsumerName = nameof(BillableEntryConsumer);
    private readonly BillingDbContext _db;

    public BillableEntryConsumer(BillingDbContext db) => _db = db;

    public async Task Consume(ConsumeContext<TimesheetApprovedIntegrationEvent> context)
    {
        var ev = context.Message;

        // 1. Quality Gate
        if (ev.ActorId == Guid.Empty) return;

        // 2. Idempotency Check
        var processed = await _db.ProcessedEventLogs
            .AnyAsync(l => l.EventId == ev.EventId && l.ConsumerName == ConsumerName, context.CancellationToken);
        if (processed) return;

        // 3. Handle Retroactive Edits (Void old entries)
        if (ev.IsRetroactive)
        {
            var affectedDates = ev.Entries.Select(e => e.Date).Distinct().ToList();
            var existing = await _db.BillableEntries
                .Where(e => e.TenantId == ev.TenantId && 
                            e.EmployeeId == ev.EmployeeId && 
                            affectedDates.Contains(e.WorkDate) && 
                            !e.IsVoided)
                .ToListAsync(context.CancellationToken);

            foreach (var entry in existing)
            {
                entry.Void();
            }
        }

        // 4. Generate Billable Entries
        foreach (var entry in ev.Entries.Where(e => e.IsBillable && e.ProjectId.HasValue))
        {
            var projectId = entry.ProjectId!.Value;

            // Resolve Role for the employee
            var roleMapping = await _db.EmployeeRoles
                .Where(r => r.TenantId == ev.TenantId && r.EmployeeId == ev.EmployeeId && 
                            (r.ProjectId == null || r.ProjectId == projectId) &&
                            r.EffectiveFrom <= entry.Date && (r.EffectiveTo == null || r.EffectiveTo >= entry.Date))
                .OrderByDescending(r => r.ProjectId) // Prefer project-specific role
                .FirstOrDefaultAsync(context.CancellationToken);

            if (roleMapping == null)
            {
                // In production, we might want to flag this for manual review instead of failing the whole batch
                // For now, we'll skip but log a warning or use a default if business rules allow.
                continue; 
            }

            // Resolve Contract and Rate
            var contract = await _db.Contracts
                .Include(c => c.RateCards)
                .Where(c => c.TenantId == ev.TenantId && c.ProjectId == projectId && c.IsActive)
                .FirstOrDefaultAsync(context.CancellationToken);

            if (contract == null) continue;

            try
            {
                var rate = contract.ResolveRate(roleMapping.RoleId, entry.Date);
                var amount = entry.Hours * rate;

                _db.BillableEntries.Add(new BillableEntry
                {
                    TenantId = ev.TenantId,
                    TimesheetEntryId = Guid.NewGuid(), // Placeholder for unique row link if needed
                    ContractId = contract.Id,
                    ProjectId = projectId,
                    EmployeeId = ev.EmployeeId,
                    WorkDate = entry.Date,
                    Hours = entry.Hours,
                    Rate = rate,
                    Amount = amount
                });
            }
            catch (InvalidOperationException)
            {
                // Missing rate card for role - skip or flag
                continue;
            }
        }

        // 5. Stamp Processed
        _db.ProcessedEventLogs.Add(new Domain.Analytics.ProcessedEventLog
        {
            EventId = ev.EventId,
            ConsumerName = ConsumerName,
            TenantId = ev.TenantId
        });

        await _db.SaveChangesAsync(context.CancellationToken);
    }
}
