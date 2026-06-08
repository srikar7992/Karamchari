using Karamchari.Core.Messaging;
using Karamchari.HR.Domain.Employees.Events;
using Karamchari.Onboarding.Domain;
using Karamchari.Onboarding.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Karamchari.Onboarding.Consumers;

/// <summary>
/// Auto-creates a NewHire OnboardingCase when an employee is hired.
/// Applies the first active NewHire template for the tenant if one exists.
/// </summary>
public sealed class EmployeeHiredConsumer(OnboardingDbContext db)
    : IConsumer<EnterpriseEventEnvelope<EmployeeHired>>
{
    public async Task Consume(ConsumeContext<EnterpriseEventEnvelope<EmployeeHired>> context)
    {
        var envelope = context.Message;
        var payload = envelope.Payload;
        var tenantId = envelope.TenantId;

        // Idempotency: one NewHire case per employee per tenant
        var exists = await db.Cases.AnyAsync(
            c => c.TenantId == tenantId
              && c.EmployeeId == payload.EmployeeId
              && c.CaseType == OnboardingCaseType.NewHire,
            context.CancellationToken);

        if (exists) return;

        var template = await db.Templates
            .Include(t => t.Tasks)
            .Where(t => t.TenantId == tenantId
                     && t.IsActive
                     && t.CaseType == OnboardingCaseType.NewHire)
            .FirstOrDefaultAsync(context.CancellationToken);

        var startDate = new DateTimeOffset(
            payload.HiredOn.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));

        var @case = OnboardingCase.Initiate(
            tenantId,
            payload.EmployeeId,
            payload.LegalName,
            OnboardingCaseType.NewHire,
            startDate,
            department: null,
            role: null,
            templateId: template?.Id);

        if (template is not null)
        {
            template.ApplyTo(@case);
            @case.Start();
        }

        db.Cases.Add(@case);
        await db.SaveChangesAsync(context.CancellationToken);
    }
}
