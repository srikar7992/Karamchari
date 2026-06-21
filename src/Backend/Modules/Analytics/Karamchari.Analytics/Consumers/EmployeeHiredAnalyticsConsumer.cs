using Karamchari.Analytics.Domain;
using Karamchari.Analytics.Persistence;
using Karamchari.Core.Contracts.IntegrationEvents.V1;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Analytics.Consumers;

public sealed class EmployeeHiredAnalyticsConsumer(AnalyticsDbContext db) : IConsumer<EmployeeOnboardedIntegrationEvent>
{
    public async Task Consume(ConsumeContext<EmployeeOnboardedIntegrationEvent> context)
    {
        var msg = context.Message;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dateKey = today.Year * 10000 + today.Month * 100 + today.Day;

        var existing = await db.DimEmployees.FindAsync([msg.EmployeeId], context.CancellationToken);
        if (existing is null)
        {
            db.DimEmployees.Add(new DimEmployee
            {
                EmployeeId = msg.EmployeeId, TenantId = msg.TenantId,
                FullName = msg.LegalName, HireDate = msg.HiredOn, IsActive = true
            });
        }
        else
        {
            existing.FullName = msg.LegalName;
            existing.HireDate = msg.HiredOn;
            existing.IsActive = true;
            existing.LastUpdatedUtc = DateTimeOffset.UtcNow;
        }

        db.FactHiring.Add(new FactHiring
        {
            TenantId = msg.TenantId, DateKey = dateKey,
            EmployeeId = msg.EmployeeId, DepartmentId = Guid.Empty,
            SourceChannel = "HR", OfferToJoinDays = 0
        });

        await db.SaveChangesAsync(context.CancellationToken);
    }
}
