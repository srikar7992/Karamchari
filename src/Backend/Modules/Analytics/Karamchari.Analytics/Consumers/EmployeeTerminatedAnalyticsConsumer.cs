using Karamchari.Analytics.Domain;
using Karamchari.Analytics.Persistence;
using Karamchari.Core.Contracts.IntegrationEvents.V1;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Analytics.Consumers;

// Consumes EmployeeOnboardedIntegrationEventV1 to mark historical record; 
// no EmployeeTerminated integration event exists yet — stubbed for future wire-up.
public sealed class EmployeeTerminatedAnalyticsConsumer(AnalyticsDbContext db)
    : IConsumer<EmployeeOnboardedIntegrationEventV1>
{
    // This consumer registers as a secondary subscriber to EmployeeOnboarded
    // to ensure DimEmployee is populated even if EmployeeHired domain event was missed.
    public async Task Consume(ConsumeContext<EmployeeOnboardedIntegrationEventV1> context)
    {
        var msg = context.Message;
        var existing = await db.DimEmployees.FindAsync([msg.EmployeeId], context.CancellationToken);
        if (existing is not null) return;

        db.DimEmployees.Add(new DimEmployee
        {
            EmployeeId = msg.EmployeeId, TenantId = msg.TenantId,
            FullName = msg.LegalName, HireDate = msg.HiredOn, IsActive = true
        });
        await db.SaveChangesAsync(context.CancellationToken);
    }
}
