// -----------------------------------------------------------------------
// <copyright file="BenefitAdminService.cs" company="Karamchari">
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
/// HR administrator operations for plan catalog management, enrollment window scheduling,
/// and enrollment lifecycle.
///
/// Plan lifecycle: Draft → (SetTierCost) → Published (Active) → Inactive → Discontinued
/// Window lifecycle: (CreateOpenEnrollmentWindowAsync) → Scheduled → (OpenEnrollmentWindowAsync) → Open → (CloseEnrollmentWindowAsync) → Closed
/// Enrollment lifecycle: PendingSubmission → Submitted → (ActivateEnrollmentAsync) → Active
/// Life event lifecycle: PendingVerification → (VerifyLifeEventAsync) → Verified
///                      (employee applies change) → Completed
/// </summary>
public sealed class BenefitAdminService(
    BenefitsDbContext db,
    ITenantProvider tenantProvider,
    IPublishEndpoint bus)
{
    // ── Plan management ────────────────────────────────────────────────────────

    /// <summary>Creates a new benefit plan in Draft status. Returns the new plan ID.</summary>
    public async Task<Guid> CreatePlanAsync(
        string planName,
        string? description,
        BenefitCategory category,
        string providerName,
        string? externalPlanCode,
        DateOnly planYearStart,
        DateOnly planYearEnd,
        WaitingPeriodRule? waitingRule = null,
        CancellationToken ct = default)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();
        var plan = BenefitPlan.Create(
            tenantId, planName, description, category,
            providerName, externalPlanCode, planYearStart, planYearEnd, waitingRule);
        db.Plans.Add(plan);
        await db.SaveChangesAsync(ct);
        return plan.Id.Value;
    }

    /// <summary>Adds or updates a coverage tier cost on a Draft or Active plan.</summary>
    public async Task SetTierCostAsync(
        Guid planId,
        CoverageTier tier,
        decimal employeePremium,
        decimal employerPremium,
        CancellationToken ct = default)
    {
        var plan = await LoadPlanAsync(planId, ct);
        plan.SetTierCost(tier, employeePremium, employerPremium);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Publishes a Draft plan, making it visible during employee enrollment.
    /// Requires at least one tier to be configured.
    /// </summary>
    public async Task PublishPlanAsync(Guid planId, CancellationToken ct = default)
    {
        var plan = await LoadPlanAsync(planId, ct);
        plan.Publish();
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Moves an Active plan to Inactive (hidden from new enrollments but not discontinued).</summary>
    public async Task DeactivatePlanAsync(Guid planId, CancellationToken ct = default)
    {
        var plan = await LoadPlanAsync(planId, ct);
        plan.Deactivate();
        await db.SaveChangesAsync(ct);
    }

    // ── Enrollment window management ───────────────────────────────────────────

    /// <summary>
    /// Creates an open-enrollment window in Scheduled status for the specified plan year.
    /// Call <see cref="OpenEnrollmentWindowAsync"/> when the window period begins.
    /// Returns the new window ID.
    /// </summary>
    public async Task<Guid> CreateOpenEnrollmentWindowAsync(
        DateOnly planYearStart,
        DateOnly planYearEnd,
        DateTimeOffset windowOpensAt,
        DateTimeOffset windowClosesAt,
        CancellationToken ct = default)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();
        var window = EnrollmentWindow.CreateOpenEnrollment(
            tenantId, planYearStart, planYearEnd, windowOpensAt, windowClosesAt);
        db.Windows.Add(window);
        await db.SaveChangesAsync(ct);
        return window.Id.Value;
    }

    /// <summary>Transitions a Scheduled window to Open, allowing elections.</summary>
    public async Task OpenEnrollmentWindowAsync(Guid windowId, CancellationToken ct = default)
    {
        var window = await LoadWindowAsync(windowId, ct);
        window.Open();
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Closes an Open window, preventing any further elections.</summary>
    public async Task CloseEnrollmentWindowAsync(Guid windowId, CancellationToken ct = default)
    {
        var window = await LoadWindowAsync(windowId, ct);
        window.Close();
        await db.SaveChangesAsync(ct);
    }

    // ── Enrollment management ──────────────────────────────────────────────────

    /// <summary>
    /// Activates a Submitted enrollment after HR review.
    /// Publishes BenefitEnrollmentActivatedIntegrationEvent so Payroll can set up deductions
    /// and Notifications can send the employee a coverage confirmation.
    /// </summary>
    public async Task ActivateEnrollmentAsync(Guid enrollmentId, CancellationToken ct = default)
    {
        var enrollment = await LoadEnrollmentAsync(enrollmentId, ct);
        enrollment.Activate();
        await db.SaveChangesAsync(ct);

        await bus.Publish(new BenefitEnrollmentActivatedIntegrationEvent(
            enrollment.Id.Value,
            enrollment.EmployeeId,
            enrollment.TenantId,
            enrollment.PlanYearStart,
            enrollment.PlanYearEnd,
            enrollment.ActiveElections.Select(el => new ActiveElectionDto(
                el.PlanId.Value,
                el.Category.ToString(),
                el.CoverageTier.ToString(),
                el.EmployeeMonthlyPremium,
                el.EmployerMonthlyPremium,
                el.EffectiveDate)).ToList(),
            DateTimeOffset.UtcNow), ct);
    }

    /// <summary>
    /// Verifies a life event, opening the election change window for the employee.
    /// </summary>
    public async Task VerifyLifeEventAsync(
        Guid enrollmentId,
        Guid lifeEventId,
        string reviewerEmployeeId,
        CancellationToken ct = default)
    {
        var enrollment = await LoadEnrollmentAsync(enrollmentId, ct);
        enrollment.VerifyLifeEvent(new LifeEventId(lifeEventId), reviewerEmployeeId);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Rejects a life event due to invalid or missing documentation.</summary>
    public async Task RejectLifeEventAsync(
        Guid enrollmentId,
        Guid lifeEventId,
        string reviewerEmployeeId,
        string reason,
        CancellationToken ct = default)
    {
        var enrollment = await LoadEnrollmentAsync(enrollmentId, ct);
        enrollment.RejectLifeEvent(new LifeEventId(lifeEventId), reviewerEmployeeId, reason);
        await db.SaveChangesAsync(ct);
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    private async Task<BenefitPlan> LoadPlanAsync(Guid planId, CancellationToken ct)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();
        var id = new BenefitPlanId(planId);
        return await db.Plans
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException($"Benefit plan {planId} not found.");
    }

    private async Task<EnrollmentWindow> LoadWindowAsync(Guid windowId, CancellationToken ct)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();
        var id = new EnrollmentWindowId(windowId);
        return await db.Windows
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException($"Enrollment window {windowId} not found.");
    }

    private async Task<BenefitEnrollment> LoadEnrollmentAsync(Guid enrollmentId, CancellationToken ct)
    {
        var tenantId = tenantProvider.GetCurrentTenantId();
        var id = new BenefitEnrollmentId(enrollmentId);
        return await db.Enrollments
            .Include(e => e.Elections)
            .Include(e => e.Dependents)
            .Include(e => e.LifeEvents)
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId, ct)
            ?? throw new InvalidOperationException($"Enrollment {enrollmentId} not found.");
    }
}
