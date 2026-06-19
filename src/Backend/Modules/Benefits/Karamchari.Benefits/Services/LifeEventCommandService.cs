// -----------------------------------------------------------------------
// <copyright file="LifeEventCommandService.cs" company="Karamchari">
// Copyright (c) Karamchari.
// All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace Karamchari.Benefits.Services;

using Karamchari.Benefits.Contracts.IntegrationEvents;
using Karamchari.Benefits.Domain;
using Karamchari.Benefits.Persistence;
using Karamchari.Core.Multitenancy;
using MassTransit;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Write operations for qualifying life events and the mid-year election changes they enable.
///
/// Flow:
///   1. Employee calls SubmitLifeEventAsync → life event created in PendingVerification.
///      LifeEventSubmittedIntegrationEvent published → Workflow module starts verification.
///   2. Admin verifies the event (BenefitAdminService.VerifyLifeEventAsync) → window opens.
///   3. Employee calls ApplyElectionChangeAsync within the 30-day window.
///      BenefitElectionChangedIntegrationEvent published → Payroll updates deductions.
/// </summary>
public sealed class LifeEventCommandService(
    BenefitsDbContext db,
    ITenantProvider tenantProvider,
    IPublishEndpoint bus)
{
    public async Task<Guid> SubmitLifeEventAsync(
        Guid employeeId,
        LifeEventType eventType,
        DateOnly eventDate,
        string? documentStorageReference,
        CancellationToken ct = default)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();
        var enrollment = await LoadActiveEnrollmentAsync(employeeId, tenantId, ct);

        var lifeEvent = enrollment.SubmitLifeEvent(eventType, eventDate, documentStorageReference);
        await db.SaveChangesAsync(ct);

        await bus.Publish(new LifeEventSubmittedIntegrationEvent(
            enrollment.Id.Value,
            lifeEvent.Id.Value,
            employeeId,
            tenantId,
            eventType.ToString(),
            eventDate,
            lifeEvent.WindowClosesAt,
            documentStorageReference,
            DateTimeOffset.UtcNow), ct);

        return lifeEvent.Id.Value;
    }

    /// <summary>
    /// Applies an election change enabled by a verified life event.
    /// Supersedes the prior active election for the category; costs snapshotted from plan now.
    /// Publishes BenefitElectionChangedIntegrationEvent so Payroll can update deductions.
    /// </summary>
    public async Task<Guid> ApplyElectionChangeAsync(
        Guid employeeId,
        Guid lifeEventId,
        Guid planId,
        BenefitCategory category,
        CoverageTier tier,
        DateOnly effectiveDate,
        CancellationToken ct = default)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();
        var enrollment = await LoadActiveEnrollmentAsync(employeeId, tenantId, ct);

        var leId = new LifeEventId(lifeEventId);
        var lifeEventTypeStr = enrollment.LifeEvents
            .FirstOrDefault(le => le.Id == leId)?.EventType.ToString()
            ?? throw new InvalidOperationException($"Life event {lifeEventId} not found on enrollment.");

        var planKey = new BenefitPlanId(planId);
        var plan = await db.Plans
            .FirstOrDefaultAsync(p => p.Id == planKey && p.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException($"Benefit plan {planId} not found.");

        var tierCost = plan.GetTier(tier)
            ?? throw new InvalidOperationException(
                $"Plan '{plan.PlanName}' does not offer the {tier} coverage tier.");

        var updated = enrollment.ApplyLifeEventElectionChange(
            leId, plan.Id, category, tier,
            tierCost.EmployeeMonthlyPremium, tierCost.EmployerMonthlyPremium, effectiveDate);

        await db.SaveChangesAsync(ct);

        await bus.Publish(new BenefitElectionChangedIntegrationEvent(
            enrollment.Id.Value,
            employeeId,
            tenantId,
            lifeEventTypeStr,
            effectiveDate,
            enrollment.ActiveElections.Select(el => new ActiveElectionDto(
                el.PlanId.Value,
                el.Category.ToString(),
                el.CoverageTier.ToString(),
                el.EmployeeMonthlyPremium,
                el.EmployerMonthlyPremium,
                el.EffectiveDate)).ToList(),
            DateTimeOffset.UtcNow), ct);

        return updated.Id.Value;
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private async Task<BenefitEnrollment> LoadActiveEnrollmentAsync(
        Guid employeeId, string tenantId, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await db.Enrollments
            .Include(e => e.Elections)
            .Include(e => e.Dependents)
            .Include(e => e.LifeEvents)
            .FirstOrDefaultAsync(e =>
                e.TenantId == tenantId &&
                e.EmployeeId == employeeId &&
                e.PlanYearStart <= today &&
                e.PlanYearEnd >= today &&
                e.Status == EnrollmentStatus.Active, ct)
            ?? throw new InvalidOperationException(
                $"No active benefits enrollment found for employee {employeeId}. " +
                "Life events require an Active enrollment.");
    }
}
