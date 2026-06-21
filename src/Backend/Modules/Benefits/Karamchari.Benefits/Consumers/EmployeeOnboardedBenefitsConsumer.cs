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
/// Opens a new-hire enrollment window and an enrollment record when an employee is onboarded.
///
/// The enrollment window (30 days) establishes the provenance for all elections the employee
/// makes during that period. The enrollment itself starts in PendingSubmission; the employee
/// must log in and make their elections within the window.
///
/// Idempotent: if an enrollment already exists for this employee + plan year, the event is skipped.
/// </summary>
public sealed class EmployeeOnboardedBenefitsConsumer : IConsumer<EmployeeOnboardedIntegrationEventV1>
{
    private readonly BenefitsDbContext _db;
    private readonly ILogger<EmployeeOnboardedBenefitsConsumer> _logger;

    public EmployeeOnboardedBenefitsConsumer(BenefitsDbContext db, ILogger<EmployeeOnboardedBenefitsConsumer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<EmployeeOnboardedIntegrationEventV1> context)
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

        // Create the 30-day new-hire window (opens immediately).
        var now = DateTimeOffset.UtcNow;
        var window = EnrollmentWindow.CreateForNewHire(
            tenantId: msg.TenantId,
            planYearStart: planYearStart,
            planYearEnd: planYearEnd,
            now: now);

        _db.Windows.Add(window);

        // Open the enrollment anchored to that window.
        var enrollment = BenefitEnrollment.Open(
            tenantId: msg.TenantId,
            employeeId: msg.EmployeeId,
            employeeName: msg.LegalName,
            planYearStart: planYearStart,
            planYearEnd: planYearEnd,
            hireDate: hireDate,
            windowId: window.Id);

        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync(context.CancellationToken);

        _logger.LogInformation(
            "Opened new-hire enrollment window {WindowId} and enrollment {EnrollmentId} for Employee {EmployeeId} (plan year {Year}).",
            window.Id.Value, enrollment.Id.Value, msg.EmployeeId, hireDate.Year);
    }
}
