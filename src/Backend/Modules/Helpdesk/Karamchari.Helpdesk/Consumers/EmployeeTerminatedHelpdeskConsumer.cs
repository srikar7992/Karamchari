using Karamchari.Core.Contracts.IntegrationEvents;
using Karamchari.Helpdesk.Domain;
using Karamchari.Helpdesk.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Helpdesk.Consumers;

/// <summary>
/// When an employee is terminated, close all their open support tickets.
/// Prevents orphaned tickets in the helpdesk queue.
/// </summary>
public sealed class EmployeeTerminatedHelpdeskConsumer(HelpdeskDbContext db)
    : IConsumer<EmployeeTerminatedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<EmployeeTerminatedIntegrationEvent> context)
    {
        var ev = context.Message;
        var ct = context.CancellationToken;

        var openTickets = await db.SupportTickets
            .Where(t => t.TenantId == ev.TenantId
                && t.EmployeeId == ev.EmployeeId
                && t.Status != TicketStatus.Resolved
                && t.Status != TicketStatus.Closed)
            .ToListAsync(ct);

        if (openTickets.Count == 0) return;

        foreach (var ticket in openTickets)
        {
            ticket.Close();
        }

        await db.SaveChangesAsync(ct);
    }
}
