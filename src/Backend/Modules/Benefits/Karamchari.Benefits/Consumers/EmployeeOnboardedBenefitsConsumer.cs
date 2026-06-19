// -----------------------------------------------------------------------
// <copyright file="EmployeeOnboardedBenefitsConsumer.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Benefits.Consumers;

using Karamchari.Benefits.Domain;
using Karamchari.Benefits.Persistence;
using Karamchari.Core.Contracts.IntegrationEvents.V1;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// Opens a new-hire enrollment window when an employee is onboarded.
/// The enrollment is created in PendingSubmission status; the employee has 30 days
/// (the standard new-hire window) to log in and make their elections.
///
/// Idempotent: if an enrollment already exists for this employee + plan year, the event is skipped.
/// </summary>
public sealed class EmployeeOnboardedBenefitsConsumer : IConsumer<EmployeeOnboardedIntegrationEvent>
{
    private readonly BenefitsDbContext _db;
    private readonly ILogger<EmployeeOnboardedBenefitsConsumer> _logger;

    public EmployeeOnboardedBenefitsConsumer(BenefitsDbContext db, ILogger<EmployeeOnboardedBenefitsConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EmployeeOnboardedIntegrationEvent> context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var msg = context.Message;

        // Derive the plan year from the hire date. Plan years run Jan 1 – Dec 31 for simplicity.
        // Tenants with April–March fiscal years will configure this via PlanYear on BenefitPlan;
        // this consumer just creates the enrollment container — the actual plan year dates come
        // from the tenant's active benefit plans at election time.
        var hireDate = msg.HiredOn;
        var planYearStart = new DateOnly(hireDate.Year, 1, 1);
        var planYearEnd = new DateOnly(hireDate.Year, 12, 31);

        // Idempotency: skip if enrollment already exists for this employee + plan year.
        var exists = await _db.Enrollments
            .AnyAsync(e => e.TenantId == msg.TenantId
                           && e.EmployeeId == msg.EmployeeId
                           && e.PlanYearStart == planYearStart,
                context.CancellationToken);

        if (exists)
        {
            _logger.LogInformation(
                "Benefits enrollment for Employee {EmployeeId} in plan year {Year} already exists. Skipping.",
                msg.EmployeeId, hireDate.Year);
            return;
        }

        var enrollment = BenefitEnrollment.Open(
            tenantId: msg.TenantId,
            employeeId: msg.EmployeeId,
            employeeName: msg.LegalName,
            planYearStart: planYearStart,
            planYearEnd: planYearEnd,
            hireDate: hireDate);

        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation(
            "Opened new-hire benefits enrollment {EnrollmentId} for Employee {EmployeeId} (plan year {Year}).",
            enrollment.Id.Value, msg.EmployeeId, hireDate.Year);
    }
}
